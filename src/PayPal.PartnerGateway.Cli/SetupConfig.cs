namespace PayPal.PartnerGateway.Cli;

/// <summary>The PayPal partner settings the wizard collects and writes to <c>appsettings.json</c>.</summary>
internal class SetupConfig
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Environment { get; set; }
    public string? PartnerAttributionId { get; set; }
    public string? WebhookId { get; set; }
}
