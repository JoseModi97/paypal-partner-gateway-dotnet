using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// Reads and refunds captured payments and reads authorizations (<c>/v2/payments/*</c>).
/// Access via <c>client.Payments</c>.
/// </summary>
public class PaymentsResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal PaymentsResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<PayPalApiResult<Capture>> GetCaptureAsync(
        string captureId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(captureId, nameof(captureId));
        return _gateway.SendAsync<Capture>(
            HttpMethod.Get, $"/v2/payments/captures/{Uri.EscapeDataString(captureId)}",
            authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    public Task<PayPalApiResult<Authorization>> GetAuthorizationAsync(
        string authorizationId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(authorizationId, nameof(authorizationId));
        return _gateway.SendAsync<Authorization>(
            HttpMethod.Get, $"/v2/payments/authorizations/{Uri.EscapeDataString(authorizationId)}",
            authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    /// <summary>Captures a previously authorized payment (an <see cref="Authorization"/> from <c>Orders.AuthorizeAsync</c>).</summary>
    public Task<PayPalApiResult<Capture>> CaptureAuthorizationAsync(
        string authorizationId,
        RefundRequest? amountOverride = null,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(authorizationId, nameof(authorizationId));
        return _gateway.SendAsync<Capture>(
            HttpMethod.Post, $"/v2/payments/authorizations/{Uri.EscapeDataString(authorizationId)}/capture",
            amountOverride, authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    /// <summary>Refunds a captured payment, in full or in part.</summary>
    public Task<PayPalApiResult<Refund>> RefundCaptureAsync(
        string captureId,
        RefundRequest? request = null,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(captureId, nameof(captureId));
        return _gateway.SendAsync<Refund>(
            HttpMethod.Post, $"/v2/payments/captures/{Uri.EscapeDataString(captureId)}/refund",
            request, authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    private static void RequireId(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
    }
}
