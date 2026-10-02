using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;
using Xunit;

namespace PayPal.PartnerGateway.Tests;

// Regression tests for flaws found running a real shop's PayPal checkout against Sandbox:
// capturing straight from the return URL failed when the buyer refreshed (ORDER_ALREADY_CAPTURED),
// an order reading COMPLETED was treated as paid while its capture was still PENDING/DECLINED,
// and a token PayPal stopped accepting early failed every call until the cache expired.
public class OrdersCompletionTests
{
    private const string Token = "{\"access_token\":\"abc123\",\"expires_in\":32400}";

    private static (PayPalPartnerClient Client, FakeHttpMessageHandler Handler) ClientWith(
        params (HttpStatusCode, string)[] responses)
    {
        var handler = new FakeHttpMessageHandler(responses);
        var client = new PayPalPartnerClient(new PayPalPartnerConfig
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Environment = PayPalEnvironment.Sandbox,
        }, new HttpClient(handler));
        return (client, handler);
    }

    private static string OrderJson(string status, string? captureStatus = null) =>
        captureStatus == null
            ? $"{{\"id\":\"ORDER-1\",\"status\":\"{status}\"}}"
            : $"{{\"id\":\"ORDER-1\",\"status\":\"{status}\",\"purchase_units\":[{{\"amount\":{{\"currency_code\":\"USD\",\"value\":\"10.00\"}}," +
              $"\"payments\":{{\"captures\":[{{\"id\":\"CAPTURE-1\",\"status\":\"{captureStatus}\",\"amount\":{{\"currency_code\":\"USD\",\"value\":\"10.00\"}}}}]}}}}]}}";

    private static string IssueJson(string issue) =>
        $"{{\"name\":\"UNPROCESSABLE_ENTITY\",\"message\":\"The requested action could not be performed.\",\"debug_id\":\"dbg\",\"details\":[{{\"issue\":\"{issue}\",\"description\":\"...\"}}]}}";

    private static string Header(HttpRequestMessage request, string name) =>
        request.Headers.TryGetValues(name, out var values) ? values.First() : "";

    [Fact]
    public async Task CompleteAsync_CapturesAnApprovedOrder_WithARequestIdFixedForTheOrder()
    {
        var (client, handler) = ClientWith(
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.OK, OrderJson("APPROVED")),
            (HttpStatusCode.Created, OrderJson("COMPLETED", "COMPLETED")));

        var result = await client.Orders.CompleteAsync("ORDER-1");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
        Assert.Equal("CAPTURE-1", result.Data.Capture!.Id);
        Assert.Equal(10.00m, decimal.Parse(result.Data.Capture.Amount!.Value, System.Globalization.CultureInfo.InvariantCulture));
        var capture = handler.Requests[2];
        Assert.Equal(HttpMethod.Post, capture.Method);
        Assert.EndsWith("/v2/checkout/orders/ORDER-1/capture", capture.RequestUri!.AbsolutePath);
        Assert.Equal("capture-ORDER-1", Header(capture, "PayPal-Request-Id"));
        Assert.Equal("return=representation", Header(capture, "Prefer"));
    }

    [Fact]
    public async Task CompleteAsync_OnAnOrderAlreadyCaptured_ReadsItInsteadOfCapturingAgain()
    {
        // e.g. the buyer refreshed the return page after the first visit captured it
        var (client, handler) = ClientWith(
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.OK, OrderJson("COMPLETED", "COMPLETED")));

        var result = await client.Orders.CompleteAsync("ORDER-1");

        Assert.True(result.Data!.IsPaid);
        Assert.Equal(2, handler.Requests.Count); // token + GET, no capture
    }

    [Fact]
    public async Task CompleteAsync_TreatsOrderAlreadyCaptured_AsSuccess()
    {
        // Two callers raced: both saw APPROVED, the other one captured first.
        var (client, handler) = ClientWith(
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.OK, OrderJson("APPROVED")),
            (HttpStatusCode.UnprocessableEntity, IssueJson(PayPalIssues.OrderAlreadyCaptured)),
            (HttpStatusCode.OK, OrderJson("COMPLETED", "COMPLETED")));

        var result = await client.Orders.CompleteAsync("ORDER-1");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
        Assert.Equal(HttpMethod.Get, handler.Requests[3].Method);
    }

    [Fact]
    public async Task CompleteAsync_LeavesAnOrderNotYetApproved_Alone()
    {
        var (client, handler) = ClientWith(
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.OK, OrderJson("CREATED")));

        var result = await client.Orders.CompleteAsync("ORDER-1");

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsPaid);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task CompleteAsync_ReportsADeclinedCard_ThroughTheErrorIssue()
    {
        var (client, _) = ClientWith(
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.OK, OrderJson("APPROVED")),
            (HttpStatusCode.UnprocessableEntity, IssueJson(PayPalIssues.InstrumentDeclined)));

        var result = await client.Orders.CompleteAsync("ORDER-1");

        Assert.False(result.IsSuccess);
        Assert.Equal("UNPROCESSABLE_ENTITY", result.Error!.Name);
        Assert.Equal(PayPalIssues.InstrumentDeclined, result.Error.Issue);
        Assert.True(result.Error.HasIssue("instrument_declined"));
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("DECLINED")]
    public void IsPaid_IsFalse_ForACompletedOrderWhoseCaptureDidNotComplete(string captureStatus)
    {
        // This project runs with reflection-based JSON off by default (see the .csproj).
        var options = new System.Text.Json.JsonSerializerOptions
        {
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        var order = System.Text.Json.JsonSerializer.Deserialize<Order>(OrderJson("COMPLETED", captureStatus), options)!;

        Assert.Equal("COMPLETED", order.Status);
        Assert.False(order.IsPaid);
    }

    [Fact]
    public async Task CaptureAsync_WithARequestId_SendsIt()
    {
        var (client, handler) = ClientWith(
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.Created, OrderJson("COMPLETED", "COMPLETED")));

        await client.Orders.CaptureAsync("ORDER-1", requestId: "attempt-42");

        Assert.Equal("attempt-42", Header(handler.Requests[1], "PayPal-Request-Id"));
    }

    [Fact]
    public async Task SendAsync_On401_GetsAFreshTokenAndResendsOnce_WithTheSameRequestIdAndBody()
    {
        var (client, handler) = ClientWith(
            (HttpStatusCode.OK, "{\"access_token\":\"old-token\",\"expires_in\":32400}"),
            (HttpStatusCode.Unauthorized, "{\"error\":\"invalid_token\"}"),
            (HttpStatusCode.OK, "{\"access_token\":\"new-token\",\"expires_in\":32400}"),
            (HttpStatusCode.Created, OrderJson("CREATED")));

        var result = await client.Orders.CreateAsync(new OrderRequest
        {
            PurchaseUnits = { new PurchaseUnit { Amount = new AmountWithBreakdown(10.00m, "USD") } }
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(4, handler.Requests.Count);
        Assert.Equal("new-token", handler.Requests[3].Headers.Authorization!.Parameter);
        Assert.Equal(Header(handler.Requests[1], "PayPal-Request-Id"), Header(handler.Requests[3], "PayPal-Request-Id"));
        Assert.Equal(handler.RequestBodies[1], handler.RequestBodies[3]);
        Assert.Contains("\"value\":\"10.00\"", handler.RequestBodies[3]);
    }

    [Fact]
    public async Task SendAsync_On401Twice_ReturnsTheFailure_WithoutLooping()
    {
        var (client, handler) = ClientWith(
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.Unauthorized, "{\"error\":\"invalid_token\"}"),
            (HttpStatusCode.OK, Token),
            (HttpStatusCode.Unauthorized, "{\"error\":\"invalid_token\"}"));

        var result = await client.Orders.GetAsync("ORDER-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal(4, handler.Requests.Count);
    }
}
