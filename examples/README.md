# Examples

Standalone, runnable mini-projects - one per hosting model `paypal-partner init` auto-detects.
Each references the **real published** `PayPal.PartnerGateway` package(s) from NuGet.org (not a
project reference to the source in this repo), so they show exactly what a consumer's own project
would look like.

All five use PayPal's own **public Sandbox test credentials**, published in PayPal's
["PayPal Partner APIs"](https://developer.paypal.com/api/rest/) Postman collection for exactly this
kind of try-it-out purpose - clone the repo, `dotnet run`, and it talks to a real PayPal Sandbox
environment with zero setup. Swap in your own Client ID/Secret from the
[PayPal Developer Dashboard](https://developer.paypal.com/dashboard/applications) for anything
beyond quick experimentation - never use public test credentials in anything real.

| Example | Hosting model | Run it |
|---|---|---|
| [`console-script`](console-script) | Plain console app, no DI | `cd console-script && dotnet run` |
| [`aspnetcore-minimal-api`](aspnetcore-minimal-api) | ASP.NET Core Minimal API | `cd aspnetcore-minimal-api && dotnet run` |
| [`aspnetcore-mvc`](aspnetcore-mvc) | ASP.NET Core MVC | `cd aspnetcore-mvc && dotnet run` |
| [`worker-service`](worker-service) | .NET Generic Host / BackgroundService | `cd worker-service && dotnet run` |
| [`file-based-app`](file-based-app) | .NET 10 file-based app, no `.csproj` | `dotnet run file-based-app/app.cs` |

## How the payment actually completes

Every example creates a $10.00 USD Sandbox order via `client.Orders.CreateAsync(...)` - the same call
regardless of hosting model, since that's the point of the library. Turning that into an actual
payment is PayPal's own three-step Checkout flow, and step 2 needs a human in a browser:

1. **Create** the order (all five examples do this) - returns an `approvalUrl`.
2. **Approve** it - open that URL and log in as a PayPal **Sandbox buyer** account (not the app
   credentials above, which identify the platform, not a payer). Get one from your own
   [PayPal Developer account](https://developer.paypal.com/dashboard/accounts) - a "Personal" test
   account - then click through to **Pay Now** on the Sandbox checkout page. If the page seems to
   loop instead of progressing, retry in an incognito/private window - it's a Sandbox session-cookie
   quirk, unrelated to the order itself.
3. **Capture** the order (`client.Orders.CaptureAsync(...)`) - this is the step that actually moves
   the (fake, Sandbox) money and returns a completed payment with a capture ID.

| Example | How far it goes |
|---|---|
| `console-script`, `file-based-app` | All three steps - pauses with `Console.ReadLine()` after printing the approval URL so you can approve it in a browser, then captures and prints the result |
| `aspnetcore-minimal-api`, `aspnetcore-mvc` | Steps 1 and 3 as separate endpoints - `POST /orders` then, after you approve it in a browser, `POST /orders/{id}/capture` |
| `worker-service` | Step 1 only - a headless worker can't pause for browser approval, so it just creates the order and logs the approval URL |

Prefer the terminal over any of these? The `paypal-partner` CLI's `order` command walks the exact
same three steps without a project at all:

```bash
paypal-partner order create --amount 10.00
# open the printed approvalUrl, approve it as a Sandbox buyer, then:
paypal-partner order capture <orderId>
```

Calling capture before the order is approved returns an `ORDER_NOT_APPROVED` error - that's PayPal
telling you step 2 hasn't happened yet, not a problem with the order or the request.

These projects intentionally live outside [`PayPal.PartnerGateway.sln`](../PayPal.PartnerGateway.sln) -
they're meant to be read and copied from, not built as part of the main solution.
