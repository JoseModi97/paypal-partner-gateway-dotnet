using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// Request body for <c>POST /v2/checkout/orders</c>. Covers the fields nearly every integration
/// needs (intent, purchase units, payee/platform fees for split payments) with typed models;
/// less common or highly variant sections (<c>payment_source</c>, <c>application_context</c>) are
/// left as raw JSON via <see cref="PaymentSource"/>/<see cref="ApplicationContext"/> so you can pass
/// exactly the shape PayPal's API reference shows for the flow you need (PayPal wallet, card, Venmo, etc.)
/// without waiting on this library to model every payment source. See
/// https://developer.paypal.com/docs/api/orders/v2/#orders_create
/// </summary>
public class OrderRequest
{
    /// <summary>"CAPTURE" or "AUTHORIZE".</summary>
    [JsonPropertyName("intent")]
    public string Intent { get; set; } = "CAPTURE";

    [JsonPropertyName("purchase_units")]
    public List<PurchaseUnit> PurchaseUnits { get; set; } = new();

    /// <summary>
    /// Raw JSON for the <c>payment_source</c> object (e.g. <c>{ "paypal": { "experience_context": {...} } }</c>).
    /// Optional - omit to let the buyer pick a funding source in the PayPal-hosted checkout flow.
    /// </summary>
    [JsonPropertyName("payment_source")]
    public JsonNode? PaymentSource { get; set; }

    /// <summary>Raw JSON for the <c>application_context</c> object (return_url, brand_name, shipping_preference, etc.).</summary>
    [JsonPropertyName("application_context")]
    public JsonNode? ApplicationContext { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}

public class PurchaseUnit
{
    /// <summary>Required when an order has more than one purchase unit.</summary>
    [JsonPropertyName("reference_id")]
    public string? ReferenceId { get; set; }

    [JsonPropertyName("custom_id")]
    public string? CustomId { get; set; }

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("soft_descriptor")]
    public string? SoftDescriptor { get; set; }

    [JsonPropertyName("amount")]
    public AmountWithBreakdown Amount { get; set; } = new();

    [JsonPropertyName("items")]
    public List<OrderItem>? Items { get; set; }

    /// <summary>Who receives the funds - set this when a partner is processing on behalf of a connected merchant.</summary>
    [JsonPropertyName("payee")]
    public Payee? Payee { get; set; }

    /// <summary>Platform/partner fees and disbursement mode for split/marketplace payments.</summary>
    [JsonPropertyName("payment_instruction")]
    public PaymentInstruction? PaymentInstruction { get; set; }

    [JsonPropertyName("shipping")]
    public JsonNode? Shipping { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}

public class AmountWithBreakdown
{
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "USD";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "0.00";

    /// <summary>Only required when <see cref="PurchaseUnit.Items"/> is set - the parts must sum to <see cref="Value"/>.</summary>
    [JsonPropertyName("breakdown")]
    public AmountBreakdown? Breakdown { get; set; }

    public AmountWithBreakdown()
    {
    }

    public AmountWithBreakdown(decimal value, string currencyCode = "USD")
    {
        Value = value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        CurrencyCode = currencyCode;
    }
}

public class AmountBreakdown
{
    [JsonPropertyName("item_total")]
    public Money? ItemTotal { get; set; }

    [JsonPropertyName("shipping")]
    public Money? Shipping { get; set; }

    [JsonPropertyName("handling")]
    public Money? Handling { get; set; }

    [JsonPropertyName("tax_total")]
    public Money? TaxTotal { get; set; }

    [JsonPropertyName("insurance")]
    public Money? Insurance { get; set; }

    [JsonPropertyName("shipping_discount")]
    public Money? ShippingDiscount { get; set; }

    [JsonPropertyName("discount")]
    public Money? Discount { get; set; }
}

public class OrderItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("unit_amount")]
    public Money UnitAmount { get; set; } = new();

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; } = "1";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    /// <summary>"DIGITAL_GOODS", "PHYSICAL_GOODS", or "DONATION".</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("tax")]
    public Money? Tax { get; set; }
}

public class Payee
{
    /// <summary>The connected merchant's PayPal merchant/payer ID (from Partner Referrals onboarding).</summary>
    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }

    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }
}

public class PaymentInstruction
{
    [JsonPropertyName("platform_fees")]
    public List<PlatformFee>? PlatformFees { get; set; }

    /// <summary>"INSTANT" (default) or "DELAYED".</summary>
    [JsonPropertyName("disbursement_mode")]
    public string? DisbursementMode { get; set; }
}

public class PlatformFee
{
    [JsonPropertyName("amount")]
    public Money Amount { get; set; } = new();

    /// <summary>Defaults to the partner's own account when omitted.</summary>
    [JsonPropertyName("payee")]
    public Payee? Payee { get; set; }
}

/// <summary>Response body for order create/get/confirm/authorize/capture calls.</summary>
public class Order
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>e.g. CREATED, SAVED, APPROVED, VOIDED, COMPLETED, PAYER_ACTION_REQUIRED.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("purchase_units")]
    public List<PurchaseUnit>? PurchaseUnits { get; set; }

    [JsonPropertyName("payer")]
    public JsonNode? Payer { get; set; }

    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }

    /// <summary>
    /// Convenience accessor for the buyer's approval URL (the <c>rel: "payer-action"</c> or
    /// legacy <c>rel: "approve"</c> link) - redirect the browser here to collect buyer approval.
    /// Returns null for server-side flows that don't require redirect (e.g. a vaulted card).
    /// </summary>
    [JsonIgnore]
    public string? ApprovalUrl
    {
        get
        {
            if (Links == null) return null;
            foreach (var link in Links)
            {
                if (link.Rel is "payer-action" or "approve") return link.Href;
            }
            return null;
        }
    }
}
