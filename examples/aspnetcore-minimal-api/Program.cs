// ASP.NET Core Minimal API usage of PayPal.PartnerGateway.
//
// appsettings.json ships with PayPal's own PUBLIC Sandbox test credentials (see the comment in
// that file) so this runs against a real Sandbox environment with zero setup. Swap in your own
// from https://developer.paypal.com/dashboard/applications for anything beyond quick experimentation.
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
    "<p>3. POST /orders/{id}/capture to actually complete the payment.</p>",
    "text/html"));

app.MapPost("/orders", async (PayPalPartnerClient client) =>
{
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
        }
    });

    return order.IsSuccess
        ? Results.Ok(new { orderId = order.Data!.Id, status = order.Data.Status, approvalUrl = order.Data.ApprovalUrl })
        : Results.BadRequest(new { error = order.Error?.Message, details = order.Error?.Details });
});

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
