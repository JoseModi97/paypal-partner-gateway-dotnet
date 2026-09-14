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

Every example creates a $10.00 USD Sandbox order via `client.Orders.CreateAsync(...)` and prints the
order ID and approval URL - the same call regardless of hosting model, since that's the point of the
library. The two ASP.NET Core examples expose it as `POST /orders` / `GET /orders/{id}` instead of
running it immediately on startup; `worker-service` and the two script-style examples run it once and
exit.

These projects intentionally live outside [`PayPal.PartnerGateway.sln`](../PayPal.PartnerGateway.sln) -
they're meant to be read and copied from, not built as part of the main solution.
