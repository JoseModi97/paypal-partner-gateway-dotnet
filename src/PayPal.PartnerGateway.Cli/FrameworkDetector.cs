using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PayPal.PartnerGateway.Cli;

/// <summary>
/// Figures out what kind of .NET project lives in a directory - ASP.NET Core web app, worker
/// service, console app, or class library - so <c>init</c> can wire up the right package(s) and
/// registration style without the user having to tell us via a <c>--framework</c> flag.
/// </summary>
public static class FrameworkDetector
{
    /// <summary>Scans <paramref name="directory"/> for a single .csproj and classifies it. Returns null if none is found.</summary>
    public static DetectedProject? Detect(string directory)
    {
        var csprojPath = Directory.GetFiles(directory, "*.csproj").FirstOrDefault();
        if (csprojPath == null) return null;

        XDocument doc;
        try
        {
            doc = XDocument.Load(csprojPath);
        }
        catch (Exception)
        {
            return new DetectedProject { CsprojPath = csprojPath, Kind = ProjectFrameworkKind.Unknown };
        }

        var sdk = doc.Root?.Attribute("Sdk")?.Value ?? string.Empty;
        var outputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;
        var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value
            ?? doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value?.Split(';').FirstOrDefault()
            ?? "net8.0";

        var referencesAspNetCore = doc.Descendants("FrameworkReference")
            .Any(e => string.Equals(e.Attribute("Include")?.Value, "Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase));

        var kind = ClassifyFromProjectFile(sdk, outputType, referencesAspNetCore);

        // Program.cs content is a strong secondary signal - useful when the .csproj alone is
        // ambiguous (e.g. a plain Sdk="Microsoft.NET.Sdk" console-shaped worker/minimal API host).
        if (kind is ProjectFrameworkKind.Console or ProjectFrameworkKind.Unknown)
        {
            var programCsPath = Path.Combine(directory, "Program.cs");
            if (File.Exists(programCsPath))
            {
                kind = ClassifyFromProgramSource(File.ReadAllText(programCsPath)) ?? kind;
            }
        }

        return new DetectedProject
        {
            CsprojPath = csprojPath,
            Kind = kind,
            TargetFramework = targetFramework,
        };
    }

    private static ProjectFrameworkKind ClassifyFromProjectFile(string sdk, string? outputType, bool referencesAspNetCore)
    {
        if (referencesAspNetCore || sdk.IndexOf("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return ProjectFrameworkKind.AspNetCoreWeb;
        }

        if (sdk.IndexOf("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return ProjectFrameworkKind.WorkerService;
        }

        if (string.Equals(outputType, "Exe", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectFrameworkKind.Console;
        }

        if (sdk.IndexOf("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return ProjectFrameworkKind.ClassLibrary;
        }

        return ProjectFrameworkKind.Unknown;
    }

    private static ProjectFrameworkKind? ClassifyFromProgramSource(string source)
    {
        if (source.Contains("WebApplication.CreateBuilder")) return ProjectFrameworkKind.AspNetCoreWeb;
        if (source.Contains("Host.CreateApplicationBuilder") || source.Contains("Host.CreateDefaultBuilder")) return ProjectFrameworkKind.WorkerService;
        return null;
    }
}
