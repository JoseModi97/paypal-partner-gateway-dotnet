using Microsoft.AspNetCore.Mvc;
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;

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
            }
        });

        if (!order.IsSuccess)
        {
            return BadRequest(new { error = order.Error?.Message, details = order.Error?.Details });
        }

        return Ok(new { orderId = order.Data!.Id, status = order.Data.Status, approvalUrl = order.Data.ApprovalUrl });
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(string orderId)
    {
        var order = await _client.Orders.GetAsync(orderId);
        return order.IsSuccess ? Ok(order.Data) : NotFound(new { error = order.Error?.Message });
    }
}
