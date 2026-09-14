using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// Request body for <c>POST /v3/vault/setup-tokens</c>. <see cref="PaymentSource"/> is raw JSON
/// because its shape depends entirely on the funding source you're vaulting (card, PayPal wallet,
/// Venmo) - build it exactly as shown in PayPal's Vault API reference for that funding source, e.g.
/// <c>new { card = new { number = "...", expiry = "2027-02", ... } }</c>.
/// See https://developer.paypal.com/docs/checkout/save-payment-methods/
/// </summary>
public class SetupTokenRequest
{
    [JsonPropertyName("payment_source")]
    public JsonNode PaymentSource { get; set; } = new JsonObject();

    [JsonPropertyName("customer")]
    public JsonNode? Customer { get; set; }
}

/// <summary>Response for setup-token create/get calls.</summary>
public class SetupToken
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>e.g. CREATED, APPROVED, PAYER_ACTION_REQUIRED.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("customer")]
    public JsonNode? Customer { get; set; }

    [JsonPropertyName("payment_source")]
    public JsonNode? PaymentSource { get; set; }

    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }
}

/// <summary>
/// Request body for <c>POST /v3/vault/payment-tokens</c> - promotes an approved setup token (or a
/// legacy billing agreement, for migration) into a reusable payment token.
/// </summary>
public class PaymentTokenRequest
{
    [JsonPropertyName("payment_source")]
    public PaymentTokenSource PaymentSource { get; set; } = new();

    [JsonPropertyName("customer")]
    public JsonNode? Customer { get; set; }

    public static PaymentTokenRequest FromSetupToken(string setupTokenId) => new()
    {
        PaymentSource = new PaymentTokenSource { Token = new TokenReference { Id = setupTokenId, Type = "SETUP_TOKEN" } }
    };

    public static PaymentTokenRequest FromBillingAgreement(string billingAgreementId) => new()
    {
        PaymentSource = new PaymentTokenSource { Token = new TokenReference { Id = billingAgreementId, Type = "BILLING_AGREEMENT" } }
    };
}

public class PaymentTokenSource
{
    [JsonPropertyName("token")]
    public TokenReference Token { get; set; } = new();
}

public class TokenReference
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>"SETUP_TOKEN" or "BILLING_AGREEMENT".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "SETUP_TOKEN";
}

/// <summary>Response for payment-token create/get calls - the reusable, vaulted payment method.</summary>
public class PaymentToken
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("customer")]
    public JsonNode? Customer { get; set; }

    [JsonPropertyName("payment_source")]
    public JsonNode? PaymentSource { get; set; }

    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }
}
