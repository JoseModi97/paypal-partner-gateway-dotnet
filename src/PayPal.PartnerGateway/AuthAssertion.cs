using System;
using System.Text;
using System.Text.Json;

namespace PayPal.PartnerGateway;

/// <summary>
/// Builds a <c>PayPal-Auth-Assertion</c> header value: an unsigned JWT that identifies which
/// connected merchant a partner is acting on behalf of. Required on Orders/Payments calls made
/// with the partner's own access token for a transaction that belongs to a merchant onboarded via
/// Partner Referrals. See https://developer.paypal.com/api/rest/requests/#paypal-auth-assertion
/// </summary>
public static class AuthAssertion
{
    /// <summary>
    /// Creates the header value for <paramref name="merchantPayerId"/> (the connected merchant's
    /// PayPal merchant/payer ID) issued by your app (<paramref name="clientId"/>).
    /// </summary>
    public static string Create(string clientId, string merchantPayerId)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(merchantPayerId)) throw new ArgumentException("Merchant payer ID is required.", nameof(merchantPayerId));

        var header = Base64UrlEncode(JsonSerializer.Serialize(new { alg = "none" }));
        var payload = Base64UrlEncode(JsonSerializer.Serialize(new { iss = clientId, payer_id = merchantPayerId }));

        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
