using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;

namespace WorkerServiceExample;

/// <summary>
/// Injects PayPalPartnerClient into a BackgroundService exactly like any other DI-registered
/// dependency - creates one Sandbox order on startup and logs the result, then stops.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly PayPalPartnerClient _client;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, PayPalPartnerClient client, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _client = client;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var order = await _client.Orders.CreateAsync(new OrderRequest
        {
            Intent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnit>
            {
                new()
                {
                    Amount = new AmountWithBreakdown(10.00m, "USD"),
                    Description = "Example order from paypal-partner-gateway-dotnet (worker-service)",
                }
            }
        }, cancellationToken: stoppingToken);

        if (order.IsSuccess)
        {
            _logger.LogInformation(
                "Created Sandbox order {OrderId} (status: {Status}). Approval URL: {ApprovalUrl} " +
                "- a headless worker can't pause for browser approval, so this demo stops at order " +
                "creation; see the console-script or file-based-app examples for the full " +
                "create -> approve -> capture flow.",
                order.Data!.Id, order.Data.Status, order.Data.ApprovalUrl);
        }
        else
        {
            _logger.LogError("Failed to create order: {Error}", order.Error?.Message);
        }

        _lifetime.StopApplication();
    }
}
