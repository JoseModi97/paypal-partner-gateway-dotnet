using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// Onboards connected merchants (sellers) under your platform. Covers the "Connected Path"
/// (Partner Referral APIs) endpoints for both the 1st-party and 3rd-party integration flows.
/// Access via <c>client.PartnerReferrals</c>.
/// </summary>
public class PartnerReferralsResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal PartnerReferralsResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    /// <summary>
    /// Starts onboarding a new seller. For the 3rd-party flow, redirect the seller's browser to
    /// the returned <see cref="PartnerReferralResponse.ActionUrl"/> to complete signup on PayPal.
    /// </summary>
    public Task<PayPalApiResult<PartnerReferralResponse>> CreateAsync(
        PartnerReferralRequest request,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<PartnerReferralResponse>(
            HttpMethod.Post, "/v2/customer/partner-referrals", request, cancellationToken: cancellationToken);

    /// <summary>Fetches the referral submission back (echoes what was submitted, plus links).</summary>
    public Task<PayPalApiResult<PartnerReferralRequest>> GetAsync(
        string partnerReferralId,
        CancellationToken cancellationToken = default)
    {
        RequireId(partnerReferralId, nameof(partnerReferralId));
        return _gateway.SendAsync<PartnerReferralRequest>(
            HttpMethod.Get, $"/v2/customer/partner-referrals/{Uri.EscapeDataString(partnerReferralId)}",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Checks onboarding status/capabilities for a specific connected seller.
    /// <paramref name="merchantId"/> is the seller's PayPal merchant ID (or, in some PayPal
    /// accounts, the tracking ID you supplied at referral creation - PayPal accepts either here).
    /// </summary>
    /// <param name="partnerId">
    /// Your own PayPal merchant/payer ID as the partner. Omit to use <see cref="PayPalPartnerConfig.PartnerId"/>.
    /// </param>
    /// <param name="merchantId">The seller's PayPal merchant ID (or tracking ID).</param>
    /// <param name="cancellationToken">Cancellation token for the underlying HTTP call.</param>
    public Task<PayPalApiResult<SellerStatus>> GetSellerStatusAsync(
        string? partnerId,
        string merchantId,
        CancellationToken cancellationToken = default)
    {
        var resolvedPartnerId = ResolvePartnerId(partnerId);
        RequireId(merchantId, nameof(merchantId));
        return _gateway.SendAsync<SellerStatus>(
            HttpMethod.Get,
            $"/v1/customer/partners/{Uri.EscapeDataString(resolvedPartnerId)}/merchant-integrations/{Uri.EscapeDataString(merchantId)}",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Lists sellers onboarded under your partner account (3rd-party flow), optionally filtered by
    /// the tracking ID you supplied at referral creation.
    /// </summary>
    /// <param name="partnerId">
    /// Your own PayPal merchant/payer ID as the partner. Omit to use <see cref="PayPalPartnerConfig.PartnerId"/>.
    /// </param>
    /// <param name="trackingId">Filters to the seller with this tracking ID, if supplied at referral creation.</param>
    /// <param name="cancellationToken">Cancellation token for the underlying HTTP call.</param>
    public Task<PayPalApiResult<System.Text.Json.Nodes.JsonNode>> ListSellersAsync(
        string? partnerId = null,
        string? trackingId = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPartnerId = ResolvePartnerId(partnerId);
        var path = $"/v1/customer/partners/{Uri.EscapeDataString(resolvedPartnerId)}/merchant-integrations";
        if (!string.IsNullOrWhiteSpace(trackingId))
        {
            path += $"?tracking_id={Uri.EscapeDataString(trackingId)}";
        }
        return _gateway.SendAsync<System.Text.Json.Nodes.JsonNode>(HttpMethod.Get, path, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves the connected seller's own REST API Client ID/Secret (1st-party flow only - lets
    /// you call the PayPal APIs directly as the seller instead of via <see cref="AuthAssertion"/>).
    /// </summary>
    /// <param name="partnerId">
    /// Your own PayPal merchant/payer ID as the partner. Omit to use <see cref="PayPalPartnerConfig.PartnerId"/>.
    /// </param>
    /// <param name="queryParams">Extra query-string parameters PayPal's API reference lists for this call.</param>
    /// <param name="cancellationToken">Cancellation token for the underlying HTTP call.</param>
    public Task<PayPalApiResult<System.Text.Json.Nodes.JsonNode>> GetSellerCredentialsAsync(
        string? partnerId = null,
        IDictionary<string, string>? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPartnerId = ResolvePartnerId(partnerId);
        var path = $"/v1/customer/partners/{Uri.EscapeDataString(resolvedPartnerId)}/merchant-integrations/credentials";
        if (queryParams is { Count: > 0 })
        {
            var pairs = new List<string>();
            foreach (var kvp in queryParams)
            {
                pairs.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            path += "?" + string.Join("&", pairs);
        }
        return _gateway.SendAsync<System.Text.Json.Nodes.JsonNode>(HttpMethod.Get, path, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Exchanges a seller's one-time authorization code (from the 1st-party onboarding return
    /// redirect) for an access token you can use to call the PayPal APIs as that seller.
    /// </summary>
    public async Task<PayPalApiResult<System.Text.Json.Nodes.JsonNode>> GetSellerAccessTokenAsync(
        string authorizationCode,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode)) throw new ArgumentException("Authorization code is required.", nameof(authorizationCode));
        if (string.IsNullOrWhiteSpace(codeVerifier)) throw new ArgumentException("Code verifier is required.", nameof(codeVerifier));

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, _gateway.BaseUrl + "/v1/oauth2/token");

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_gateway.Config.ClientId}:{_gateway.Config.ClientSecret}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
        });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var data = string.IsNullOrWhiteSpace(body) ? null : System.Text.Json.Nodes.JsonNode.Parse(body);
            return PayPalApiResult<System.Text.Json.Nodes.JsonNode>.Success(response.StatusCode, data, body, null);
        }

        PayPalError? error = null;
        try
        {
            error = System.Text.Json.JsonSerializer.Deserialize<PayPalError>(body, PayPalPartnerGateway.JsonOptions);
        }
        catch (System.Text.Json.JsonException)
        {
        }

        return PayPalApiResult<System.Text.Json.Nodes.JsonNode>.Failure(response.StatusCode, error, body, null);
    }

    /// <summary>Falls back to <see cref="PayPalPartnerConfig.PartnerId"/> when <paramref name="partnerId"/> is omitted.</summary>
    private string ResolvePartnerId(string? partnerId)
    {
        var resolved = string.IsNullOrWhiteSpace(partnerId) ? _gateway.Config.PartnerId : partnerId;
        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                "This call needs a partner ID. Pass one explicitly, or set PayPalPartnerConfig.PartnerId / the PAYPAL_PARTNER_ID environment variable.");
        }

        return resolved!;
    }

    private static void RequireId(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
    }
}
