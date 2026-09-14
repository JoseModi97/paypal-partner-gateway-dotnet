using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PayPal.PartnerGateway.Cli;

/// <summary>
/// Implements <c>paypal-partner init</c>: detects the .NET project (or .NET 10+ file-based app)
/// in the current directory, adds the right PayPal.PartnerGateway package(s) for it the way that
/// project actually consumes packages (<c>dotnet add package</c>, or a <c>#:package</c> directive
/// for a file-based app), wires up configuration for DI-capable hosts, and prints a ready-to-paste
/// usage snippet - all without asking the user what kind of project they're in.
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
        var options = ParseArgs(args);
        var directory = Directory.GetCurrentDirectory();

        var project = FrameworkDetector.Detect(directory);
        if (project == null)
        {
            Console.Error.WriteLine("No .csproj and no single .cs file found in the current directory.");
            Console.Error.WriteLine("Run 'paypal-partner init' from your project's root folder, or a folder containing one file-based app (e.g. app.cs).");
            return 1;
        }

        Console.WriteLine($"Entry point: {Path.GetFileName(project.ProjectPath)}{(project.IsFileBasedApp ? " (file-based app, no .csproj)" : "")}");
        Console.WriteLine($"Framework:   {Describe(project.Kind)} ({project.TargetFramework})");
        Console.WriteLine();

        var packages = new List<string> { "PayPal.PartnerGateway" };
        if (project.HasDependencyInjection)
        {
            packages.Add("PayPal.PartnerGateway.AspNetCore");
        }

        if (options.DryRun)
        {
            var via = project.IsFileBasedApp ? "#:package directives" : "dotnet add package";
            Console.WriteLine($"(--dry-run) Would add via {via}: " + string.Join(", ", packages));
        }
        else if (project.IsFileBasedApp)
        {
            InstallIntoFileBasedApp(project, packages);
        }
        else
        {
            await InstallViaDotnetAddPackageAsync(project, packages).ConfigureAwait(false);
        }

        if (project.HasDependencyInjection && !project.IsFileBasedApp && !options.DryRun)
        {
            WriteAppSettings(directory, options);
        }

        Console.WriteLine();
        PrintNextSteps(project, options);

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

        if (directivesToAdd.Count == 0)
        {
            return;
        }

        // Directives must come before any other code, but after any directives already present.
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

    private static void WriteAppSettings(string directory, InitOptions options)
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
            ["ClientId"] = options.ClientId ?? "YOUR_SANDBOX_CLIENT_ID",
            ["ClientSecret"] = options.ClientSecret ?? "YOUR_SANDBOX_CLIENT_SECRET",
            ["Environment"] = options.Environment ?? "Sandbox",
        };

        root["PayPalPartner"] = section;

        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        Console.WriteLine($"Wrote PayPalPartner section to {Path.GetFileName(path)}.");
    }

    private static void PrintNextSteps(DetectedProject project, InitOptions options)
    {
        Console.WriteLine("Next steps:");
        Console.WriteLine();

        switch (project.Kind)
        {
            case ProjectFrameworkKind.AspNetCoreWeb:
                Console.WriteLine("  builder.Services.AddPayPalPartnerGateway(builder.Configuration);");
                Console.WriteLine();
                Console.WriteLine("  app.MapPayPalPartnerWebhook(\"/webhooks/paypal\", onSuccess: async (result, ctx) =>");
                Console.WriteLine("  {");
                Console.WriteLine("      // handle result.Event");
                Console.WriteLine("  });");
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
                Console.WriteLine("  var client = new PayPal.PartnerGateway.PayPalPartnerClient(new PayPal.PartnerGateway.PayPalPartnerConfig");
                Console.WriteLine("  {");
                Console.WriteLine($"      ClientId = \"{options.ClientId ?? "YOUR_CLIENT_ID"}\",");
                Console.WriteLine($"      ClientSecret = \"{options.ClientSecret ?? "YOUR_CLIENT_SECRET"}\",");
                Console.WriteLine($"      Environment = PayPal.PartnerGateway.PayPalEnvironment.{options.Environment ?? "Sandbox"},");
                Console.WriteLine("  });");
                Console.WriteLine();
                Console.WriteLine("  Or set PAYPAL_CLIENT_ID / PAYPAL_CLIENT_SECRET / PAYPAL_ENVIRONMENT environment variables and use 'new PayPalPartnerClient()'.");
                break;
        }

        Console.WriteLine();
        Console.WriteLine("Full docs: https://github.com/JoseModi97/paypal-partner-gateway-dotnet");
    }

    private static string Describe(ProjectFrameworkKind kind) => kind switch
    {
        ProjectFrameworkKind.AspNetCoreWeb => "ASP.NET Core (Minimal API / MVC)",
        ProjectFrameworkKind.WorkerService => "Worker Service / Generic Host",
        ProjectFrameworkKind.Console => "Console application",
        ProjectFrameworkKind.ClassLibrary => "Class library",
        _ => "Unrecognized project type - defaulting to console-style usage",
    };

    private static InitOptions ParseArgs(string[] args)
    {
        var options = new InitOptions();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--client-id" when i + 1 < args.Length:
                    options.ClientId = args[++i];
                    break;
                case "--client-secret" when i + 1 < args.Length:
                    options.ClientSecret = args[++i];
                    break;
                case "--environment" when i + 1 < args.Length:
                    options.Environment = args[++i];
                    break;
                case "--dry-run":
                    options.DryRun = true;
                    break;
            }
        }

        return options;
    }

    private class InitOptions
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Environment { get; set; }
        public bool DryRun { get; set; }
    }
}
