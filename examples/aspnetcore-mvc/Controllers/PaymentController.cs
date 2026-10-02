using Microsoft.AspNetCore.Mvc;
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;
using System.Text.Json.Nodes;

namespace AspNetCoreMvcExample.Controllers;

[ApiController]
[Route("[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PayPalPartnerClient _client;

    public PaymentController(PayPalPartnerClient client)
    {
        _client = client;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder()
    {
        // Without application_context.return_url/cancel_url, PayPal's hosted approval page has
        // nowhere to send the buyer back to after they click Pay Now - they're just left on
        // PayPal's own page with no visible confirmation, even though the approval itself
        // succeeded. Setting both is what makes the redirect (and the confirmation the buyer
        // expects) actually happen.
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var order = await _client.Orders.CreateAsync(new OrderRequest
        {
            Intent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnit>
            {
                new()
                {
                    Amount = new AmountWithBreakdown(10.00m, "USD"),
                    Description = "Example order from paypal-partner-gateway-dotnet (aspnetcore-mvc)",
                }
            },
            ApplicationContext = new JsonObject
            {
                ["return_url"] = $"{baseUrl}/Payment/orders/return",
                ["cancel_url"] = $"{baseUrl}/Payment/orders/cancel",
                ["user_action"] = "PAY_NOW",
                ["shipping_preference"] = "NO_SHIPPING",
            }
        });

        if (!order.IsSuccess)
        {
            return BadRequest(new { error = order.Error?.Message, details = order.Error?.Details });
        }

        return Ok(new { orderId = order.Data!.Id, status = order.Data.Status, approvalUrl = order.Data.ApprovalUrl });
    }

    // PayPal redirects the buyer's browser here after approval, appending ?token={orderId}&PayerID=...
    // The "token" query param IS the order ID - no need to have stored it yourself. CompleteAsync
    // (not CaptureAsync) so a refresh of this page shows the payment instead of failing with
    // ORDER_ALREADY_CAPTURED.
    [HttpGet("orders/return")]
    public async Task<IActionResult> ReturnFromApproval([FromQuery] string token)
    {
        var result = await _client.Orders.CompleteAsync(token);
        if (!result.IsSuccess)
        {
            return Content(result.Error?.HasIssue(PayPalIssues.InstrumentDeclined) == true
                ? "<h3>Payment declined</h3><p>Nothing was charged. Please try again with another card or PayPal.</p>"
                : $"<h3>Payment failed</h3><p>{result.Error?.Issue ?? result.Error?.Message}</p>", "text/html");
        }

        // IsPaid, not Status == "COMPLETED": the capture itself can still be PENDING or DECLINED.
        return Content(result.Data!.IsPaid
            ? $"<h3>Payment successful!</h3><p>Capture {result.Data.Capture!.Id}: {result.Data.Capture.Amount?.Value} {result.Data.Capture.Amount?.CurrencyCode}</p>"
            : $"<h3>Payment not completed yet</h3><p>Order {result.Data.Status}, capture {result.Data.Capture?.Status ?? "none"}.</p>",
            "text/html");
    }

    [HttpGet("orders/cancel")]
    public IActionResult CancelApproval() =>
        Content("<h3>Payment cancelled</h3><p>The buyer backed out before approving.</p>", "text/html");

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(string orderId)
    {
        var order = await _client.Orders.GetAsync(orderId);
        return order.IsSuccess ? Ok(order.Data) : NotFound(new { error = order.Error?.Message });
    }

    // Call this after a Sandbox buyer has approved the order at its approvalUrl. Safe to call more
    // than once: it captures an APPROVED order (never twice) and otherwise reports where it stands.
    [HttpPost("orders/{orderId}/capture")]
    public async Task<IActionResult> CaptureOrder(string orderId)
    {
        var result = await _client.Orders.CompleteAsync(orderId);
        return result.IsSuccess
            ? Ok(new { paid = result.Data!.IsPaid, status = result.Data.Status, captureId = result.Data.Capture?.Id })
            : BadRequest(new { issue = result.Error?.Issue, error = result.Error?.Message });
    }
}
