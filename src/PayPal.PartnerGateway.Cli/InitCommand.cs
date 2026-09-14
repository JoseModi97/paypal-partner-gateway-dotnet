using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PayPal.PartnerGateway.Cli;

/// <summary>
/// Implements <c>paypal-partner init</c>: an interactive setup wizard (mirroring the wizard in
/// ecitizen-pesaflow-gateway-dotnet's CLI) that walks you through PayPal credentials and optional
/// partner settings, then adds the right package(s) for your project the way that project actually
/// consumes packages, wires configuration, and scaffolds a starter usage file.
///
/// Unlike the eCitizen wizard, this one never asks which framework/architecture you're targeting -
/// <see cref="FrameworkDetector"/> figures that out from your project and it's simply announced, not
/// a question. Pass <c>--yes</c>/<c>-y</c> to skip the interactive prompts entirely and run from
/// flags/environment variables only (e.g. for CI).
/// </summary>
public static class InitCommand
{
    /// <summary>
    /// The version written into <c>#:package</c> directives for file-based apps, since there's no
    /// PackageReference for <c>dotnet add package</c> to resolve "latest" against for those. Keep
    /// this in sync with the shipped package version.
    /// </summary>
    private const string PackageVersion = "1.0.0";

    public static async Task<int> RunAsync(string[] args)
    {
        var directory = Directory.GetCurrentDirectory();
        var project = FrameworkDetector.Detect(directory);
        if (project == null)
        {
            Console.Error.WriteLine("No .csproj and no single .cs file found in the current directory.");
            Console.Error.WriteLine("Run 'paypal-partner init' from your project's root folder, or a folder containing one file-based app (e.g. app.cs).");
            return 1;
        }

        var flags = ParseFlags(args);
        var interactive = !(flags.ContainsKey("yes") || flags.ContainsKey("y"));
        var dryRun = flags.ContainsKey("dry-run");

        var clientId = flags.GetValueOrDefault("client-id") ?? Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID") ?? string.Empty;
        var clientSecret = flags.GetValueOrDefault("client-secret") ?? Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET") ?? string.Empty;
        var environmentName = flags.GetValueOrDefault("environment") ?? Environment.GetEnvironmentVariable("PAYPAL_ENVIRONMENT") ?? "Sandbox";
        var partnerAttributionId = flags.GetValueOrDefault("partner-attribution-id") ?? Environment.GetEnvironmentVariable("PAYPAL_PARTNER_ATTRIBUTION_ID") ?? string.Empty;
        var webhookId = flags.GetValueOrDefault("webhook-id") ?? Environment.GetEnvironmentVariable("PAYPAL_WEBHOOK_ID") ?? string.Empty;

        Console.WriteLine($"Detected project: {Describe(project)}");
        Console.WriteLine($"  -> {Path.GetFileName(project.ProjectPath)}{(project.IsFileBasedApp ? " (file-based app, no .csproj)" : "")}");
        Console.WriteLine();

        if (interactive)
        {
            Console.WriteLine("Step 1: PayPal REST API credentials (from the PayPal Developer Dashboard)\n");
            clientId = Prompter.Ask("Client ID", clientId, s => !string.IsNullOrWhiteSpace(s), "Client ID cannot be empty.");
            clientSecret = Prompter.AskSecret("Client Secret", clientSecret, s => !string.IsNullOrWhiteSpace(s), "Client Secret cannot be empty.");

            Console.WriteLine("\nStep 2: Environment\n");
            environmentName = Prompter.Select("Select environment:", new (string, string)[]
            {
                ("Sandbox - safe for development and testing", "Sandbox"),
                ("Live - real money, real merchants", "Live"),
            }, environmentName.Equals("Live", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

            Console.WriteLine("\nStep 3: Optional partner settings (press Enter to skip)\n");
            partnerAttributionId = Prompter.Ask("PayPal-Partner-Attribution-Id / BN code", partnerAttributionId);
            webhookId = Prompter.Ask("Webhook ID (Webhooks tab in the dashboard)", webhookId);

            Console.WriteLine();
            if (!Prompter.Confirm("Apply this configuration now?", true))
            {
                Console.WriteLine("Setup aborted - nothing was changed.");
                return 0;
            }

            Console.WriteLine();
        }

        var packages = new List<string> { "PayPal.PartnerGateway" };
        if (project.HasDependencyInjection)
        {
            packages.Add("PayPal.PartnerGateway.AspNetCore");
        }

        if (dryRun)
        {
            var via = project.IsFileBasedApp ? "#:package directives" : "dotnet add package";
            Console.WriteLine($"(--dry-run) Would add via {via}: {string.Join(", ", packages)}");
            Console.WriteLine("(--dry-run) Would write configuration and scaffold a starter file - stopping here.");
            return 0;
        }

        if (project.IsFileBasedApp)
        {
            InstallIntoFileBasedApp(project, packages);
        }
        else
        {
            await InstallViaDotnetAddPackageAsync(project, packages).ConfigureAwait(false);
        }

        var configOptions = new ConfigOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Environment = environmentName,
            PartnerAttributionId = partnerAttributionId,
            WebhookId = webhookId,
        };

        if (project.HasDependencyInjection && !project.IsFileBasedApp)
        {
            WriteAppSettings(directory, configOptions);
        }

        var scaffoldedFile = CodeScaffolder.Scaffold(directory, project);
        if (scaffoldedFile != null)
        {
            Console.WriteLine($"Generated starter file: {Path.GetRelativePath(directory, scaffoldedFile)}");
        }

        Console.WriteLine();
        PrintNextSteps(project, configOptions);

        return 0;
    }

    private static async Task InstallViaDotnetAddPackageAsync(DetectedProject project, List<string> packages)
    {
        foreach (var package in packages)
        {
            Console.WriteLine($"Adding {package}...");
            var exitCode = await ProcessRunner.RunAsync("dotnet", $"add \"{project.ProjectPath}\" package {package}").ConfigureAwait(false);
            if (exitCode != 0)
            {
                Console.Error.WriteLine($"  'dotnet add package {package}' failed (exit {exitCode}). Add it manually and re-run if needed.");
            }
        }
    }

    /// <summary>
    /// A .NET 10 file-based app has no PackageReference item, so dependencies are declared as
    /// <c>#:package Id@Version</c> directive lines at the top of the file instead. ASP.NET Core
    /// hosting inside a file-based app also needs a <c>#:sdk Microsoft.NET.Sdk.Web</c> directive
    /// (the default SDK there is the plain console one) for the AspNetCore package's
    /// FrameworkReference to resolve, so that's added too when needed.
    /// </summary>
    private static void InstallIntoFileBasedApp(DetectedProject project, List<string> packages)
    {
        var lines = File.ReadAllLines(project.ProjectPath).ToList();
        var directivesToAdd = new List<string>();

        if (project.Kind == ProjectFrameworkKind.AspNetCoreWeb &&
            !lines.Any(l => l.TrimStart().StartsWith("#:sdk", StringComparison.OrdinalIgnoreCase) && l.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase)))
        {
            directivesToAdd.Add("#:sdk Microsoft.NET.Sdk.Web");
        }

        foreach (var package in packages)
        {
            var alreadyPresent = lines.Any(l => l.TrimStart().StartsWith($"#:package {package}@", StringComparison.OrdinalIgnoreCase)
                                                 || l.TrimStart().Equals($"#:package {package}", StringComparison.OrdinalIgnoreCase));
            if (alreadyPresent)
            {
                Console.WriteLine($"{package} is already referenced in {Path.GetFileName(project.ProjectPath)}.");
                continue;
            }

            directivesToAdd.Add($"#:package {package}@{PackageVersion}");
        }

        if (directivesToAdd.Count == 0) return;

        var insertAt = 0;
        while (insertAt < lines.Count && lines[insertAt].TrimStart().StartsWith("#:", StringComparison.Ordinal))
        {
            insertAt++;
        }

        lines.InsertRange(insertAt, directivesToAdd);
        File.WriteAllLines(project.ProjectPath, lines);

        foreach (var directive in directivesToAdd)
        {
            Console.WriteLine($"Added '{directive}' to {Path.GetFileName(project.ProjectPath)}.");
        }
    }

