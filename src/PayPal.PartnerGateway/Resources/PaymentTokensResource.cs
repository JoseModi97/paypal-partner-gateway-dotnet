using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// The Vault APIs (<c>/v3/vault/*</c>): save a payment method without charging it (setup tokens),
/// then turn it into a reusable payment token for later orders. Access via <c>client.PaymentTokens</c>.
/// </summary>
public class PaymentTokensResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal PaymentTokensResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<PayPalApiResult<SetupToken>> CreateSetupTokenAsync(
        SetupTokenRequest request,
        string? authAssertion = null,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<SetupToken>(
            HttpMethod.Post, "/v3/vault/setup-tokens", request,
            authAssertion: authAssertion, cancellationToken: cancellationToken);

    public Task<PayPalApiResult<SetupToken>> GetSetupTokenAsync(
        string setupTokenId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(setupTokenId, nameof(setupTokenId));
        return _gateway.SendAsync<SetupToken>(
            HttpMethod.Get, $"/v3/vault/setup-tokens/{Uri.EscapeDataString(setupTokenId)}",
            authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    /// <summary>Promotes an approved setup token (<see cref="PaymentTokenRequest.FromSetupToken"/>) into a reusable payment token.</summary>
    public Task<PayPalApiResult<PaymentToken>> CreatePaymentTokenAsync(
        PaymentTokenRequest request,
        string? authAssertion = null,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<PaymentToken>(
            HttpMethod.Post, "/v3/vault/payment-tokens", request,
            authAssertion: authAssertion, cancellationToken: cancellationToken);

    public Task<PayPalApiResult<PaymentToken>> GetPaymentTokenAsync(
        string paymentTokenId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(paymentTokenId, nameof(paymentTokenId));
        return _gateway.SendAsync<PaymentToken>(
            HttpMethod.Get, $"/v3/vault/payment-tokens/{Uri.EscapeDataString(paymentTokenId)}",
            authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    /// <summary>Migrates a legacy billing agreement ID (e.g. "B-7LT926395E2643345") into a modern reusable payment token.</summary>
    public Task<PayPalApiResult<PaymentToken>> MigrateBillingAgreementAsync(
        string billingAgreementId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(billingAgreementId, nameof(billingAgreementId));
        return CreatePaymentTokenAsync(
            PaymentTokenRequest.FromBillingAgreement(billingAgreementId), authAssertion, cancellationToken);
    }

    private static void RequireId(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
    }
}
