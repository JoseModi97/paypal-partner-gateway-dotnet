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

    /// <summary>Captures payment for an order (an order created with <c>intent: "CAPTURE"</c> that the buyer has approved).</summary>
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
