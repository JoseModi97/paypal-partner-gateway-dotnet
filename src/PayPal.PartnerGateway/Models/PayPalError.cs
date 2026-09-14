using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// The error body PayPal returns for a non-2xx response.
/// See https://developer.paypal.com/api/rest/responses/#error
/// </summary>
public class PayPalError
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("debug_id")]
    public string? DebugId { get; set; }

    [JsonPropertyName("details")]
    public List<PayPalErrorDetail>? Details { get; set; }

    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }
}

public class PayPalErrorDetail
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("issue")]
    public string? Issue { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