    private static void WriteAppSettings(string directory, ConfigOptions options)
    {
        var path = Path.Combine(directory, "appsettings.json");
        JsonObject root;

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path);
            root = string.IsNullOrWhiteSpace(existing)
                ? new JsonObject()
                : (JsonNode.Parse(existing) as JsonObject ?? new JsonObject());
        }
        else
        {
            root = new JsonObject();
        }

        var section = new JsonObject
        {
            ["ClientId"] = string.IsNullOrWhiteSpace(options.ClientId) ? "YOUR_SANDBOX_CLIENT_ID" : options.ClientId,
            ["ClientSecret"] = string.IsNullOrWhiteSpace(options.ClientSecret) ? "YOUR_SANDBOX_CLIENT_SECRET" : options.ClientSecret,
            ["Environment"] = string.IsNullOrWhiteSpace(options.Environment) ? "Sandbox" : options.Environment,
        };

        if (!string.IsNullOrWhiteSpace(options.PartnerAttributionId)) section["PartnerAttributionId"] = options.PartnerAttributionId;
        if (!string.IsNullOrWhiteSpace(options.WebhookId)) section["WebhookId"] = options.WebhookId;

        root["PayPalPartner"] = section;

        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        Console.WriteLine($"Wrote PayPalPartner section to {Path.GetFileName(path)}.");
    }

    private static void PrintNextSteps(DetectedProject project, ConfigOptions options)
    {
        Console.WriteLine("Next steps:");
        Console.WriteLine();

        switch (project.Kind)
        {
            case ProjectFrameworkKind.AspNetCoreWeb:
                Console.WriteLine("  builder.Services.AddPayPalPartnerGateway(builder.Configuration);");
                Console.WriteLine("  app.MapPayPalPaymentEndpoints();   // from the generated Endpoints/PayPalPaymentEndpoints.cs");
                if (project.IsFileBasedApp)
                {
                    Console.WriteLine();
                    Console.WriteLine("  This all goes in the same .cs file, below the #:package/#:sdk directives - a file-based");
                    Console.WriteLine("  app's top-level statements work exactly like a Minimal API Program.cs.");
                }
                break;

            case ProjectFrameworkKind.WorkerService:
                Console.WriteLine("  builder.Services.AddPayPalPartnerGateway(builder.Configuration);");
                Console.WriteLine("  // inject PayPalPartnerClient into your worker/BackgroundService via constructor DI.");
                break;

            default:
                Console.WriteLine("  Try it: call PayPalDemo.RunAsync() from the generated PayPalDemo.cs,");
                Console.WriteLine("  or build your own client:");
                Console.WriteLine();
                Console.WriteLine("  var client = new PayPal.PartnerGateway.PayPalPartnerClient(new PayPal.PartnerGateway.PayPalPartnerConfig");
                Console.WriteLine("  {");
                Console.WriteLine($"      ClientId = \"{(string.IsNullOrWhiteSpace(options.ClientId) ? "YOUR_CLIENT_ID" : options.ClientId)}\",");
                Console.WriteLine($"      ClientSecret = \"{(string.IsNullOrWhiteSpace(options.ClientSecret) ? "YOUR_CLIENT_SECRET" : options.ClientSecret)}\",");
                Console.WriteLine($"      Environment = PayPal.PartnerGateway.PayPalEnvironment.{(string.IsNullOrWhiteSpace(options.Environment) ? "Sandbox" : options.Environment)},");
                Console.WriteLine("  });");
                Console.WriteLine();
                Console.WriteLine("  Or set PAYPAL_CLIENT_ID / PAYPAL_CLIENT_SECRET / PAYPAL_ENVIRONMENT environment variables and use 'new PayPalPartnerClient()'.");
                break;
        }

        Console.WriteLine();
        Console.WriteLine("Full docs: https://github.com/JoseModi97/paypal-partner-gateway-dotnet");
    }

    private static string Describe(DetectedProject project) => project.Kind switch
    {
        ProjectFrameworkKind.AspNetCoreWeb => "ASP.NET Core (Minimal API / MVC)",
        ProjectFrameworkKind.WorkerService => "Worker Service / Generic Host",
        ProjectFrameworkKind.Console => "Console application",
        ProjectFrameworkKind.ClassLibrary => "Class library",
        _ => "Unrecognized project type - defaulting to console-style usage",
    };

    private static Dictionary<string, string> ParseFlags(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var key = arg[2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    dict[key] = args[++i];
                }
                else
                {
                    dict[key] = "true";
                }
            }
            else if (arg.StartsWith('-'))
            {
                dict[arg[1..]] = "true";
            }
        }
        return dict;
    }

    private class ConfigOptions
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Environment { get; set; }
        public string? PartnerAttributionId { get; set; }
        public string? WebhookId { get; set; }
    }
}
