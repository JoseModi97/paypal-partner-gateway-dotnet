using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// Registers/unregisters the web domains your storefront serves Apple Pay from
/// (<c>/v1/customer/wallet-domains</c>). Access via <c>client.ApplePay</c>.
/// </summary>
public class ApplePayResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal ApplePayResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<PayPalApiResult<JsonNode>> RegisterDomainAsync(
        string domainName,
        string? authAssertion = null,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<JsonNode>(
            HttpMethod.Post, "/v1/customer/wallet-domains", new WalletDomainRequest(domainName),
            authAssertion: authAssertion, cancellationToken: cancellationToken);

    public Task<PayPalApiResult<JsonNode>> UnregisterDomainAsync(
        string domainName,
        string? reason = null,
        string? authAssertion = null,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<JsonNode>(
            HttpMethod.Post, "/v1/customer/unregister-wallet-domain", new WalletDomainRequest(domainName, reason),
            authAssertion: authAssertion, cancellationToken: cancellationToken);
}
