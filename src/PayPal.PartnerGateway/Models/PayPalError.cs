using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// The error body PayPal returns for a non-2xx response.
/// See https://developer.paypal.com/api/rest/responses/#error
/// </summary>
public class PayPalError
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("debug_id")]
    public string? DebugId { get; set; }

    [JsonPropertyName("details")]
    public List<PayPalErrorDetail>? Details { get; set; }

    [JsonPropertyName("links")]
    public List<LinkDescription>? Links { get; set; }

    /// <summary>
    /// The specific reason for the failure: the first detail's <c>issue</c>, e.g.
    /// <c>ORDER_ALREADY_CAPTURED</c>, <c>INSTRUMENT_DECLINED</c> or <c>ORDER_NOT_APPROVED</c>.
    /// <see cref="Name"/> is only the broad category (<c>UNPROCESSABLE_ENTITY</c>), so branch on
    /// this instead. Null when PayPal sent no details.
    /// </summary>
    [JsonIgnore]
    public string? Issue => Details is { Count: > 0 } ? Details[0].Issue : null;

    /// <summary>True when any detail reports <paramref name="issue"/> (e.g. <c>"INSTRUMENT_DECLINED"</c>).</summary>
    public bool HasIssue(string issue)
    {
        if (Details == null) return false;
        foreach (var detail in Details)
        {
            if (string.Equals(detail.Issue, issue, System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}

/// <summary>The <c>issue</c> codes callers most often need to branch on.</summary>
public static class PayPalIssues
{
    /// <summary>The order was captured already - e.g. the buyer refreshed the return page. Not a failure.</summary>
    public const string OrderAlreadyCaptured = "ORDER_ALREADY_CAPTURED";

    /// <summary>The buyer hasn't approved the order yet (capture called too early).</summary>
    public const string OrderNotApproved = "ORDER_NOT_APPROVED";

    /// <summary>
    /// The card or funding source was declined at capture. Nothing was charged; the buyer must
    /// pay again with another card or PayPal (create a new order for the retry).
    /// </summary>
    public const string InstrumentDeclined = "INSTRUMENT_DECLINED";
}

public class PayPalErrorDetail
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("issue")]
    public string? Issue { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
