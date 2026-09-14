using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>A postal address, as used by onboarding and shipping objects.</summary>
public class Address
{
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("address_line_2")]
    public string? AddressLine2 { get; set; }

    /// <summary>City, town, or village.</summary>
    [JsonPropertyName("admin_area_2")]
    public string? AdminArea2 { get; set; }

    /// <summary>State, province, or region.</summary>
    [JsonPropertyName("admin_area_1")]
    public string? AdminArea1 { get; set; }

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    /// <summary>Two-character ISO 3166-1 country code, e.g. "US".</summary>
    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = string.Empty;
}
