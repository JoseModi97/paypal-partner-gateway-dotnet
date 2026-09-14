using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.AspNetCore;

/// <summary>
/// Helper for handling PayPal webhooks in classic MVC controllers or custom handlers, where you
/// can't use the Minimal API <c>MapPayPalPartnerWebhook</c> extension.
/// </summary>
public static class PayPalPartnerWebhookHandler
{
    /// <summary>Verifies and processes an incoming PayPal webhook request, returning an <see cref="IResult"/>.</summary>
    public static async Task<IResult> ProcessAsync(
        HttpRequest request,
        PayPalPartnerClient client,
        Func<WebhookVerificationResult, Task>? onSuccess = null,
        Func<WebhookVerificationResult, Task>? onFailure = null,
        string? webhookId = null)
    {
        var rawBody = await EndpointRouteBuilderExtensions.ReadRawBodyAsync(request).ConfigureAwait(false);
        var headers = EndpointRouteBuilderExtensions.ReadHeaders(request);

        var result = await client.Webhooks.VerifyAsync(headers, rawBody, webhookId, request.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (result.IsValid)
        {
            if (onSuccess != null)
            {
                await onSuccess(result).ConfigureAwait(false);
            }

            return Results.Ok(new { status = "ok" });
        }

        if (onFailure != null)
        {
            await onFailure(result).ConfigureAwait(false);
        }

        return Results.BadRequest(new { status = "error", message = "PayPal webhook signature verification failed." });
    }
}
