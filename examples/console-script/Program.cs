// Standalone console usage of PayPal.PartnerGateway - no DI, no ASP.NET Core, just `new PayPalPartnerClient(...)`.
//
// Uses PayPal's own public Sandbox test credentials (published in PayPal's "PayPal Partner APIs"
// Postman collection for exactly this kind of try-it-out purpose) so this example runs against a
// real Sandbox environment with zero setup. Swap in your own Client ID/Secret from
// https://developer.paypal.com/dashboard/applications for anything beyond quick experimentation.
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;

var client = new PayPalPartnerClient(new PayPalPartnerConfig
{
    ClientId = "ASJ7ZDtuc9fT8cDWY-EEt6xtnUCe0QiJa9Zn5Eme_rb3SfVIlbWZlu4KU87hKKFl8wxUCMRZ-HP49FTd",
    ClientSecret = "EN3AlrRVld1MnsZwYK1o2RJAxSAdv9DnrwNQCo3s3HOm45NAFQtzQhc4U5HsmwBpikVfX8uXUF7Ajb3t",
    Environment = PayPalEnvironment.Sandbox,
});

Console.WriteLine("Creating a Sandbox order...");

var order = await client.Orders.CreateAsync(new OrderRequest
{
    Intent = "CAPTURE",
    PurchaseUnits = new List<PurchaseUnit>
    {
        new()
        {
            Amount = new AmountWithBreakdown(10.00m, "USD"),
            Description = "Example order from paypal-partner-gateway-dotnet (console-script)",
        }
    }
});

if (!order.IsSuccess)
{
    Console.WriteLine($"Failed to create order: {order.StatusCode} {order.Error?.Name} - {order.Error?.Message}");
    return;
}

Console.WriteLine($"Order created: {order.Data!.Id} (status: {order.Data.Status})");
Console.WriteLine();
Console.WriteLine($"1. Open this URL and approve it as a Sandbox buyer:");
Console.WriteLine($"   {order.Data.ApprovalUrl}");
Console.WriteLine();
Console.WriteLine("   Need a Sandbox buyer login? Get one from your own PayPal Developer account:");
Console.WriteLine("   https://developer.paypal.com/dashboard/accounts (use a 'Personal' test account).");
Console.WriteLine();
Console.Write("2. Once you've approved it in the browser, press Enter here to capture the payment...");
Console.ReadLine();

var capture = await client.Orders.CaptureAsync(order.Data.Id);

if (capture.IsSuccess)
{
    Console.WriteLine($"Captured! Payment status: {capture.Data!.Status}");
}
else
{
    Console.WriteLine($"Capture failed: {capture.Error?.Name} - {capture.Error?.Message}");
    Console.WriteLine("(If this says ORDER_NOT_APPROVED, the order wasn't actually approved in the browser yet.)");
}
