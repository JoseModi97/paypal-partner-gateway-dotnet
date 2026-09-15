// ASP.NET Core Minimal API usage of PayPal.PartnerGateway.
//
// appsettings.json ships with PayPal's own PUBLIC Sandbox test credentials (see the comment in
// that file) so this runs against a real Sandbox environment with zero setup. Swap in your own
// from https://developer.paypal.com/dashboard/applications for anything beyond quick experimentation.
using System.Text.Json.Nodes;
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.AspNetCore;
using PayPal.PartnerGateway.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPayPalPartnerGateway(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Content(
    "<h3>PayPal.PartnerGateway - Minimal API example</h3>" +
    "<p>1. POST /orders to create a Sandbox order.</p>" +
    "<p>2. Open the returned approvalUrl and approve it as a Sandbox buyer (developer.paypal.com/dashboard/accounts).</p>" +
    "<p>3. PayPal redirects the browser back to GET /orders/return, which captures automatically.</p>",
    "text/html"));

app.MapPost("/orders", async (HttpRequest request, PayPalPartnerClient client) =>
{
    // Without application_context.return_url/cancel_url, PayPal's hosted approval page has
    // nowhere to send the buyer back to after they click Pay Now - they're just left on
    // PayPal's own page with no visible confirmation, even though the approval itself
    // succeeded. Setting both is what makes the redirect (and the confirmation the buyer
    // expects) actually happen.
    var baseUrl = $"{request.Scheme}://{request.Host}";

    var order = await client.Orders.CreateAsync(new OrderRequest
    {
        Intent = "CAPTURE",
        PurchaseUnits = new List<PurchaseUnit>
        {
            new()
            {
                Amount = new AmountWithBreakdown(10.00m, "USD"),
                Description = "Example order from paypal-partner-gateway-dotnet (aspnetcore-minimal-api)",
            }
        },
        ApplicationContext = new JsonObject
        {
            ["return_url"] = $"{baseUrl}/orders/return",
            ["cancel_url"] = $"{baseUrl}/orders/cancel",
            ["user_action"] = "PAY_NOW",
            ["shipping_preference"] = "NO_SHIPPING",
        }
    });

    return order.IsSuccess
        ? Results.Ok(new { orderId = order.Data!.Id, status = order.Data.Status, approvalUrl = order.Data.ApprovalUrl })
        : Results.BadRequest(new { error = order.Error?.Message, details = order.Error?.Details });
});

// PayPal redirects the buyer's browser here after approval, appending ?token={orderId}&PayerID=...
// The "token" query param IS the order ID - capture it directly, no need to have stored it yourself.
app.MapGet("/orders/return", async (string token, PayPalPartnerClient client) =>
{
    var capture = await client.Orders.CaptureAsync(token);
    return capture.IsSuccess
        ? Results.Content($"<h3>Payment successful!</h3><p>Status: {capture.Data!.Status}</p>", "text/html")
        : Results.Content($"<h3>Capture failed</h3><p>{capture.Error?.Message}</p>", "text/html");
});

app.MapGet("/orders/cancel", () =>
    Results.Content("<h3>Payment cancelled</h3><p>The buyer backed out before approving.</p>", "text/html"));

app.MapGet("/orders/{orderId}", async (string orderId, PayPalPartnerClient client) =>
{
    var order = await client.Orders.GetAsync(orderId);
    return order.IsSuccess ? Results.Ok(order.Data) : Results.NotFound(new { error = order.Error?.Message });
});

// Call this only after a Sandbox buyer has approved the order at its approvalUrl - capturing
// before approval fails with an ORDER_NOT_APPROVED error, which is expected, not a bug.
app.MapPost("/orders/{orderId}/capture", async (string orderId, PayPalPartnerClient client) =>
{
    var capture = await client.Orders.CaptureAsync(orderId);
    return capture.IsSuccess
        ? Results.Ok(capture.Data)
        : Results.BadRequest(new { error = capture.Error?.Message, details = capture.Error?.Details });
});

app.Run();
