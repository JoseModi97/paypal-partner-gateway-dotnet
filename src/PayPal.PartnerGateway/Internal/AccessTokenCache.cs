using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Internal;

/// <summary>
/// Obtains and caches an OAuth2 client_credentials access token from PayPal, refreshing it
/// proactively before it expires. One instance is shared for the lifetime of a
/// <see cref="PayPal.PartnerGateway.PayPalPartnerGateway"/>, so all requests reuse the same token
/// instead of exchanging credentials for a new one on every call.
/// </summary>
internal sealed class AccessTokenCache
{
    private readonly HttpClient _httpClient;
    private readonly PayPalPartnerConfig _config;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public AccessTokenCache(HttpClient httpClient, PayPalPartnerConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    /// <summary>
    /// Returns a currently-valid access token, requesting a new one from PayPal only when the
    /// cached token is missing or within <see cref="PayPalPartnerConfig.TokenExpiryBuffer"/> of expiring.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _accessToken;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_accessToken != null && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _accessToken;
            }

            _config.AssertConfigured();

            using var request = new HttpRequestMessage(HttpMethod.Post, _config.ResolveBaseUrl() + "/v1/oauth2/token");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new PayPalApiException(
                    $"Failed to obtain a PayPal access token (HTTP {(int)response.StatusCode}). Check ClientId/ClientSecret and Environment. Response: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var token = root.GetProperty("access_token").GetString()
                        ?? throw new PayPalApiException("PayPal token response did not contain an access_token.");
            var expiresInSeconds = root.TryGetProperty("expires_in", out var expiresInEl) ? expiresInEl.GetInt32() : 32400;

            _accessToken = token;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds) - _config.TokenExpiryBuffer;

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Drops the cached token, forcing the next call to request a fresh one.</summary>
    public void Invalidate()
    {
        _accessToken = null;
        _expiresAt = DateTimeOffset.MinValue;
    }
}
