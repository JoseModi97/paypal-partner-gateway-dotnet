using System;
using System.Collections.Generic;
using System.Linq;
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
        // Set explicitly, not left to the app's default: .NET 10 file-based apps (`dotnet run
        // app.cs`) and AOT-ready projects turn reflection-based JSON off by default
        // (JsonSerializerIsReflectionEnabledByDefault=false), and every call then failed with
        // "Reflection-based serialization has been disabled for this application".
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
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
        HttpContent? content;
        if (body != null)
        {
            content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }
        else if (RequiresJsonContentType(method))
        {
            // PayPal's REST API expects Content-Type: application/json on POST/PUT/PATCH calls even
            // when there's nothing to send (e.g. capturing/authorizing an order with no overrides) -
            // omitting it entirely gets a 415 UNSUPPORTED_MEDIA_TYPE back from their servers.
            content = new StringContent("{}", Encoding.UTF8, "application/json");
        }
        else
        {
            content = null;
        }

        return SendRawAsync(method, path, content, headers, authAssertion, cancellationToken);
    }

    private static bool RequiresJsonContentType(HttpMethod method) =>
        method == HttpMethod.Post || method == HttpMethod.Put
        || string.Equals(method.Method, "PATCH", StringComparison.OrdinalIgnoreCase);

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
        // One id for the call, kept if it's resent below, so PayPal treats the resend as the same request.
        var requestId = Guid.NewGuid().ToString("N");

        // A sent request's content is disposed (by HttpClient on .NET Framework, by the request
        // everywhere), so a resend needs a copy: in-memory bodies (JSON, bytes) are kept as bytes.
        var replayable = content == null || content is ByteArrayContent;
        var replayBody = content is ByteArrayContent
            ? await content.ReadAsByteArrayAsync().ConfigureAwait(false)
            : null;
        var replayHeaders = content?.Headers.ToList();

        var result = await SendOnceAsync(method, url, content, headers, authAssertion, requestId, cancellationToken)
            .ConfigureAwait(false);

        // PayPal can stop accepting a cached token before its expires_in (revoked, credentials
        // rotated). Without this every call would fail with 401 until the cache expired: get a
        // fresh token and resend once. A streamed upload can't be resent; its 401 is returned as is.
        if (result.StatusCode == HttpStatusCode.Unauthorized && replayable)
        {
            _tokenCache.Invalidate();
            HttpContent? again = null;
            if (replayBody != null)
            {
                again = new ByteArrayContent(replayBody);
                foreach (var header in replayHeaders!)
                {
                    again.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            result = await SendOnceAsync(method, url, again, headers, authAssertion, requestId, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    private async Task<RawPayPalResponse> SendOnceAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        IDictionary<string, string>? headers,
        string? authAssertion,
        string requestId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);

        var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        request.Headers.TryAddWithoutValidation("PayPal-Request-Id", requestId);

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
