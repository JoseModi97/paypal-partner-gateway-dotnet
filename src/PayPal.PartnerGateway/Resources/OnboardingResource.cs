using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// The Managed Path onboarding APIs (<c>/v3/customer/managed-accounts</c>, Limited Release - ask
/// your PayPal partner manager for access before using these). Access via <c>client.Onboarding</c>.
/// For the standard "Connected Path" onboarding flow, use <c>client.PartnerReferrals</c> instead.
/// </summary>
public class OnboardingResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal OnboardingResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<PayPalApiResult<ManagedAccount>> CreateManagedAccountAsync(
        ManagedAccountRequest request,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<ManagedAccount>(
            HttpMethod.Post, "/v3/customer/managed-accounts", request, cancellationToken: cancellationToken);

    public Task<PayPalApiResult<ManagedAccount>> SearchByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        RequireId(externalId, nameof(externalId));
        return _gateway.SendAsync<ManagedAccount>(
            HttpMethod.Get, $"/v3/customer/managed-accounts?external_id={Uri.EscapeDataString(externalId)}",
            cancellationToken: cancellationToken);
    }

    public Task<PayPalApiResult<ManagedAccount>> GetBySellerIdAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        RequireId(accountId, nameof(accountId));
        return _gateway.SendAsync<ManagedAccount>(
            HttpMethod.Get, $"/v3/customer/managed-accounts/{Uri.EscapeDataString(accountId)}",
            cancellationToken: cancellationToken);
    }

    public Task<PayPalApiResult<ManagedAccount>> UpdateAsync(
        string accountId,
        List<JsonPatchOperation> patch,
        CancellationToken cancellationToken = default)
    {
        RequireId(accountId, nameof(accountId));
        return _gateway.SendAsync<ManagedAccount>(
            new HttpMethod("PATCH"), $"/v3/customer/managed-accounts/{Uri.EscapeDataString(accountId)}", patch,
            cancellationToken: cancellationToken);
    }

    public Task<PayPalApiResult<System.Text.Json.Nodes.JsonNode>> GetWalletDomainsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        RequireId(accountId, nameof(accountId));
        return _gateway.SendAsync<System.Text.Json.Nodes.JsonNode>(
            HttpMethod.Get, $"/v3/customer/managed-accounts/{Uri.EscapeDataString(accountId)}/wallet-domains",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Uploads a supporting document (e.g. ID proof) against a verification token PayPal issued
    /// for a managed account that needs additional documentation.
    /// </summary>
    public async Task<RawPayPalResponse> UploadVerificationDocumentAsync(
        string verificationToken,
        Stream fileContent,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        RequireId(verificationToken, nameof(verificationToken));
        if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));

        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileContent);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);

        return await _gateway.SendRawAsync(
            HttpMethod.Post,
            $"/v1/customer/supporting-documents/{Uri.EscapeDataString(verificationToken)}/upload",
            form,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static void RequireId(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
    }
}
