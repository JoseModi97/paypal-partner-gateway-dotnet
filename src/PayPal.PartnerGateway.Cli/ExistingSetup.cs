using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace PayPal.PartnerGateway.Cli;

/// <summary>
/// Detects whether this project (or file-based app) already went through 'init' before, the way
/// Yii's Gii code generator checks for an existing file before regenerating it. Re-running the
/// wizard should pick up what's already there as defaults instead of presenting a blank slate, and
/// should ask before clobbering anything that already exists.
/// </summary>
internal static class ExistingSetup
{
    /// <summary>Reads the "PayPalPartner" section of appsettings.json, if present.</summary>
    public static SetupConfig? ReadAppSettings(string directory)
    {
        var path = Path.Combine(directory, "appsettings.json");
        if (!File.Exists(path)) return null;

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
            var section = root?["PayPalPartner"] as JsonObject;
            if (section == null) return null;

            return new SetupConfig
            {
                ClientId = section["ClientId"]?.ToString(),
                ClientSecret = section["ClientSecret"]?.ToString(),
                Environment = section["Environment"]?.ToString(),
                PartnerAttributionId = section["PartnerAttributionId"]?.ToString(),
                WebhookId = section["WebhookId"]?.ToString(),
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>True when a file-based app's directives already reference <paramref name="packageId"/>.</summary>
    public static bool FileBasedAppReferencesPackage(string entryFilePath, string packageId)
    {
        if (!File.Exists(entryFilePath)) return false;

        return File.ReadLines(entryFilePath).Any(line =>
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith($"#:package {packageId}@", StringComparison.OrdinalIgnoreCase)
                   || trimmed.Equals($"#:package {packageId}", StringComparison.OrdinalIgnoreCase);
        });
    }
}
