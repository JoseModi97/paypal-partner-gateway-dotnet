# PayPal.PartnerGateway - PayPal Partner API Client for .NET

[![NuGet](https://img.shields.io/nuget/v/PayPal.PartnerGateway.svg)](https://www.nuget.org/packages/PayPal.PartnerGateway)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PayPal.PartnerGateway.svg)](https://www.nuget.org/packages/PayPal.PartnerGateway)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net10.0-blue.svg)](https://dotnet.microsoft.com/)

**PayPal.PartnerGateway** is an idiomatic, beginner-friendly **.NET / C# SDK for PayPal's Partner APIs**
(Partner Referrals / seller onboarding, Checkout Orders, Payments, Webhooks, Vault, Apple Pay, Shipment
Tracking) - built directly from PayPal's public [`PayPal Partner APIs`](https://developer.paypal.com/api/rest/)
Postman collection, so a platform/marketplace payments integration is a `dotnet add package` away instead
of hand-rolling OAuth2 client-credentials and HTTP plumbing yourself. Works with ASP.NET Core, Worker
Services, console apps, and plain class libraries - and a companion CLI tool can auto-detect which one
you're in and set it up for you.

Authored by [Jose Modi](https://github.com/JoseModi97).

Covers:
- **OAuth2 client-credentials token management** - acquired and refreshed for you, no manual caching.
- **Partner Referrals** - onboard connected sellers (1st-party and 3rd-party / hosted flows).
- **Checkout Orders** - create, confirm, authorize, capture.
- **Payments** - read captures/authorizations, capture an authorization, refund a capture.
- **Webhooks** - create/update/delete subscriptions, and **verify inbound signatures** in one call.
- **Vault (Payment Tokens)** - save a payment method and reuse it, including legacy billing-agreement migration.
- **Apple Pay** domain registration, **Shipment Tracking**, and the limited-release **Managed Accounts** onboarding path.
- **First-class ASP.NET Core integration** (`AddPayPalPartnerGateway` DI, Minimal API `MapPayPalPartnerWebhook`, and MVC controller helpers).
- An escape hatch (`client.Gateway.SendAsync<T>(...)`) for any PayPal REST endpoint this library hasn't wrapped yet.

---

## Packages

| Package | Description | Target Frameworks |
|---|---|---|
| **`PayPal.PartnerGateway`** | Core client: token management, all resource groups, DTOs | `netstandard2.0`, `net8.0`, `net10.0` |
| **`PayPal.PartnerGateway.AspNetCore`** | ASP.NET Core DI extensions and Minimal API webhook route builder | `net8.0`, `net10.0` |
| **`dotnet-paypal-partner-gateway`** | Global CLI tool (`paypal-partner`) that detects your project type and sets everything up | `net8.0` (runs on .NET 8, 9, 10+) |

## Installation

```bash
dotnet add package PayPal.PartnerGateway
dotnet add package PayPal.PartnerGateway.AspNetCore
```

Or skip the manual steps and let the CLI figure out what you need (see
[Automatic setup](#automatic-setup-cli) below):

```bash
dotnet tool install --global dotnet-paypal-partner-gateway
paypal-partner init
```

Get your **Client ID** and **Client Secret** from the [PayPal Developer Dashboard](https://developer.paypal.com/dashboard/applications) -
create a Sandbox app first, then a Live app once you're ready to go live.

---

## Automatic setup (CLI)

The `paypal-partner` CLI **auto-detects which kind of .NET project you're in** - ASP.NET Core (Minimal
API/MVC), a Worker Service, a console app, or a class library - and installs and wires up the right
package(s) accordingly. No `--framework` flag to get right; it inspects your `.csproj` (and `Program.cs`
as a fallback) for you.

```bash
dotnet tool install --global dotnet-paypal-partner-gateway

cd YourProject
paypal-partner init
```

Running `init` inside an ASP.NET Core project adds both `PayPal.PartnerGateway` and
`PayPal.PartnerGateway.AspNetCore`, writes a `PayPalPartner` section into `appsettings.json`, and prints
the exact `AddPayPalPartnerGateway`/`MapPayPalPartnerWebhook` snippet to paste into `Program.cs`. Running
it inside a console app or class library adds only the core package and prints a `new PayPalPartnerClient(...)`
snippet instead. Pass `--client-id`, `--client-secret`, and `--environment Sandbox|Live` to prefill values,
or `--dry-run` to preview without changing anything:

```bash
paypal-partner detect                 # show what init would do, without doing it
paypal-partner init --dry-run
paypal-partner init --client-id ID --client-secret SECRET --environment Sandbox
```

---

## Quickstart: ASP.NET Core Minimal APIs

### 1. Configure credentials (`appsettings.json`)

```json
{
  "PayPalPartner": {
    "ClientId": "YOUR_SANDBOX_CLIENT_ID",
    "ClientSecret": "YOUR_SANDBOX_CLIENT_SECRET",
    "Environment": "Sandbox",
    "PartnerAttributionId": "YOUR_BN_CODE",
    "WebhookId": "YOUR_WEBHOOK_ID"
  }
}
```

Or set environment variables instead: `PAYPAL_CLIENT_ID`, `PAYPAL_CLIENT_SECRET`, `PAYPAL_ENVIRONMENT`
(`Sandbox`/`Live`), `PAYPAL_BASE_URL`, `PAYPAL_PARTNER_ATTRIBUTION_ID`, `PAYPAL_PARTNER_ID`, `PAYPAL_WEBHOOK_ID`.

> **Never commit real credentials.** Use `dotnet user-secrets` locally and your host's secret store in production.

### 2. Register & use it (`Program.cs`)

```csharp
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.AspNetCore;
using PayPal.PartnerGateway.Models;

var builder = WebApplication.CreateBuilder(args);

// Reads the "PayPalPartner" section of appsettings.json.
builder.Services.AddPayPalPartnerGateway(builder.Configuration);

var app = builder.Build();

// Create an order.
app.MapPost("/orders", async (PayPalPartnerClient client) =>
{
    var order = await client.Orders.CreateAsync(new OrderRequest
    {
        Intent = "CAPTURE",
        PurchaseUnits = new List<PurchaseUnit>
        {
            new() { Amount = new AmountWithBreakdown(26.00m, "USD"), Description = "Order #1042" }
        }
    });

    return order.IsSuccess
        ? Results.Ok(new { orderId = order.Data!.Id, approvalUrl = order.Data.ApprovalUrl })
        : Results.BadRequest(new { error = order.Error?.Message, details = order.Error?.Details });
});

// Capture it once the buyer approves.
app.MapPost("/orders/{orderId}/capture", async (string orderId, PayPalPartnerClient client) =>
{
    var capture = await client.Orders.CaptureAsync(orderId);
    return capture.IsSuccess ? Results.Ok(capture.Data) : Results.BadRequest(capture.Error);
});

// Receive and verify webhook notifications - signature checking is handled for you.
app.MapPayPalPartnerWebhook("/webhooks/paypal",
    onSuccess: async (result, ctx) =>
    {
        Console.WriteLine($"Verified: {result.Event?.EventType} ({result.Event?.Id})");
        // await orderService.HandleAsync(result.Event);
    },
    onFailure: async (result, ctx) => Console.WriteLine("Webhook signature verification failed."));

app.Run();
```

A full runnable example (including onboarding a seller) is in
[`samples/PayPal.PartnerGateway.Sample.WebApi`](samples/PayPal.PartnerGateway.Sample.WebApi).

Every call returns a `PayPalApiResult<T>` - check `IsSuccess` before reading `Data`; declined payments and
validation failures come back as an ordinary result with `Error` populated, not an exception. An exception
(`PayPalApiException`) is only thrown for transport failures (network down, response not valid JSON).

---

## Onboarding a connected seller (Partner Referrals)

```csharp
var referral = await client.PartnerReferrals.CreateAsync(new PartnerReferralRequest
{
    TrackingId = $"SELLER-{sellerId}",   // your own ID for this seller
    Operations = new List<ReferralOperation>
    {
        new()
        {
            ApiIntegrationPreference = new ApiIntegrationPreference
            {
                RestApiIntegration = new RestApiIntegration
                {
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
    PartnerConfigOverride = new PartnerConfigOverride { ReturnUrl = "https://yourapp.com/onboarded" },
    LegalConsents = new List<LegalConsent> { new() { Granted = true } }
});

// Redirect the seller's browser here to finish signup on PayPal:
var onboardingUrl = referral.Data?.ActionUrl;

// Later, check whether they finished:
var status = await client.PartnerReferrals.GetSellerStatusAsync(partnerId: "YOUR_PARTNER_MERCHANT_ID", merchantId: "SELLER_MERCHANT_ID");
```

## Splitting a payment with a platform fee

```csharp
await client.Orders.CreateAsync(new OrderRequest
{
    Intent = "CAPTURE",
    PurchaseUnits = new List<PurchaseUnit>
    {
        new()
        {
            Amount = new AmountWithBreakdown(100.00m, "USD"),
            Payee = new Payee { MerchantId = "CONNECTED_SELLER_MERCHANT_ID" },
            PaymentInstruction = new PaymentInstruction
            {
                PlatformFees = new List<PlatformFee> { new() { Amount = new Money(5.00m, "USD") } }
            }
        }
    }
});
```

## Acting on behalf of a connected merchant

```csharp
var assertion = AuthAssertion.Create(client.Gateway.Config.ClientId, "CONNECTED_SELLER_MERCHANT_ID");
var order = await client.Orders.GetAsync(orderId, authAssertion: assertion);
```

## Standalone / console usage (no DI)

```csharp
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;

var client = new PayPalPartnerClient(new PayPalPartnerConfig
{
    ClientId = "YOUR_CLIENT_ID",
    ClientSecret = "YOUR_CLIENT_SECRET",
    Environment = PayPalEnvironment.Sandbox,
});

var order = await client.Orders.CreateAsync(new OrderRequest
{
    Intent = "CAPTURE",
    PurchaseUnits = new List<PurchaseUnit> { new() { Amount = new AmountWithBreakdown(10.00m) } }
});

Console.WriteLine(order.IsSuccess ? order.Data!.ApprovalUrl : order.Error!.Message);
```

## Verifying a webhook manually (MVC controllers)

```csharp
[HttpPost("webhooks/paypal")]
public Task<IResult> Webhook() =>
    PayPalPartnerWebhookHandler.ProcessAsync(Request, _client,
        onSuccess: async result => { /* update your database */ });
```

---

## Endpoint coverage

| Area | Resource | Methods |
|---|---|---|
| Auth | *(internal, automatic)* | OAuth2 client-credentials token fetch/refresh |
| Partner Referrals | `client.PartnerReferrals` | `CreateAsync`, `GetAsync`, `GetSellerStatusAsync`, `ListSellersAsync`, `GetSellerCredentialsAsync`, `GetSellerAccessTokenAsync` |
| Managed Accounts *(limited release)* | `client.Onboarding` | `CreateManagedAccountAsync`, `SearchByExternalIdAsync`, `GetBySellerIdAsync`, `UpdateAsync`, `GetWalletDomainsAsync`, `UploadVerificationDocumentAsync` |
| Checkout / Capture | `client.Orders` | `CreateAsync`, `GetAsync`, `ConfirmPaymentSourceAsync`, `AuthorizeAsync`, `CaptureAsync`, `AddTrackingAsync` |
| Payments | `client.Payments` | `GetCaptureAsync`, `GetAuthorizationAsync`, `CaptureAuthorizationAsync`, `RefundCaptureAsync` |
| Apple Pay | `client.ApplePay` | `RegisterDomainAsync`, `UnregisterDomainAsync` |
| Vault / Payment Tokens | `client.PaymentTokens` | `CreateSetupTokenAsync`, `GetSetupTokenAsync`, `CreatePaymentTokenAsync`, `GetPaymentTokenAsync`, `MigrateBillingAgreementAsync` |
| Shipment Tracking | `client.ShipmentTracking` | `AddAsync`, `AddBatchAsync` |
| Webhooks | `client.Webhooks` | `CreateAsync`, `GetAsync`, `ListAsync`, `UpdateAsync`, `DeleteAsync`, `ResendEventNotificationAsync`, `VerifySignatureAsync`, `VerifyAsync` |

Anything not listed above (or any future PayPal endpoint) can still be called through the low-level engine:

```csharp
var result = await client.Gateway.SendAsync<MyResponseType>(HttpMethod.Get, "/v1/some/new/endpoint");
```

## Error handling

```csharp
var result = await client.Orders.CreateAsync(request);

if (!result.IsSuccess)
{
    Console.WriteLine($"{result.StatusCode}: {result.Error?.Name} - {result.Error?.Message}");
    foreach (var detail in result.Error?.Details ?? new())
    {
        Console.WriteLine($"  {detail.Field}: {detail.Issue} - {detail.Description}");
    }
    // Include result.PayPalDebugId when contacting PayPal Merchant Technical Support.
}
```

---

## License

MIT © [Jose Modi](https://github.com/JoseModi97)

---

<sub>Keywords: PayPal .NET SDK, PayPal C# client, PayPal Partner API, PayPal Checkout Orders API,
PayPal Partner Referral onboarding, PayPal webhook signature verification, PayPal Vault payment tokens,
PayPal marketplace/split payments, PayPal ASP.NET Core integration.</sub>
