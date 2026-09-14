using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>An ISO-4217 currency amount, as used throughout the Orders/Payments APIs.</summary>
public class Money
{
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>String representation of the amount, e.g. "26.00". PayPal requires a string, not a number.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "0.00";

    public Money()
    {
    }

    public Money(decimal value, string currencyCode = "USD")
    {
        Value = value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        CurrencyCode = currencyCode;
    }

    public static implicit operator Money(decimal value) => new(value);
}
