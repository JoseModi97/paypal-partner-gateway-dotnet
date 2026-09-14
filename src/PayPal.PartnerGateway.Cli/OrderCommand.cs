using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Cli;

/// <summary>
/// Implements <c>paypal-partner order create|get|capture</c>: calls the real Orders API directly
/// from the terminal, using whatever credentials it can find - explicit flags, environment
/// variables, or the "PayPalPartner" section of an appsettings.json in the current directory
/// (the same file <c>init</c> writes) - so you don't need to write any code just to try the API.
/// </summary>
public static class OrderCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: paypal-partner order <create|get|capture> [options]");
            return 1;
        }

        var sub = args[0];
        var rest = args.Length > 1 ? args[1..] : Array.Empty<string>();
        var flags = ParseFlags(rest);

        PayPalPartnerConfig config;
        try
        {
            config = ResolveConfig(flags);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var client = new PayPalPartnerClient(config);

        return sub.ToLowerInvariant() switch
        {
            "create" => await CreateAsync(client, flags),
            "get" => await GetAsync(client, rest),
            "capture" => await CaptureAsync(client, rest),
            _ => Unknown(sub),
        };
    }

    private static async Task<int> CreateAsync(PayPalPartnerClient client, Dictionary<string, string> flags)
    {
        var amount = flags.GetValueOrDefault("amount") ?? "10.00";
        var currency = flags.GetValueOrDefault("currency") ?? "USD";
        var description = flags.GetValueOrDefault("description") ?? "Order created via paypal-partner CLI";

        if (!decimal.TryParse(amount, out var amountValue))
        {
            Console.Error.WriteLine($"--amount '{amount}' is not a valid number.");
            return 1;
        }

        var order = await client.Orders.CreateAsync(new OrderRequest
        {
            Intent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnit>
            {
                new() { Amount = new AmountWithBreakdown(amountValue, currency), Description = description }
            }
        });

        if (!order.IsSuccess)
        {
            Console.Error.WriteLine($"Failed to create order: {order.Error?.Name} - {order.Error?.Message}");
            return 1;
        }

        Console.WriteLine($"Order ID:      {order.Data!.Id}");
        Console.WriteLine($"Status:        {order.Data.Status}");
        Console.WriteLine($"Approval URL:  {order.Data.ApprovalUrl}");
        Console.WriteLine();
        Console.WriteLine("Open the approval URL, log in as a Sandbox buyer (developer.paypal.com/dashboard/accounts),");
        Console.WriteLine("click Pay Now, then run:");
        Console.WriteLine($"  paypal-partner order capture {order.Data.Id}");
        return 0;
    }

    private static async Task<int> GetAsync(PayPalPartnerClient client, string[] rest)
    {
        if (rest.Length == 0 || rest[0].StartsWith('-'))
        {
            Console.Error.WriteLine("Usage: paypal-partner order get <orderId>");
            return 1;
        }

        var order = await client.Orders.GetAsync(rest[0]);
        if (!order.IsSuccess)
        {
            Console.Error.WriteLine($"Failed to fetch order: {order.Error?.Message}");
            return 1;
        }

        Console.WriteLine(JsonSerializer.Serialize(order.Data, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static async Task<int> CaptureAsync(PayPalPartnerClient client, string[] rest)
    {
        if (rest.Length == 0 || rest[0].StartsWith('-'))
        {
            Console.Error.WriteLine("Usage: paypal-partner order capture <orderId>");
            return 1;
        }

        var capture = await client.Orders.CaptureAsync(rest[0]);
        if (!capture.IsSuccess)
        {
            Console.Error.WriteLine($"Capture failed: {capture.Error?.Name} - {capture.Error?.Message}");
            Console.Error.WriteLine("(ORDER_NOT_APPROVED means the buyer hasn't approved it at the approval URL yet.)");
            return 1;
        }

        Console.WriteLine($"Captured! Status: {capture.Data!.Status}");
        return 0;
    }

    /// <summary>
    /// Resolves credentials in priority order: explicit flags, then PAYPAL_* environment
    /// variables, then the "PayPalPartner" section of an appsettings.json in the current
    /// directory (the same file 'init' writes) - so 'order create' just works right after
    /// running 'init' in a project, with no extra flags needed.
    /// </summary>
    private static PayPalPartnerConfig ResolveConfig(Dictionary<string, string> flags)
    {
        var fromAppSettings = ExistingSetup.ReadAppSettings(Directory.GetCurrentDirectory());

        var clientId = flags.GetValueOrDefault("client-id")
            ?? Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID")
            ?? fromAppSettings?.ClientId;

        var clientSecret = flags.GetValueOrDefault("client-secret")
            ?? Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET")
            ?? fromAppSettings?.ClientSecret;

        var environmentName = flags.GetValueOrDefault("environment")
            ?? Environment.GetEnvironmentVariable("PAYPAL_ENVIRONMENT")
            ?? fromAppSettings?.Environment
            ?? "Sandbox";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "No PayPal credentials found. Pass --client-id/--client-secret, set PAYPAL_CLIENT_ID/" +
                "PAYPAL_CLIENT_SECRET, or run 'paypal-partner init' in this directory first.");
        }

        return new PayPalPartnerConfig
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Environment = environmentName.Equals("Live", StringComparison.OrdinalIgnoreCase)
                ? PayPalEnvironment.Live
                : PayPalEnvironment.Sandbox,
        };
    }

    private static int Unknown(string sub)
    {
        Console.Error.WriteLine($"Unknown 'order' subcommand '{sub}'. Use create, get, or capture.");
        return 1;
    }

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
        }
        return dict;
    }
}
