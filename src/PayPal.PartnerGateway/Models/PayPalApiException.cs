using System;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// Thrown for transport-level failures only (the HTTP call itself could not complete, or PayPal's
/// response body could not be parsed as JSON). Ordinary API errors - declined payments, validation
/// failures, 404s - are never thrown; they come back as a non-success <see cref="PayPalApiResult{T}"/>.
/// </summary>
public class PayPalApiException : Exception
{
    public PayPalApiException(string message) : base(message)
    {
    }

    public PayPalApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
