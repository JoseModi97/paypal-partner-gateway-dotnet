using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Models;

namespace PayPal.PartnerGateway.Resources;

/// <summary>
/// Adds shipment tracking to captured payments so buyers see delivery status in PayPal.
/// Access via <c>client.ShipmentTracking</c>.
/// </summary>
public class ShipmentTrackingResource
{
    private readonly PayPalPartnerGateway _gateway;

    internal ShipmentTrackingResource(PayPalPartnerGateway gateway)
    {
        _gateway = gateway;
    }

    /// <summary>Adds tracking for a single captured order. Equivalent to <c>client.Orders.AddTrackingAsync</c>.</summary>
    public Task<PayPalApiResult<JsonNode>> AddAsync(
        string orderId,
        TrackingInfo tracking,
        string? authAssertion = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("Order ID is required.", nameof(orderId));
        return _gateway.SendAsync<JsonNode>(
            HttpMethod.Post, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/track", tracking,
            authAssertion: authAssertion, cancellationToken: cancellationToken);
    }

    /// <summary>Adds or updates tracking for up to 20 transactions in a single call.</summary>
    public Task<PayPalApiResult<JsonNode>> AddBatchAsync(
        BatchTrackerRequest request,
        string? authAssertion = null,
        CancellationToken cancellationToken = default) =>
        _gateway.SendAsync<JsonNode>(
            HttpMethod.Post, "/v1/shipping/trackers-batch", request,
            authAssertion: authAssertion, cancellationToken: cancellationToken);
}
