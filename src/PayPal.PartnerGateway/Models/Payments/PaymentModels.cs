using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>Request body for <c>POST /v2/payments/captures/{id}/refund</c>.</summary>
public class RefundRequest
{
    /// <summary>Omit to refund the full captured amount.</summary>
    [JsonPropertyName("amount")]
    public Money? Amount { get; set; }

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("note_to_payer")]
    public string? NoteToPayer { get; set; }
}

/// <summary>A captured payment, as returned by capture/refund/get-capture calls.</summary>
public class Capture
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>e.g. COMPLETED, DECLINED, PENDING, REFUNDED, PARTIALLY_REFUNDED.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public Money? Amount { get; set; }

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("final_capture")]
    public bool? FinalCapture { get; set; }

    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("links")]
    public System.Collections.Generic.List<LinkDescription>? Links { get; set; }
}

/// <summary>An authorized (not-yet-captured) payment, as returned by authorize/get-authorization calls.</summary>
public class Authorization
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>e.g. CREATED, CAPTURED, DENIED, EXPIRED, PARTIALLY_CAPTURED, VOIDED, PENDING.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public Money? Amount { get; set; }

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("expiration_time")]
    public string? ExpirationTime { get; set; }

    [JsonPropertyName("links")]
    public System.Collections.Generic.List<LinkDescription>? Links { get; set; }
}

/// <summary>Response body for a refund.</summary>
public class Refund
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>e.g. COMPLETED, PENDING, FAILED, CANCELLED.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public Money? Amount { get; set; }

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("note_to_payer")]
    public string? NoteToPayer { get; set; }

    [JsonPropertyName("links")]
    public System.Collections.Generic.List<LinkDescription>? Links { get; set; }
}
