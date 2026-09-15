using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;
using Xunit;

namespace PayPal.PartnerGateway.Tests;

public class GatewayTests
{
    private static PayPalPartnerConfig TestConfig(string? partnerAttributionId = null) => new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret",
        Environment = PayPalEnvironment.Sandbox,
        PartnerAttributionId = partnerAttributionId,
    };

    [Fact]
    public async Task SendAsync_AttachesBearerTokenFromOAuthExchange()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{\"id\":\"ORDER-1\",\"status\":\"CREATED\"}"));
        var httpClient = new HttpClient(handler);
        var gateway = new PayPalPartnerGateway(TestConfig(), httpClient);

        var result = await gateway.SendAsync<Order>(HttpMethod.Get, "/v2/checkout/orders/ORDER-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("ORDER-1", result.Data!.Id);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("Bearer", handler.Requests[1].Headers.Authorization!.Scheme);
        Assert.Equal("abc123", handler.Requests[1].Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task SendAsync_ReusesCachedToken_DoesNotRequestANewOneEveryCall()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{\"id\":\"ORDER-1\",\"status\":\"CREATED\"}"),
            (HttpStatusCode.OK, "{\"id\":\"ORDER-1\",\"status\":\"CREATED\"}"));
        var httpClient = new HttpClient(handler);
        var gateway = new PayPalPartnerGateway(TestConfig(), httpClient);

        await gateway.SendAsync<Order>(HttpMethod.Get, "/v2/checkout/orders/ORDER-1");
        await gateway.SendAsync<Order>(HttpMethod.Get, "/v2/checkout/orders/ORDER-1");

        // 1 token exchange + 2 API calls, not 2 token exchanges + 2 API calls.
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task SendAsync_AttachesPartnerAttributionIdHeader_WhenConfigured()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{\"id\":\"ORDER-1\",\"status\":\"CREATED\"}"));
        var httpClient = new HttpClient(handler);
        var gateway = new PayPalPartnerGateway(TestConfig(partnerAttributionId: "MY-BN-CODE"), httpClient);

        await gateway.SendAsync<Order>(HttpMethod.Get, "/v2/checkout/orders/ORDER-1");

        var apiRequest = handler.Requests[1];
        Assert.True(apiRequest.Headers.TryGetValues("PayPal-Partner-Attribution-Id", out var values));
        Assert.Equal("MY-BN-CODE", System.Linq.Enumerable.First(values));
    }

    [Fact]
    public async Task SendAsync_ReturnsParsedError_OnNonSuccessResponse_WithoutThrowing()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.UnprocessableEntity,
                "{\"name\":\"UNPROCESSABLE_ENTITY\",\"message\":\"The requested action could not be performed.\",\"debug_id\":\"abc\"}"));
        var httpClient = new HttpClient(handler);
        var gateway = new PayPalPartnerGateway(TestConfig(), httpClient);

        var result = await gateway.SendAsync<Order>(HttpMethod.Post, "/v2/checkout/orders");

        Assert.False(result.IsSuccess);
        Assert.Equal("UNPROCESSABLE_ENTITY", result.Error!.Name);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
    }

    // Regression test for a real Sandbox failure: PayPal's API returns 415 UNSUPPORTED_MEDIA_TYPE
    // for a bodyless POST/PUT/PATCH (e.g. Orders.CaptureAsync/AuthorizeAsync with no overrides)
    // unless Content-Type: application/json is present - .NET's HttpClient never sends that header
    // on a request with no content, so the gateway has to attach an empty JSON body itself.
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task SendAsync_AttachesEmptyJsonBody_ForBodylessWriteMethods(string methodName)
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{\"id\":\"ORDER-1\",\"status\":\"COMPLETED\"}"));
        var httpClient = new HttpClient(handler);
        var gateway = new PayPalPartnerGateway(TestConfig(), httpClient);

        await gateway.SendAsync<Order>(new HttpMethod(methodName), "/v2/checkout/orders/ORDER-1/capture");

        Assert.Equal("application/json", handler.RequestContentTypes[1]);
        Assert.Equal("{}", handler.RequestBodies[1]);
    }

    [Fact]
    public async Task SendAsync_SendsNoBody_ForBodylessGet()
    {
        var handler = new FakeHttpMessageHandler(
            (HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":32400}"),
            (HttpStatusCode.OK, "{\"id\":\"ORDER-1\",\"status\":\"CREATED\"}"));
        var httpClient = new HttpClient(handler);
        var gateway = new PayPalPartnerGateway(TestConfig(), httpClient);

        await gateway.SendAsync<Order>(HttpMethod.Get, "/v2/checkout/orders/ORDER-1");

        Assert.Null(handler.RequestBodies[1]);
    }
}
