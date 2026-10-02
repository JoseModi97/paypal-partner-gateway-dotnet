using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// Creates and manages Checkout Orders (<c>/v2/checkout/orders</c>) - covers both the "Checkout"
/// and "Capture" sections of the PayPal Partner APIs, since they operate on the same resource.
/// Access via <c>client.Orders</c>.
/// </summary>
public class OrdersResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal OrdersResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    /// <summary>
    /// Creates an order. Pass <c>authAssertion</c> (build with <see cref="AuthAssertion.Create"/>)
    /// when processing on behalf of a connected merchant using your own partner access token.
    /// </summary>
    public Task<PayPalApiResult<Order>> CreateAsync(
        OrderRequest request,
        bool returnFullRepresentation = true,
        string? authAssertion = null,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<Order>(
            HttpMethod.Post,
            "/v2/checkout/orders",
            request,
            headers: PreferHeader(returnFullRepresentation),
            authAssertion: authAssertion,
            cancellationToken: cancellationToken);

    /// <summary>Retrieves an order by ID.</summary>
    public Task<PayPalApiResult<Order>> GetAsync(
        string orderId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(orderId, nameof(orderId));
        return _gateway.SendAsync<Order>(
            HttpMethod.Get, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}",
            authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Confirms the payment source for an order created without one (e.g. finalizing a card or
    /// vaulted-token payment before capture/authorization).
    /// </summary>
    public Task<PayPalApiResult<Order>> ConfirmPaymentSourceAsync(
        string orderId,
        JsonNode paymentSourcePayload,
        bool returnFullRepresentation = true,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(orderId, nameof(orderId));
        return _gateway.SendAsync<Order>(
            HttpMethod.Post,
            $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/confirm-payment-source",
            paymentSourcePayload,
            headers: PreferHeader(returnFullRepresentation),
            authAssertion: authAssertion,
            cancellationToken: cancellationToken);
    }

    /// <summary>Authorizes an order created with <c>intent: "AUTHORIZE"</c>, placing a hold on funds.</summary>
    public Task<PayPalApiResult<Order>> AuthorizeAsync(
        string orderId,
        bool returnFullRepresentation = true,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(orderId, nameof(orderId));
        return _gateway.SendAsync<Order>(
            HttpMethod.Post,
            $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/authorize",
            headers: PreferHeader(returnFullRepresentation),
            authAssertion: authAssertion,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Captures payment for an order (an order created with <c>intent: "CAPTURE"</c> that the buyer
    /// has approved). Each call is a new capture attempt: a second call fails with
    /// <c>ORDER_ALREADY_CAPTURED</c> - e.g. when the buyer refreshes your return page. To finish
    /// a payment safely from a return page or webhook, use <see cref="CompleteAsync"/> instead.
    /// </summary>
    public Task<PayPalApiResult<Order>> CaptureAsync(
        string orderId,
        bool returnFullRepresentation = true,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(orderId, nameof(orderId));
        return _gateway.SendAsync<Order>(
            HttpMethod.Post,
            $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture",
            headers: PreferHeader(returnFullRepresentation),
            authAssertion: authAssertion,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Captures payment for an approved order with your own <c>PayPal-Request-Id</c>. PayPal
    /// answers a repeat with the same id with the first call's result instead of capturing again,
    /// so retrying a capture whose response you never saw (timeout, crash) is safe. Use one id
    /// per payment attempt, e.g. <see cref="CaptureRequestId"/>.
    /// </summary>
    public Task<PayPalApiResult<Order>> CaptureAsync(
        string orderId,
        string requestId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(orderId, nameof(orderId));
        RequireId(requestId, nameof(requestId));
        var headers = PreferHeader(returnFullRepresentation: true);
        headers["PayPal-Request-Id"] = requestId;
        return _gateway.SendAsync<Order>(
            HttpMethod.Post,
            $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture",
            headers: headers,
            authAssertion: authAssertion,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Finishes a buyer-approved order and returns it as it now stands - safe to call any number
    /// of times, from anywhere: your return_url handler (including when the buyer refreshes it),
    /// a JS SDK <c>onApprove</c>, a webhook, or a background reconciler.
    /// <list type="bullet">
    /// <item>Reads the order first and captures only if it's <c>APPROVED</c>; orders not approved
    /// yet (<c>CREATED</c>, <c>PAYER_ACTION_REQUIRED</c>) and finished ones are returned unchanged.</item>
    /// <item>Captures with a <c>PayPal-Request-Id</c> fixed for the order
    /// (<see cref="CaptureRequestId"/>), so two calls racing each other get PayPal's single
    /// capture back rather than an error.</item>
    /// <item>Treats <c>ORDER_ALREADY_CAPTURED</c> as success: it reads the order again and returns it.</item>
    /// </list>
    /// Check <see cref="Order.IsPaid"/> on the result (not just <c>Status</c>), and compare
    /// <see cref="Order.Capture"/>'s amount with what you charged. A declined card comes back as
    /// a failure whose <see cref="PayPalError.Issue"/> is <see cref="PayPalIssues.InstrumentDeclined"/>:
    /// nothing was charged, and the retry needs a new order.
    /// </summary>
    public async Task<PayPalApiResult<Order>> CompleteAsync(
        string orderId,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(orderId, nameof(orderId));
        var current = await GetAsync(orderId, authAssertion, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || !string.Equals(current.Data?.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        var capture = await CaptureAsync(orderId, CaptureRequestId(orderId), authAssertion, cancellationToken)
            .ConfigureAwait(false);
        if (!capture.IsSuccess && capture.Error?.HasIssue(PayPalIssues.OrderAlreadyCaptured) == true)
        {
            return await GetAsync(orderId, authAssertion, cancellationToken).ConfigureAwait(false);
        }
        return capture;
    }

    /// <summary>The <c>PayPal-Request-Id</c> <see cref="CompleteAsync"/> captures an order with.</summary>
    public static string CaptureRequestId(string orderId) => "capture-" + orderId;

    /// <summary>Adds shipment tracking to a captured order (equivalent to <c>client.ShipmentTracking.AddAsync</c> but scoped to one order).</summary>
    public Task<PayPalApiResult<JsonNode>> AddTrackingAsync(
        string orderId,
        TrackingInfo tracking,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(orderId, nameof(orderId));
        return _gateway.SendAsync<JsonNode>(
            HttpMethod.Post,
            $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/track",
            tracking,
            authAssertion: authAssertion,
            cancellationToken: cancellationToken);
    }

    private static IDictionary<string, string> PreferHeader(bool returnFullRepresentation) =>
        new Dictionary<string, string>
        {
            ["Prefer"] = returnFullRepresentation ? "return=representation" : "return=minimal"
        };

    private static void RequireId(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
    }
}
