using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>Request body for registering/unregistering an Apple Pay web domain.</summary>
public class WalletDomainRequest
{
    /// <summary>Currently only "APPLE_PAY".</summary>
    [JsonPropertyName("provider_type")]
    public string ProviderType { get; set; } = "APPLE_PAY";

    [JsonPropertyName("domain")]
    public WalletDomainName Domain { get; set; } = new();

    /// <summary>Only used when unregistering.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    public WalletDomainRequest()
    {
    }

    public WalletDomainRequest(string domainName, string? reason = null)
    {
        Domain = new WalletDomainName { Name = domainName };
        Reason = reason;
    }
}

public class WalletDomainName
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
