using System.Net;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// Wraps every PayPal API call. Inspect <see cref="IsSuccess"/> before using <see cref="Data"/> -
/// declined payments, validation failures, and duplicate-request replays are all *expected* outcomes
/// that PayPal reports as ordinary non-2xx responses, not exceptional situations, so this library
/// returns them instead of throwing. A <see cref="PayPalApiException"/> is only thrown for transport-level
/// problems (network failure, cancellation) or when the response body could not be parsed at all.
/// </summary>
/// <typeparam name="T">The parsed response body type on success.</typeparam>
public class PayPalApiResult<T>
{
    /// <summary>True when PayPal returned a 2xx status code.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>The HTTP status code PayPal returned.</summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>The parsed response body. Only populated when <see cref="IsSuccess"/> is true.</summary>
    public T? Data { get; init; }

    /// <summary>The parsed PayPal error body. Only populated when <see cref="IsSuccess"/> is false.</summary>
    public PayPalError? Error { get; init; }

    /// <summary>The raw, unparsed JSON response body - useful for logging or diagnosing unexpected shapes.</summary>
    public string RawBody { get; init; } = string.Empty;

    /// <summary>
    /// The value of the <c>Paypal-Debug-Id</c> response header, if present. Include this when
    /// contacting PayPal Merchant Technical Support about a specific call.
    /// </summary>
    public string? PayPalDebugId { get; init; }

    public static PayPalApiResult<T> Success(HttpStatusCode statusCode, T? data, string rawBody, string? debugId) =>
        new()
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Data = data,
            RawBody = rawBody,
            PayPalDebugId = debugId
        };

    public static PayPalApiResult<T> Failure(HttpStatusCode statusCode, PayPalError? error, string rawBody, string? debugId) =>
        new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Error = error,
            RawBody = rawBody,
            PayPalDebugId = debugId
        };
}
