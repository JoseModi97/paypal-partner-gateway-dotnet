# PayPal.PartnerGateway - PayPal Partner API Client for .NET

[![NuGet](https://img.shields.io/nuget/v/PayPal.PartnerGateway.svg)](https://www.nuget.org/packages/PayPal.PartnerGateway)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PayPal.PartnerGateway.svg)](https://www.nuget.org/packages/PayPal.PartnerGateway)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blue.svg)](https://dotnet.microsoft.com/)

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
- A **CLI tool** (`paypal-partner`) that auto-detects and sets up any project type, *and* can create/approve/capture
  a real Sandbox order straight from the terminal (`order create`/`order get`/`order capture`) with no code written.
- An escape hatch (`client.Gateway.SendAsync<T>(...)`) for any PayPal REST endpoint this library hasn't wrapped yet.

---

## Packages

| Package | Description | Target Frameworks |
|---|---|---|
| **`PayPal.PartnerGateway`** | Core client: token management, all resource groups, DTOs | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| **`PayPal.PartnerGateway.AspNetCore`** | ASP.NET Core DI extensions and Minimal API webhook route builder | `net8.0`, `net9.0`, `net10.0` |
| **`dotnet-paypal-partner-gateway`** | Global CLI tool (`paypal-partner`) - interactive setup wizard that auto-detects your project type, plus `order create/get/capture` to exercise the real Orders API from the terminal | `net8.0` (rolls forward to run on .NET 9, 10, and any newer major runtime) |

The core package's `netstandard2.0` target means it also runs on **.NET Framework 4.6.1+**, **.NET Core 2.0+**,
Mono, Xamarin, and Unity - not just modern .NET - so it slots into most existing .NET codebases regardless
of how old or new they are, while `net8.0`/`net9.0`/`net10.0` give current runtimes their own optimized build.

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

### Updating

```bash
# Library packages
dotnet add package PayPal.PartnerGateway
dotnet add package PayPal.PartnerGateway.AspNetCore

# CLI tool
dotnet tool update --global dotnet-paypal-partner-gateway
```

`dotnet add package`/`dotnet tool update` (no version pinned) always pull the latest version on NuGet.org.
Check what you actually have installed with `dotnet list package` (library packages) or
`paypal-partner --version` (CLI) - a stale CLI in particular can bite you, since it's a separately
versioned global install that `dotnet restore` never touches; re-run `dotnet tool update -g
dotnet-paypal-partner-gateway` any time you hit unexpected API behavior before assuming it's a bug.

---

## Automatic setup (CLI)

The `paypal-partner` CLI **auto-detects which kind of .NET project you're in** and installs/wires up the
right package(s) for it - no `--framework` flag, no interactive "what kind of app is this?" prompt. It
covers every way NuGet.org itself documents installing a package for a project-based app, plus the newer
.NET 10 file-based app model:

| Detected as | Detected via | Result |
|---|---|---|
| ASP.NET Core (Minimal API/MVC) | `Sdk="Microsoft.NET.Sdk.Web"` or `WebApplication.CreateBuilder` | Core + `.AspNetCore` packages, `appsettings.json`, DI/webhook snippet |
| Worker Service | `Sdk="Microsoft.NET.Sdk.Worker"` or `Host.CreateApplicationBuilder` | Core + `.AspNetCore` packages (for DI), `appsettings.json` |
| Console app / class library | `OutputType=Exe` or plain `Microsoft.NET.Sdk` | Core package only, `new PayPalPartnerClient(...)` snippet |
| .NET 10+ file-based app (no `.csproj`) | A single `.cs` file, no project | `#:package` directives in the file instead of `dotnet add package` |

<sub>File-based apps hosting ASP.NET Core also get a `#:sdk Microsoft.NET.Sdk.Web` directive automatically.</sub>

```bash
dotnet tool install --global dotnet-paypal-partner-gateway

cd YourProject      # or a folder with a single app.cs, no .csproj required
paypal-partner
```

Running it with no arguments shows a menu:

```
=========================================================
        PayPal Partner Gateway Setup Wizard (CLI)
=========================================================
What would you like to do?
  1) Set up this project (init) (default)
  2) Detect project type only, don't change anything (detect)
  3) Exit
Enter selection (1-3) [1]:
```

Picking **init** (or running `paypal-partner init` directly) launches an **interactive setup wizard**
(the same style as [`ecitizen-pesaflow init`](https://github.com/JoseModi97/ecitizen-pesaflow-gateway-dotnet))
- it prompts for your Client ID, Client Secret (masked input), environment (Sandbox/Live), and optional
partner settings (BN code, webhook ID), then adds the package(s), writes `appsettings.json`, and
generates a starter usage file for your project (`Endpoints/PayPalPaymentEndpoints.cs` for ASP.NET Core,
or `PayPalDemo.cs` for a console app/class library). The one thing it does **not** ask about is which
kind of project you're in - that's always auto-detected, never a question.

**Re-running it is safe**, Gii-style: it detects a prior run (an existing `PayPalPartner` section in
`appsettings.json`, or an existing `#:package` directive for a file-based app), shows a note that it
found one, and pre-fills every prompt with those existing values as the default - just press Enter
through everything to leave your setup untouched. Before it would overwrite anything that already
exists (the `appsettings.json` section, or the generated starter file), it asks first; pass `--force`
to skip that confirmation and always overwrite the starter file.

```
=========================================================
        PayPal Partner Gateway Setup Wizard (CLI)
=========================================================
Detected project: ASP.NET Core (Minimal API / MVC)
  -> YourProject.csproj

Step 1: PayPal REST API credentials (from the PayPal Developer Dashboard)

Client ID: MY_CLIENT_ID
Client Secret: ********

Step 2: Environment

Select environment:
  1) Sandbox - safe for development and testing (default)
  2) Live - real money, real merchants
Enter selection (1-2) [1]:

Step 3: Optional partner settings (press Enter to skip)

PayPal-Partner-Attribution-Id / BN code:
Webhook ID (Webhooks tab in the dashboard):

Apply this configuration now? [Y/n]:
```

Pass `--yes`/`-y` to skip every prompt and run entirely from flags/environment variables instead (e.g.
for CI or scripted setup), `--client-id`/`--client-secret`/`--environment`/`--partner-attribution-id`/
`--webhook-id` to prefill values (shown as defaults in each prompt unless `-y` is used), or `--dry-run`
to preview without changing anything:

```bash
paypal-partner detect                 # show what init would detect and install, without doing it
paypal-partner init --dry-run
paypal-partner init --yes --client-id ID --client-secret SECRET --environment Sandbox
```

### Trying the API without writing any code

`paypal-partner order` calls the real Orders API directly from the terminal - useful for a quick
Sandbox sanity check before wiring anything into your own app. It resolves credentials the same way
`init` writes them: explicit flags, then `PAYPAL_*` environment variables, then the `PayPalPartner`
section of an `appsettings.json` in the current directory - so it just works right after `init`.

Turning an order into an actual payment is PayPal's own three-step Checkout flow, and step 2 needs
a human in a browser:

1. **Create** the order - `paypal-partner order create` returns an `approvalUrl`.
2. **Approve** it - open that URL and log in as a PayPal **Sandbox buyer** account (not the
   Client ID/Secret above, which identify the platform, not a payer). Get one from your own
   [PayPal Developer account](https://developer.paypal.com/dashboard/accounts) - a "Personal" test
   account - then click through to **Pay Now** on the Sandbox checkout page. If the page seems to
   loop instead of progressing, retry in an incognito/private window - it's a Sandbox session-cookie
   quirk, unrelated to the order itself. **Clicking Pay Now here won't redirect you anywhere or show
   a confirmation** - the CLI didn't set a `return_url` (there's no running web app for PayPal to
   send you back to), so you're left on PayPal's own page. That's expected for this terminal-only
   flow; just proceed to step 3. If your *own app* has this same "nothing happens after Pay Now"
   problem, see [Quickstart](#quickstart-aspnet-core-minimal-apis) - that's a missing `return_url`
   on your order, not a bug.
3. **Capture** the order - `paypal-partner order capture <orderId>` actually moves the (fake,
   Sandbox) money and returns a completed payment with a capture ID.

```bash
paypal-partner order create --amount 10.00 --description "Test order"
# Order ID:      8P4...
# Approval URL:  https://www.sandbox.paypal.com/checkoutnow?token=8P4...
#
# Open the approval URL, approve it as a Sandbox buyer, then:

paypal-partner order capture 8P4...
paypal-partner order get 8P4...
```

Calling capture before the order is approved returns an `ORDER_NOT_APPROVED` error - that's PayPal
telling you step 2 hasn't happened yet, not a problem with the order or the request. The
[`examples/`](examples) folder shows the same three-step flow wired into each hosting model.

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
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

// Reads the "PayPalPartner" section of appsettings.json.
builder.Services.AddPayPalPartnerGateway(builder.Configuration);

var app = builder.Build();

// Create an order.
app.MapPost("/orders", async (HttpRequest request, PayPalPartnerClient client) =>
{
    // application_context.return_url/cancel_url tell PayPal's hosted approval page where to
    // send the buyer back to after they click Pay Now. Omit them and the buyer is left on
    // PayPal's own page with no redirect and no visible confirmation - the approval (and any
    // capture you do afterward) still succeeds, it's just invisible to the buyer.
    var baseUrl = $"{request.Scheme}://{request.Host}";

    var order = await client.Orders.CreateAsync(new OrderRequest
    {
        Intent = "CAPTURE",
        PurchaseUnits = new List<PurchaseUnit>
        {
            new() { Amount = new AmountWithBreakdown(26.00m, "USD"), Description = "Order #1042" }
        },
        ApplicationContext = new JsonObject
        {
            ["return_url"] = $"{baseUrl}/orders/return",
            ["cancel_url"] = $"{baseUrl}/orders/cancel",
            ["user_action"] = "PAY_NOW",
        }
    });

    return order.IsSuccess
        ? Results.Ok(new { orderId = order.Data!.Id, approvalUrl = order.Data.ApprovalUrl })
        : Results.BadRequest(new { error = order.Error?.Message, details = order.Error?.Details });
});

// PayPal redirects the buyer here after approval, appending ?token={orderId} - capture it directly.
app.MapGet("/orders/return", async (string token, PayPalPartnerClient client) =>
{
    var capture = await client.Orders.CaptureAsync(token);
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
[`samples/PayPal.PartnerGateway.Sample.WebApi`](samples/PayPal.PartnerGateway.Sample.WebApi). For a smaller
example per hosting model - console app, Minimal API, MVC, Worker Service, and a .NET 10 file-based app -
each pre-wired with PayPal's public Sandbox test credentials so it runs against a real Sandbox with zero
setup, see [`examples/`](examples).

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

// Once the buyer has approved it at the approval URL above:
var capture = await client.Orders.CaptureAsync(order.Data!.Id!);
Console.WriteLine(capture.IsSuccess ? $"Captured! Status: {capture.Data!.Status}" : capture.Error!.Message);
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

OAuth2 token fetch/refresh happens automatically - there's no resource group for it. Everything else
is grouped under `client.<Resource>`:

| Area | Access via |
|---|---|
| Partner Referrals | `client.PartnerReferrals` |
| Managed Accounts *(limited release)* | `client.Onboarding` |
| Checkout / Capture | `client.Orders` |
| Payments | `client.Payments` |
| Apple Pay | `client.ApplePay` |
| Vault / Payment Tokens | `client.PaymentTokens` |
| Shipment Tracking | `client.ShipmentTracking` |
| Webhooks | `client.Webhooks` |

<details>
<summary><strong>Full method list per resource</strong></summary>

**Partner Referrals** — `client.PartnerReferrals`
`CreateAsync` · `GetAsync` · `GetSellerStatusAsync` · `ListSellersAsync` · `GetSellerCredentialsAsync` · `GetSellerAccessTokenAsync`

**Managed Accounts** *(limited release)* — `client.Onboarding`
`CreateManagedAccountAsync` · `SearchByExternalIdAsync` · `GetBySellerIdAsync` · `UpdateAsync` · `GetWalletDomainsAsync` · `UploadVerificationDocumentAsync`

**Checkout / Capture** — `client.Orders`
`CreateAsync` · `GetAsync` · `ConfirmPaymentSourceAsync` · `AuthorizeAsync` · `CaptureAsync` · `AddTrackingAsync`

**Payments** — `client.Payments`
`GetCaptureAsync` · `GetAuthorizationAsync` · `CaptureAuthorizationAsync` · `RefundCaptureAsync`

**Apple Pay** — `client.ApplePay`
`RegisterDomainAsync` · `UnregisterDomainAsync`

**Vault / Payment Tokens** — `client.PaymentTokens`
`CreateSetupTokenAsync` · `GetSetupTokenAsync` · `CreatePaymentTokenAsync` · `GetPaymentTokenAsync` · `MigrateBillingAgreementAsync`

**Shipment Tracking** — `client.ShipmentTracking`
`AddAsync` · `AddBatchAsync`

**Webhooks** — `client.Webhooks`
`CreateAsync` · `GetAsync` · `ListAsync` · `UpdateAsync` · `DeleteAsync` · `ResendEventNotificationAsync` · `VerifySignatureAsync` · `VerifyAsync`

</details>

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
