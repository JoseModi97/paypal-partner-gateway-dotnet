using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>Request body for <c>POST /v1/notifications/webhooks</c>.</summary>
public class WebhookCreateRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("event_types")]
    public List<WebhookEventType> EventTypes { get; set; } = new();

    public WebhookCreateRequest()
    {
    }

    /// <summary>Convenience constructor. Pass no event names (or "*") to subscribe to every event type.</summary>
    public WebhookCreateRequest(string url, params string[] eventTypeNames)
    {
        Url = url;
        if (eventTypeNames.Length == 0)
        {
            EventTypes.Add(new WebhookEventType { Name = "*" });
        }
        else
        {
            foreach (var name in eventTypeNames)
            {
                EventTypes.Add(new WebhookEventType { Name = name });
            }
        }
    }
}

public class WebhookEventType
{
    /// <summary>An event name (e.g. "PAYMENT.CAPTURE.COMPLETED") or "*" for all events.
    /// Full list: https://developer.paypal.com/api/rest/webhooks/event-names/</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "*";
}

/// <summary>A registered webhook, as returned by create/get/list/update.</summary>
public class Webhook
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("event_types")]
    public List<WebhookEventType>? EventTypes { get; set; }

    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }
}

/// <summary>
/// Request body for <c>POST /v1/notifications/verify-webhook-signature</c>. Populate the
/// transmission fields from the incoming webhook request's headers - <c>MapPayPalPartnerWebhook</c>
/// in the ASP.NET Core package does this for you automatically.
/// </summary>
public class VerifyWebhookSignatureRequest
{
    [JsonPropertyName("webhook_id")]
    public string WebhookId { get; set; } = string.Empty;

    [JsonPropertyName("transmission_id")]
    public string TransmissionId { get; set; } = string.Empty;

    [JsonPropertyName("transmission_time")]
    public string TransmissionTime { get; set; } = string.Empty;

    [JsonPropertyName("cert_url")]
    public string CertUrl { get; set; } = string.Empty;

    [JsonPropertyName("auth_algo")]
    public string AuthAlgo { get; set; } = string.Empty;

    [JsonPropertyName("transmission_sig")]
    public string TransmissionSig { get; set; } = string.Empty;

    /// <summary>The raw JSON body of the webhook notification, parsed as a node.</summary>
    [JsonPropertyName("webhook_event")]
    public JsonNode? WebhookEvent { get; set; }
}

public class VerifyWebhookSignatureResult
{
    /// <summary>"SUCCESS" or "FAILURE".</summary>
    [JsonPropertyName("verification_status")]
    public string VerificationStatus { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsValid => VerificationStatus == "SUCCESS";
}

/// <summary>A parsed webhook notification payload, as delivered to your webhook URL.</summary>
public class WebhookEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>The affected resource (an Order, Capture, Refund, etc.) - shape depends on <see cref="EventType"/>.</summary>
    [JsonPropertyName("resource")]
    public JsonNode? Resource { get; set; }
}

/// <summary>
/// The outcome of verifying and parsing an inbound webhook request, as returned by
/// <see cref="PayPal.PartnerGateway.Resources.WebhooksResource.VerifyAsync"/> and by the ASP.NET Core
/// <c>MapPayPalPartnerWebhook</c> handler.
/// </summary>
public class WebhookVerificationResult
{
    /// <summary>True only when PayPal confirmed the transmission signature is authentic.</summary>
    public bool IsValid { get; init; }

    /// <summary>The parsed event body. Populated even when <see cref="IsValid"/> is false, so you can log what was received.</summary>
    public WebhookEvent? Event { get; init; }

    /// <summary>The raw request body exactly as received - use this if you need to re-verify or archive it.</summary>
    public string RawBody { get; init; } = string.Empty;

    /// <summary>The full result of the underlying verify-webhook-signature call, for diagnostics.</summary>
    public PayPalApiResult<VerifyWebhookSignatureResult>? VerificationCall { get; init; }
}
