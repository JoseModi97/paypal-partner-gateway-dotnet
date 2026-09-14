using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace PayPal.PartnerGateway.Tests;

public class PartnerReferralsResourceTests
{
    private static PayPalPartnerConfig TestConfig(string? partnerId = null) => new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret",
        Environment = PayPalEnvironment.Sandbox,
        PartnerId = partnerId,
    };

    [Fact]
    public async Task GetSellerStatusAsync_UsesConfiguredPartnerId_WhenNotPassedExplicitly()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{\"merchant_id\":\"MERCHANT-1\"}"));
        var client = new PayPalPartnerClient(TestConfig(partnerId: "CONFIGURED-PARTNER"), new HttpClient(handler));

        var result = await client.PartnerReferrals.GetSellerStatusAsync(null, "MERCHANT-1");

        Assert.True(result.IsSuccess);
        var apiRequest = handler.Requests[1];
        Assert.Contains("/customer/partners/CONFIGURED-PARTNER/merchant-integrations/MERCHANT-1", apiRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetSellerStatusAsync_ExplicitPartnerId_OverridesConfig()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{\"merchant_id\":\"MERCHANT-1\"}"));
        var client = new PayPalPartnerClient(TestConfig(partnerId: "CONFIGURED-PARTNER"), new HttpClient(handler));

        await client.PartnerReferrals.GetSellerStatusAsync("EXPLICIT-PARTNER", "MERCHANT-1");

        var apiRequest = handler.Requests[1];
        Assert.Contains("/customer/partners/EXPLICIT-PARTNER/merchant-integrations/MERCHANT-1", apiRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetSellerStatusAsync_Throws_WhenNoPartnerIdAvailableAnywhere()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"));
        var client = new PayPalPartnerClient(TestConfig(), new HttpClient(handler));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PartnerReferrals.GetSellerStatusAsync(null, "MERCHANT-1"));
    }

    [Fact]
    public async Task ListSellersAsync_DefaultsPartnerIdFromConfig()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{}"));
        var client = new PayPalPartnerClient(TestConfig(partnerId: "CONFIGURED-PARTNER"), new HttpClient(handler));

        await client.PartnerReferrals.ListSellersAsync();

        var apiRequest = handler.Requests[1];
        Assert.Contains("/customer/partners/CONFIGURED-PARTNER/merchant-integrations", apiRequest.RequestUri!.ToString());
    }
}
