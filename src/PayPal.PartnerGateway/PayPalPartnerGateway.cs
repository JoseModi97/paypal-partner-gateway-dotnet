using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Internal;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway;

/// <summary>
/// The low-level engine behind <see cref="PayPalPartnerClient"/>: manages the OAuth2 access token,
/// attaches the headers every PayPal Partner API call needs, and (de)serializes requests/responses.
/// Most consumers should use <see cref="PayPalPartnerClient"/>'s resource groups (<c>client.Orders</c>,
/// <c>client.PartnerReferrals</c>, etc.) instead of calling this directly - but <see cref="SendAsync{TResponse}"/>
/// is public so you can call any PayPal REST endpoint this library hasn't wrapped with a typed method yet.
/// </summary>
public class PayPalPartnerGateway
{
    private readonly HttpClient _httpClient;
    private readonly AccessTokenCache _tokenCache;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public PayPalPartnerConfig Config { get; }

    /// <summary>The resolved API host in use, e.g. https://api-m.sandbox.paypal.com.</summary>
    public string BaseUrl => Config.ResolveBaseUrl();

    public PayPalPartnerGateway(PayPalPartnerConfig config, HttpClient? httpClient = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? new HttpClient();
        _tokenCache = new AccessTokenCache(_httpClient, Config);
    }

    /// <summary>
    /// Returns a currently-valid OAuth2 access token, fetching or refreshing it as needed.
    /// You normally don't need this directly - <see cref="SendAsync{TResponse}"/> attaches it automatically.
    /// </summary>
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        _tokenCache.GetAccessTokenAsync(cancellationToken);

    /// <summary>
    /// Sends an authenticated request to the PayPal Partner APIs and parses the JSON response.
    /// Use this for any endpoint not yet wrapped by a typed resource method on <see cref="PayPalPartnerClient"/>.
    /// </summary>
    /// <typeparam name="TResponse">The expected shape of a successful (2xx) JSON response body.</typeparam>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Path relative to <see cref="BaseUrl"/>, e.g. <c>/v2/checkout/orders</c>.</param>
    /// <param name="body">Request body, serialized as JSON. Pass <c>null</c> for no body.</param>
    /// <param name="headers">Extra headers to send, e.g. <c>Prefer</c> or <c>PayPal-Request-Id</c> overrides.</param>
    /// <param name="authAssertion">
    /// Optional <c>PayPal-Auth-Assertion</c> header value, used by partners acting on behalf of a
    /// connected merchant. Build it with <see cref="AuthAssertion.Create"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the underlying HTTP call.</param>
    public async Task<PayPalApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body = null,
        IDictionary<string, string>? headers = null,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        var rawResult = await SendRawAsync(method, path, body, headers, authAssertion, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(rawResult.Body))
        {
            return rawResult.IsSuccess
                ? PayPalApiResult<TResponse>.Success(rawResult.StatusCode, default, rawResult.Body, rawResult.DebugId)
                : PayPalApiResult<TResponse>.Failure(rawResult.StatusCode, null, rawResult.Body, rawResult.DebugId);
        }

        if (rawResult.IsSuccess)
        {
            TResponse? data;
            try
            {
                data = JsonSerializer.Deserialize<TResponse>(rawResult.Body, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new PayPalApiException(
                    $"PayPal returned a successful response that could not be parsed as {typeof(TResponse).Name}: {ex.Message}",
                    ex);
            }

            return PayPalApiResult<TResponse>.Success(rawResult.StatusCode, data, rawResult.Body, rawResult.DebugId);
        }

        PayPalError? error;
        try
        {
            error = JsonSerializer.Deserialize<PayPalError>(rawResult.Body, JsonOptions);
        }
        catch (JsonException)
        {
            error = null;
        }

        return PayPalApiResult<TResponse>.Failure(rawResult.StatusCode, error, rawResult.Body, rawResult.DebugId);
    }

    /// <summary>
    /// Sends an authenticated request and returns the raw status code and body, without attempting
    /// to parse a response type. Useful for endpoints that return <c>204 No Content</c> on success
    /// (e.g. deleting a webhook).
    /// </summary>
    public Task<RawPayPalResponse> SendRawAsync(
        HttpMethod method,
        string path,
        object? body = null,
        IDictionary<string, string>? headers = null,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        HttpContent? content = body != null
            ? new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
            : null;

        return SendRawAsync(method, path, content, headers, authAssertion, cancellationToken);
    }

    /// <summary>
    /// Sends an authenticated request with a pre-built <see cref="HttpContent"/> - use this for
    /// content types <see cref="SendAsync{TResponse}"/> doesn't cover, such as a multipart file upload.
    /// </summary>
    public async Task<RawPayPalResponse> SendRawAsync(
        HttpMethod method,
        string path,
        HttpContent? content,
        IDictionary<string, string>? headers = null,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));

        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : BaseUrl + path;

        using var request = new HttpRequestMessage(method, url);

        var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        request.Headers.TryAddWithoutValidation("PayPal-Request-Id", Guid.NewGuid().ToString("N"));

        if (!string.IsNullOrWhiteSpace(Config.PartnerAttributionId))
        {
            request.Headers.TryAddWithoutValidation("PayPal-Partner-Attribution-Id", Config.PartnerAttributionId);
        }

        if (!string.IsNullOrWhiteSpace(authAssertion))
        {
            request.Headers.TryAddWithoutValidation("PayPal-Auth-Assertion", authAssertion);
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.Remove(header.Key);
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (content != null)
        {
            request.Content = content;
        }

        HttpResponseMessage response;
        string responseBody;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new PayPalApiException($"The request to {url} could not be completed: {ex.Message}", ex);
        }

        var debugId = response.Headers.TryGetValues("Paypal-Debug-Id", out var values)
            ? System.Linq.Enumerable.FirstOrDefault(values)
            : null;

        return new RawPayPalResponse
        {
            IsSuccess = response.IsSuccessStatusCode,
            StatusCode = response.StatusCode,
            Body = responseBody,
            DebugId = debugId,
        };
    }
}

/// <summary>An unparsed PayPal HTTP response.</summary>
public class RawPayPalResponse
{
    public bool IsSuccess { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? DebugId { get; init; }
}
