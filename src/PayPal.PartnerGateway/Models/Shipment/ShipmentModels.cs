using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>Request body for <c>POST /v2/checkout/orders/{id}/track</c>.</summary>
public class TrackingInfo
{
    [JsonPropertyName("capture_id")]
    public string? CaptureId { get; set; }

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>e.g. "FEDEX", "UPS", "USPS" - see PayPal's carrier list for the full set of accepted values.</summary>
    [JsonPropertyName("carrier")]
    public string Carrier { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name_other")]
    public string? CarrierNameOther { get; set; }

    [JsonPropertyName("notify_payer")]
    public bool NotifyPayer { get; set; } = true;

    [JsonPropertyName("items")]
    public List<TrackingItem>? Items { get; set; }
}

public class TrackingItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }
}

/// <summary>One entry of <c>POST /v1/shipping/trackers-batch</c>.</summary>
public class BatchTracker
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>e.g. "SHIPPED", "ON_HOLD", "DELIVERED", "CANCELLED".</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "SHIPPED";

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier")]
    public string Carrier { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name_other")]
    public string? CarrierNameOther { get; set; }

    /// <summary>"CARRIER_PROVIDED" or "OTHER".</summary>
    [JsonPropertyName("tracking_number_type")]
    public string? TrackingNumberType { get; set; }

    /// <summary>ISO-8601 date, e.g. "2026-09-14".</summary>
    [JsonPropertyName("shipment_date")]
    public string? ShipmentDate { get; set; }

    [JsonPropertyName("notify_buyer")]
    public bool NotifyBuyer { get; set; } = true;

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("tracking_number_validated")]
    public bool? TrackingNumberValidated { get; set; }
}

public class BatchTrackerRequest
{
    [JsonPropertyName("trackers")]
    public List<BatchTracker> Trackers { get; set; } = new();
}
