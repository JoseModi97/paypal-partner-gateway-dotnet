using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// Request body for <c>POST /v3/customer/managed-accounts</c> (Managed Path onboarding - limited
/// release, ask your PayPal partner manager for access). This covers the top-level fields every
/// submission needs; the deeply-nested owner/business objects are intentionally left as raw JSON
/// (<see cref="IndividualOwners"/>, <see cref="BusinessEntity"/>) because their required fields vary
/// a lot by country and account type - build them exactly as shown in PayPal's Manage Accounts API
/// reference for the country/entity type you're onboarding.
/// </summary>
public class ManagedAccountRequest
{
    /// <summary>Your own unique ID for this account.</summary>
    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>Two-character ISO 3166-1 country code, e.g. "US".</summary>
    [JsonPropertyName("legal_country_code")]
    public string LegalCountryCode { get; set; } = string.Empty;

    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>Three-character ISO 4217 currency code, e.g. "USD".</summary>
    [JsonPropertyName("primary_currency_code")]
    public string? PrimaryCurrencyCode { get; set; }

    /// <summary>Raw JSON array - see PayPal's Manage Accounts API reference for the required shape per country.</summary>
    [JsonPropertyName("individual_owners")]
    public JsonNode? IndividualOwners { get; set; }

    /// <summary>Raw JSON object - see PayPal's Manage Accounts API reference for the required shape per country/entity type.</summary>
    [JsonPropertyName("business_entity")]
    public JsonNode? BusinessEntity { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}

/// <summary>Response for managed-account create/search/get calls.</summary>
public class ManagedAccount
{
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("account_state")]
    public string? AccountState { get; set; }

    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}
