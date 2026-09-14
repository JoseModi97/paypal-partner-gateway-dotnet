#:package PayPal.PartnerGateway@1.0.1

// .NET 10 file-based app usage of PayPal.PartnerGateway - run directly with `dotnet run app.cs`,
// no .csproj required. This is exactly the project shape `paypal-partner init` auto-detects and
// wires up for you via #:package directives (see the "Automatic setup (CLI)" section of the
// top-level README).
//
// Uses PayPal's own PUBLIC Sandbox test credentials (published in PayPal's "PayPal Partner APIs"
// Postman collection for exactly this kind of try-it-out purpose) so this runs against a real
// Sandbox environment with zero setup. Swap in your own Client ID/Secret from
// https://developer.paypal.com/dashboard/applications for anything beyond quick experimentation.
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;

var client = new PayPalPartnerClient(new PayPalPartnerConfig
{
    ClientId = "ASJ7ZDtuc9fT8cDWY-EEt6xtnUCe0QiJa9Zn5Eme_rb3SfVIlbWZlu4KU87hKKFl8wxUCMRZ-HP49FTd",
    ClientSecret = "EN3AlrRVld1MnsZwYK1o2RJAxSAdv9DnrwNQCo3s3HOm45NAFQtzQhc4U5HsmwBpikVfX8uXUF7Ajb3t",
    Environment = PayPalEnvironment.Sandbox,
});

var order = await client.Orders.CreateAsync(new OrderRequest
{
    Intent = "CAPTURE",
    PurchaseUnits = new List<PurchaseUnit>
    {
        new()
        {
            Amount = new AmountWithBreakdown(10.00m, "USD"),
            Description = "Example order from paypal-partner-gateway-dotnet (file-based-app)",
        }
    }
});

if (!order.IsSuccess)
{
    Console.WriteLine($"Failed to create order: {order.Error?.Message}");
    return;
}

Console.WriteLine($"Order created: {order.Data!.Id}");
Console.WriteLine($"Open this URL and approve it as a Sandbox buyer: {order.Data.ApprovalUrl}");
Console.WriteLine("(Need a Sandbox buyer login? https://developer.paypal.com/dashboard/accounts)");
Console.Write("Press Enter once approved to capture the payment...");
Console.ReadLine();

var capture = await client.Orders.CaptureAsync(order.Data.Id);
Console.WriteLine(capture.IsSuccess
    ? $"Captured! Payment status: {capture.Data!.Status}"
    : $"Capture failed: {capture.Error?.Message}");
