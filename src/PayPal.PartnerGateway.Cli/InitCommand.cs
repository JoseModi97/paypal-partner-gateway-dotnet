using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PayPal.PartnerGateway.Cli;

/// <summary>
/// Implements <c>paypal-partner init</c>: detects the .NET project in the current directory,
/// adds the right PayPal.PartnerGateway package(s) for it, wires up configuration for
/// DI-capable hosts, and prints a ready-to-paste usage snippet - all without asking the user
/// what kind of project they're in.
/// </summary>
public static class InitCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var options = ParseArgs(args);
        var directory = Directory.GetCurrentDirectory();

        var project = FrameworkDetector.Detect(directory);
        if (project == null)
        {
            Console.Error.WriteLine("No .csproj found in the current directory. Run 'paypal-partner init' from your project's root folder.");
            return 1;
        }

        Console.WriteLine($"Project:   {Path.GetFileName(project.CsprojPath)}");
        Console.WriteLine($"Framework: {Describe(project.Kind)} ({project.TargetFramework})");
        Console.WriteLine();

        var packages = new List<string> { "PayPal.PartnerGateway" };
        if (project.HasDependencyInjection)
        {
            packages.Add("PayPal.PartnerGateway.AspNetCore");
        }

        if (options.DryRun)
        {
            Console.WriteLine("(--dry-run) Would add: " + string.Join(", ", packages));
        }
        else
        {
            foreach (var package in packages)
            {
                Console.WriteLine($"Adding {package}...");
                var exitCode = await ProcessRunner.RunAsync("dotnet", $"add \"{project.CsprojPath}\" package {package}").ConfigureAwait(false);
                if (exitCode != 0)
                {
                    Console.Error.WriteLine($"  'dotnet add package {package}' failed (exit {exitCode}). Add it manually and re-run if needed.");
                }
            }
        }

        if (project.HasDependencyInjection && !options.DryRun)
        {
            WriteAppSettings(directory, options);
        }

        Console.WriteLine();
        PrintNextSteps(project.Kind, options);

        return 0;
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

    private static void PrintNextSteps(ProjectFrameworkKind kind, InitOptions options)
    {
        Console.WriteLine("Next steps:");
        Console.WriteLine();

        switch (kind)
        {
            case ProjectFrameworkKind.AspNetCoreWeb:
                Console.WriteLine("  builder.Services.AddPayPalPartnerGateway(builder.Configuration);");
                Console.WriteLine();
                Console.WriteLine("  app.MapPayPalPartnerWebhook(\"/webhooks/paypal\", onSuccess: async (result, ctx) =>");
                Console.WriteLine("  {");
                Console.WriteLine("      // handle result.Event");
                Console.WriteLine("  });");
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
