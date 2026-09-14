using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// Manages webhook subscriptions and verifies inbound webhook notifications
/// (<c>/v1/notifications/*</c>). Access via <c>client.Webhooks</c>.
/// </summary>
public class WebhooksResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal WebhooksResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<PayPalApiResult<Webhook>> CreateAsync(
        WebhookCreateRequest request,
        string? authAssertion = null,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<Webhook>(
            HttpMethod.Post, "/v1/notifications/webhooks", request,
            headers: new Dictionary<string, string> { ["Prefer"] = "return=representation" },
            authAssertion: authAssertion, cancellationToken: cancellationToken);

    public Task<PayPalApiResult<Webhook>> GetAsync(
        string webhookId,
        CancellationToken cancellationToken = default)
    {
        RequireId(webhookId, nameof(webhookId));
        return _gateway.SendAsync<Webhook>(
            HttpMethod.Get, $"/v1/notifications/webhooks/{Uri.EscapeDataString(webhookId)}",
            cancellationToken: cancellationToken);
    }

    /// <summary>Lists every webhook registered under your app's credentials.</summary>
    public Task<PayPalApiResult<JsonNode>> ListAsync(CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<JsonNode>(HttpMethod.Get, "/v1/notifications/webhooks", cancellationToken: cancellationToken);

    public Task<PayPalApiResult<Webhook>> UpdateAsync(
        string webhookId,
        List<JsonPatchOperation> patch,
        CancellationToken cancellationToken = default)
    {
        RequireId(webhookId, nameof(webhookId));
        return _gateway.SendAsync<Webhook>(
            new HttpMethod("PATCH"), $"/v1/notifications/webhooks/{Uri.EscapeDataString(webhookId)}", patch,
            cancellationToken: cancellationToken);
    }

    /// <summary>Deletes a webhook. Returns no body on success (HTTP 204).</summary>
    public async Task<RawPayPalResponse> DeleteAsync(string webhookId, CancellationToken cancellationToken = default)
    {
        RequireId(webhookId, nameof(webhookId));
        return await _gateway.SendRawAsync(
            HttpMethod.Delete, $"/v1/notifications/webhooks/{Uri.EscapeDataString(webhookId)}",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Asks PayPal to redeliver a past event notification to one or all of your subscribed webhooks.</summary>
    public Task<PayPalApiResult<JsonNode>> ResendEventNotificationAsync(
        string eventId,
        IEnumerable<string>? webhookIds = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(eventId, nameof(eventId));
        object? body = webhookIds != null ? new { webhook_ids = webhookIds } : null;
        return _gateway.SendAsync<JsonNode>(
            HttpMethod.Post, $"/v1/notifications/webhooks-events/{Uri.EscapeDataString(eventId)}/resend", body,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Calls PayPal to verify a webhook transmission's signature. Prefer <see cref="VerifyAsync"/>
    /// for handling an incoming HTTP request end-to-end (it builds this request from headers for you).
    /// </summary>
    public Task<PayPalApiResult<VerifyWebhookSignatureResult>> VerifySignatureAsync(
        VerifyWebhookSignatureRequest request,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<VerifyWebhookSignatureResult>(
            HttpMethod.Post, "/v1/notifications/verify-webhook-signature", request,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Verifies and parses an inbound webhook request in one call. Pass the raw request headers
    /// (case-insensitive) and the raw request body exactly as PayPal sent them - do not modify or
    /// re-serialize the body first, or signature verification will fail.
    /// </summary>
    /// <param name="headers">The incoming request's headers.</param>
    /// <param name="rawBody">The incoming request's raw body text.</param>
    /// <param name="webhookId">
    /// The webhook ID to verify against. Defaults to <see cref="PayPalPartnerConfig.WebhookId"/> if configured.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the underlying HTTP call.</param>
    public async Task<WebhookVerificationResult> VerifyAsync(
        IDictionary<string, string> headers,
        string rawBody,
        string? webhookId = null,
        CancellationToken cancellationToken = default)
    {
        if (headers == null) throw new ArgumentNullException(nameof(headers));
        if (rawBody == null) throw new ArgumentNullException(nameof(rawBody));

        var effectiveWebhookId = webhookId ?? _gateway.Config.WebhookId;
        if (string.IsNullOrWhiteSpace(effectiveWebhookId))
        {
            throw new InvalidOperationException(
                "VerifyAsync requires a webhook ID. Pass one explicitly or set PayPalPartnerConfig.WebhookId / the PAYPAL_WEBHOOK_ID environment variable.");
        }

        var lookup = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);

        WebhookEvent? parsedEvent = null;
        JsonNode? eventNode = null;
        if (!string.IsNullOrWhiteSpace(rawBody))
        {
            try
            {
                eventNode = JsonNode.Parse(rawBody);
                parsedEvent = eventNode?.Deserialize<WebhookEvent>(PayPalPartnerGateway.JsonOptions);
            }
            catch (Exception)
            {
                // Fall through - verification below will still run, and callers can inspect RawBody.
            }
        }

        var verifyRequest = new VerifyWebhookSignatureRequest
        {
            WebhookId = effectiveWebhookId!,
            TransmissionId = GetHeader(lookup, "Paypal-Transmission-Id"),
            TransmissionTime = GetHeader(lookup, "Paypal-Transmission-Time"),
            CertUrl = GetHeader(lookup, "Paypal-Cert-Url"),
            AuthAlgo = GetHeader(lookup, "Paypal-Auth-Algo"),
            TransmissionSig = GetHeader(lookup, "Paypal-Transmission-Sig"),
            WebhookEvent = eventNode,
        };

        var verificationCall = await VerifySignatureAsync(verifyRequest, cancellationToken).ConfigureAwait(false);
        var isValid = verificationCall.IsSuccess && verificationCall.Data?.IsValid == true;

        return new WebhookVerificationResult
        {
            IsValid = isValid,
            Event = parsedEvent,
            RawBody = rawBody,
            VerificationCall = verificationCall,
        };
    }

    private static string GetHeader(IDictionary<string, string> headers, string name) =>
        headers.TryGetValue(name, out var value) ? value : string.Empty;

    private static void RequireId(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
    }
}
