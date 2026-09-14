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

Every example creates a $10.00 USD Sandbox order via `client.Orders.CreateAsync(...)` - the same call
regardless of hosting model, since that's the point of the library. Actually *paying* for it needs
PayPal's own three-step Checkout flow, which needs a human in the loop for step 2:

1. **Create** the order (all five examples do this).
2. **Approve** it - open the printed `approvalUrl` and log in as a PayPal **Sandbox buyer** account
   (not the app credentials above, which identify the platform, not a payer). Get one from your own
   [PayPal Developer account](https://developer.paypal.com/dashboard/accounts) - a "Personal" test
   account - then click Pay Now on the Sandbox checkout page.
3. **Capture** the order (`client.Orders.CaptureAsync(...)`) - this is the step that actually moves
   the (fake, Sandbox) money.

| Example | How far it goes |
|---|---|
| `console-script`, `file-based-app` | All three steps - pauses with `Console.ReadLine()` after printing the approval URL so you can approve it in a browser, then captures |
| `aspnetcore-minimal-api`, `aspnetcore-mvc` | Steps 1 and 3 as separate endpoints - `POST /orders` then, after you approve it in a browser, `POST /orders/{id}/capture` |
| `worker-service` | Step 1 only - a headless worker can't pause for browser approval, so it just creates the order and logs the approval URL |

Calling capture before the order is approved returns an `ORDER_NOT_APPROVED` error - that's PayPal
telling you step 2 didn't happen yet, not a bug in the library or the example.

These projects intentionally live outside [`PayPal.PartnerGateway.sln`](../PayPal.PartnerGateway.sln) -
they're meant to be read and copied from, not built as part of the main solution.
