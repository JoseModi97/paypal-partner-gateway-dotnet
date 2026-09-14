using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// A HATEOAS link, as returned throughout PayPal's REST APIs.
/// </summary>
public class LinkDescription
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string? Method { get; set; }
}
