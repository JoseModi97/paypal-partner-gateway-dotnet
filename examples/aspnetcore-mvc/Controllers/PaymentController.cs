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
    // The "token" query param IS the order ID - capture it directly, no need to have stored it yourself.
    [HttpGet("orders/return")]
    public async Task<IActionResult> ReturnFromApproval([FromQuery] string token)
    {
        var capture = await _client.Orders.CaptureAsync(token);
        return capture.IsSuccess
            ? Content($"<h3>Payment successful!</h3><p>Status: {capture.Data!.Status}</p>", "text/html")
            : Content($"<h3>Capture failed</h3><p>{capture.Error?.Message}</p>", "text/html");
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

    // Call this only after a Sandbox buyer has approved the order at its approvalUrl - capturing
    // before approval fails with an ORDER_NOT_APPROVED error, which is expected, not a bug.
    [HttpPost("orders/{orderId}/capture")]
    public async Task<IActionResult> CaptureOrder(string orderId)
    {
        var capture = await _client.Orders.CaptureAsync(orderId);
        return capture.IsSuccess
            ? Ok(capture.Data)
            : BadRequest(new { error = capture.Error?.Message, details = capture.Error?.Details });
    }
}
