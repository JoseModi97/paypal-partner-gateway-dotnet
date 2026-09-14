using PayPal.PartnerGateway;
using PayPal.PartnerGateway.AspNetCore;
using PayPal.PartnerGateway.Models;

var builder = WebApplication.CreateBuilder(args);

// Register the PayPal Partner Gateway client, reading the "PayPalPartner" section of appsettings.json.
builder.Services.AddPayPalPartnerGateway(builder.Configuration);

var app = builder.Build();

// 1. Start onboarding a new connected seller (3rd-party flow) and redirect them to PayPal to finish signup.
app.MapGet("/sellers/onboard", async (HttpContext context, PayPalPartnerClient client) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var trackingId = $"SELLER-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    var result = await client.PartnerReferrals.CreateAsync(new PartnerReferralRequest
    {
        TrackingId = trackingId,
        Operations = new List<ReferralOperation>
        {
            new()
            {
                Operation = "API_INTEGRATION",
                ApiIntegrationPreference = new ApiIntegrationPreference
                {
                    RestApiIntegration = new RestApiIntegration
                    {
                        IntegrationMethod = "PAYPAL",
                        IntegrationType = "THIRD_PARTY",
                        ThirdPartyDetails = new ThirdPartyDetails
                        {
                            Features = new List<string> { "PAYMENT", "REFUND", "ACCESS_MERCHANT_INFORMATION" }
                        }
                    }
                }
            }
        },
        Products = new List<string> { "PPCP" },
        PartnerConfigOverride = new PartnerConfigOverride
        {
            ReturnUrl = $"{baseUrl}/sellers/onboarded?trackingId={Uri.EscapeDataString(trackingId)}"
        },
        LegalConsents = new List<LegalConsent> { new() { Type = "SHARE_DATA_CONSENT", Granted = true } }
    });

    if (!result.IsSuccess)
    {
        return Results.BadRequest(new { error = result.Error?.Message ?? "Failed to create partner referral." });
    }

    return Results.Redirect(result.Data!.ActionUrl ?? "/");
});

app.MapGet("/sellers/onboarded", (string? trackingId) =>
    Results.Content($"<h3>Thanks! We're checking your onboarding status for {trackingId}...</h3>", "text/html"));

// 2. Create and capture a simple order (buyer pays the platform directly).
app.MapPost("/orders", async (PayPalPartnerClient client) =>
{
    var order = await client.Orders.CreateAsync(new OrderRequest
    {
        Intent = "CAPTURE",
        PurchaseUnits = new List<PurchaseUnit>
        {
            new() { Amount = new AmountWithBreakdown(26.00m, "USD"), Description = "Order from the sample app" }
        }
    });

    if (!order.IsSuccess)
    {
        return Results.BadRequest(new { error = order.Error?.Message, details = order.Error?.Details });
    }

    return Results.Ok(new { orderId = order.Data!.Id, approvalUrl = order.Data.ApprovalUrl });
});

app.MapPost("/orders/{orderId}/capture", async (string orderId, PayPalPartnerClient client) =>
{
    var capture = await client.Orders.CaptureAsync(orderId);
    return capture.IsSuccess
        ? Results.Ok(capture.Data)
        : Results.BadRequest(new { error = capture.Error?.Message });
});

// 3. Receive and verify PayPal webhook notifications.
app.MapPayPalPartnerWebhook("/webhooks/paypal",
    onSuccess: async (result, ctx) =>
    {
        Console.WriteLine($"Verified webhook: {result.Event?.EventType} ({result.Event?.Id})");
        // await orderService.HandleAsync(result.Event);
    },
    onFailure: async (result, ctx) =>
    {
        Console.WriteLine("Webhook signature verification failed.");
    });

app.Run();
