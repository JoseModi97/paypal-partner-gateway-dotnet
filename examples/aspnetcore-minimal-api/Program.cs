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
    "<p><b>Redirect flow:</b> 1. POST /orders to create a Sandbox order. " +
    "2. Open the returned approvalUrl and approve it as a Sandbox buyer (developer.paypal.com/dashboard/accounts). " +
    "3. PayPal redirects the browser back to GET /orders/return, which completes the payment.</p>" +
    "<p><b>In-page flow:</b> open <a href=\"/checkout\">/checkout</a> - PayPal's own buttons pay without leaving the page.</p>",
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
// The "token" query param IS the order ID. CompleteAsync (not CaptureAsync) so a refresh of this
// page shows the payment again instead of failing with ORDER_ALREADY_CAPTURED.
app.MapGet("/orders/return", async (string token, PayPalPartnerClient client) =>
{
    var result = await client.Orders.CompleteAsync(token);
    if (!result.IsSuccess)
    {
        return Results.Content(result.Error?.HasIssue(PayPalIssues.InstrumentDeclined) == true
            ? "<h3>Payment declined</h3><p>Nothing was charged. Please try again with another card or PayPal.</p>"
            : $"<h3>Payment failed</h3><p>{result.Error?.Issue ?? result.Error?.Message}</p>", "text/html");
    }

    // IsPaid, not Status == "COMPLETED": the capture itself can still be PENDING or DECLINED.
    return Results.Content(result.Data!.IsPaid
        ? $"<h3>Payment successful!</h3><p>Capture {result.Data.Capture!.Id}: {result.Data.Capture.Amount?.Value} {result.Data.Capture.Amount?.CurrencyCode}</p>"
        : $"<h3>Payment not completed yet</h3><p>Order {result.Data.Status}, capture {result.Data.Capture?.Status ?? "none"}.</p>",
        "text/html");
});

app.MapGet("/orders/cancel", () =>
    Results.Content("<h3>Payment cancelled</h3><p>The buyer backed out before approving.</p>", "text/html"));

app.MapGet("/orders/{orderId}", async (string orderId, PayPalPartnerClient client) =>
{
    var order = await client.Orders.GetAsync(orderId);
    return order.IsSuccess ? Results.Ok(order.Data) : Results.NotFound(new { error = order.Error?.Message });
});

// Call this only after a Sandbox buyer has approved the order at its approvalUrl. Safe to call
// more than once: it captures an APPROVED order (never twice) and otherwise reports where it stands.
app.MapPost("/orders/{orderId}/capture", async (string orderId, PayPalPartnerClient client) =>
{
    var result = await client.Orders.CompleteAsync(orderId);
    return result.IsSuccess
        ? Results.Ok(new { paid = result.Data!.IsPaid, status = result.Data.Status, captureId = result.Data.Capture?.Id })
        : Results.BadRequest(new { issue = result.Error?.Issue, error = result.Error?.Message });
});

// ---- Paying without leaving your page ----
// PayPal's approval page can't be shown in an <iframe>/modal: browsers block its sign-in cookies
// inside another site's frame, and buyers get "Sorry, something went wrong" after logging in.
// PayPal's JS SDK buttons are the supported in-page alternative: "Debit or Credit Card" opens its
// form right in your page, and the PayPal button signs in through PayPal's small popup window.
// Your server still creates the order (createOrder) and captures it (onApprove -> CompleteAsync).
app.MapGet("/checkout", (PayPalPartnerClient client) => Results.Content($$"""
    <!doctype html>
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Pay in page - PayPal.PartnerGateway</title>
    <body style="font-family:system-ui;max-width:28rem;margin:2rem auto;padding:0 1rem">
      <h3>Pay USD 10.00 without leaving this page</h3>
      <div id="paypal-buttons"></div>
      <p id="result"></p>
      <!-- The client ID is public by design; the secret never leaves your server.
           buyer-country is Sandbox-only: it makes the card button show wherever you test from. -->
      <script src="https://www.paypal.com/sdk/js?client-id={{client.Gateway.Config.ClientId}}&currency=USD&intent=capture&enable-funding=card{{(client.Gateway.Config.Environment == PayPalEnvironment.Sandbox ? "&buyer-country=US" : "")}}"></script>
      <script>
        const result = document.getElementById('result');
        paypal.Buttons({
          // Your server creates the order; the buttons only need its ID.
          createOrder: async () => (await (await fetch('/checkout/orders', { method: 'POST' })).json()).orderId,
          onApprove: async ({ orderID }) => {
            // Show a "confirming" state here rather than leaving the buttons clickable.
            result.textContent = 'Confirming your payment...';
            const r = await (await fetch(`/orders/${orderID}/capture`, { method: 'POST' })).json();
            result.textContent = r.paid ? `Paid! Capture ${r.captureId}`
              : r.issue === 'INSTRUMENT_DECLINED' ? 'Declined - nothing was charged. Try another card.'
              : `Not completed: ${r.issue || r.status}`;
          },
          onCancel: () => result.textContent = 'Cancelled - nothing was charged.',
          onError: err => result.textContent = 'PayPal error: ' + err
        }).render('#paypal-buttons');
      </script>
    </body>
    """, "text/html"));

// Orders for the in-page buttons: no return_url needed, the buyer never leaves the page.
app.MapPost("/checkout/orders", async (PayPalPartnerClient client) =>
{
    var order = await client.Orders.CreateAsync(new OrderRequest
    {
        Intent = "CAPTURE",
        PurchaseUnits = new List<PurchaseUnit>
        {
            new() { Amount = new AmountWithBreakdown(10.00m, "USD"), Description = "In-page checkout example" }
        },
        // Without this the card form also asks for a full shipping address.
        ApplicationContext = new JsonObject { ["shipping_preference"] = "NO_SHIPPING" },
    });
    return order.IsSuccess
        ? Results.Ok(new { orderId = order.Data!.Id })
        : Results.BadRequest(new { error = order.Error?.Message });
});

app.Run();
