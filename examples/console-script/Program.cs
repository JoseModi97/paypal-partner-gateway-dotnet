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
Console.WriteLine($"Approval URL:  {order.Data.ApprovalUrl}");

Console.WriteLine();
Console.WriteLine("Fetching the order back by ID to confirm the round trip...");

var fetched = await client.Orders.GetAsync(order.Data.Id);

Console.WriteLine(fetched.IsSuccess
    ? $"Confirmed: order {fetched.Data!.Id} is currently '{fetched.Data.Status}'."
    : $"Failed to fetch order: {fetched.Error?.Message}");
