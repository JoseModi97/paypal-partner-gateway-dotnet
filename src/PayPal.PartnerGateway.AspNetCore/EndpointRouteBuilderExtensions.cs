using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PayPal.PartnerGateway;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.AspNetCore;

/// <summary>
/// Minimal API extensions for mapping a PayPal webhook endpoint.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps an endpoint that receives PayPal webhook notifications, verifies the transmission
    /// signature via PayPal's verify-webhook-signature API, and parses the event for you.
    /// Exempts the route from Antiforgery validation (PayPal cannot supply an antiforgery token).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to map onto.</param>
    /// <param name="pattern">Route pattern, e.g. "/webhooks/paypal".</param>
    /// <param name="onSuccess">Invoked when the signature is valid. Do your order-fulfilment/database work here.</param>
    /// <param name="onFailure">Invoked when the signature is invalid or verification failed - log and investigate, don't process the event.</param>
    /// <param name="webhookId">
    /// Overrides <see cref="PayPalPartnerConfig.WebhookId"/> for this endpoint. Only needed if you
    /// have more than one webhook subscription and route them to different endpoints.
    /// </param>
    public static RouteHandlerBuilder MapPayPalPartnerWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<WebhookVerificationResult, HttpContext, Task>? onSuccess = null,
        Func<WebhookVerificationResult, HttpContext, Task>? onFailure = null,
        string? webhookId = null)
    {
        var endpoint = endpoints.MapPost(pattern, async (HttpContext context, PayPalPartnerClient client) =>
        {
            var rawBody = await ReadRawBodyAsync(context.Request).ConfigureAwait(false);
            var headers = ReadHeaders(context.Request);

            var result = await client.Webhooks.VerifyAsync(headers, rawBody, webhookId, context.RequestAborted)
                .ConfigureAwait(false);

            if (result.IsValid)
            {
                if (onSuccess != null)
                {
                    await onSuccess(result, context).ConfigureAwait(false);
                }

                return context.Response.HasStarted ? Results.Empty : Results.Ok(new { status = "ok" });
            }

            if (onFailure != null)
            {
                await onFailure(result, context).ConfigureAwait(false);
            }

            return context.Response.HasStarted
                ? Results.Empty
                : Results.BadRequest(new { status = "error", message = "PayPal webhook signature verification failed." });
        });

        endpoint.DisableAntiforgery();
        return endpoint;
    }

    internal static async Task<string> ReadRawBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
        request.Body.Position = 0;
        return body;
    }

    internal static System.Collections.Generic.Dictionary<string, string> ReadHeaders(HttpRequest request)
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }
        return headers;
    }
}
