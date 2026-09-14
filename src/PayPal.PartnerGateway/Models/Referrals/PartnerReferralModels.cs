using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// Request body for <c>POST /v2/customer/partner-referrals</c> - the entry point for onboarding a
/// new connected merchant (seller), for both the "1st party" (you host the signup form) and
/// "3rd party" (PayPal hosts the signup flow, you redirect the seller to <see cref="PartnerReferralResponse.ActionUrl"/>)
/// flows. Richer 1st-party submissions that include the seller's business/individual details
/// directly (names, addresses, etc.) can add those fields via <see cref="AdditionalProperties"/>.
/// See https://developer.paypal.com/docs/multiparty/seller-onboarding/before-payment/
/// </summary>
public class PartnerReferralRequest
{
    /// <summary>Your own unique ID for this seller - lets you look up onboarding status by your own key later.</summary>
    [JsonPropertyName("tracking_id")]
    public string? TrackingId { get; set; }

    [JsonPropertyName("operations")]
    public List<ReferralOperation>? Operations { get; set; }

    /// <summary>e.g. ["PPCP"], ["EXPRESS_CHECKOUT"].</summary>
    [JsonPropertyName("products")]
    public List<string>? Products { get; set; }

    /// <summary>e.g. ["PAYPAL_WALLET_VAULTING_ADVANCED"] - only needed for advanced vaulting.</summary>
    [JsonPropertyName("capabilities")]
    public List<string>? Capabilities { get; set; }

    [JsonPropertyName("partner_config_override")]
    public PartnerConfigOverride? PartnerConfigOverride { get; set; }

    [JsonPropertyName("legal_consents")]
    public List<LegalConsent>? LegalConsents { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("preferred_language_code")]
    public string? PreferredLanguageCode { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}

public class ReferralOperation
{
    /// <summary>Always "API_INTEGRATION" for the standard REST integration flow.</summary>
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = "API_INTEGRATION";

    [JsonPropertyName("api_integration_preference")]
    public ApiIntegrationPreference ApiIntegrationPreference { get; set; } = new();
}

public class ApiIntegrationPreference
{
    [JsonPropertyName("rest_api_integration")]
    public RestApiIntegration RestApiIntegration { get; set; } = new();
}

public class RestApiIntegration
{
    [JsonPropertyName("integration_method")]
    public string IntegrationMethod { get; set; } = "PAYPAL";

    /// <summary>"FIRST_PARTY" or "THIRD_PARTY".</summary>
    [JsonPropertyName("integration_type")]
    public string IntegrationType { get; set; } = "THIRD_PARTY";

    [JsonPropertyName("third_party_details")]
    public ThirdPartyDetails? ThirdPartyDetails { get; set; }
}

public class ThirdPartyDetails
{
    /// <summary>e.g. ["PAYMENT", "REFUND", "ACCESS_MERCHANT_INFORMATION", "PARTNER_FEE", "VAULT", "BILLING_AGREEMENT"].</summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
}

public class PartnerConfigOverride
{
    /// <summary>Where PayPal redirects the seller after they finish the hosted onboarding flow.</summary>
    [JsonPropertyName("return_url")]
    public string? ReturnUrl { get; set; }

    [JsonPropertyName("return_url_description")]
    public string? ReturnUrlDescription { get; set; }

    [JsonPropertyName("action_renewal_url")]
    public string? ActionRenewalUrl { get; set; }

    [JsonPropertyName("show_add_credit_card")]
    public bool? ShowAddCreditCard { get; set; }

    [JsonPropertyName("partner_logo_url")]
    public string? PartnerLogoUrl { get; set; }
}

public class LegalConsent
{
    /// <summary>Typically "SHARE_DATA_CONSENT".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "SHARE_DATA_CONSENT";

    [JsonPropertyName("granted")]
    public bool Granted { get; set; }
}

/// <summary>Response from creating a partner referral.</summary>
public class PartnerReferralResponse
{
    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }

    /// <summary>
    /// For the 3rd-party flow: redirect the seller's browser here to complete onboarding
    /// (the <c>rel: "action_url"</c> link). Null for 1st-party submissions, which onboard immediately.
    /// </summary>
    [JsonIgnore]
    public string? ActionUrl
    {
        get
        {
            if (Links == null) return null;
            foreach (var link in Links)
            {
                if (link.Rel == "action_url") return link.Href;
            }
            return null;
        }
    }

    /// <summary>
    /// The referral ID (parsed out of the <c>rel: "self"</c> link) - pass to
    /// <see cref="PayPal.PartnerGateway.Resources.PartnerReferralsResource.GetAsync"/> to check status.
    /// </summary>
    [JsonIgnore]
    public string? PartnerReferralId
    {
        get
        {
            if (Links == null) return null;
            foreach (var link in Links)
            {
                if (link.Rel != "self") continue;
                var segments = link.Href.TrimEnd('/').Split('/');
                return segments.Length > 0 ? segments[segments.Length - 1] : null;
            }
            return null;
        }
    }
}

/// <summary>Response from fetching seller onboarding status (<c>GET .../merchant-integrations/{merchant_id}</c>).</summary>
public class SellerStatus
{
    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }

    [JsonPropertyName("tracking_id")]
    public string? TrackingId { get; set; }

    [JsonPropertyName("payments_receivable")]
    public bool? PaymentsReceivable { get; set; }

    [JsonPropertyName("primary_email")]
    public string? PrimaryEmail { get; set; }

    [JsonPropertyName("primary_email_confirmed")]
    public bool? PrimaryEmailConfirmed { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}
