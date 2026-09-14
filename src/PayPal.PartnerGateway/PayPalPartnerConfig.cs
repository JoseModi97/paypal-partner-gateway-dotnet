using System;

namespace PayPal.PartnerGateway;

/// <summary>
/// Configuration options for the PayPal Partner APIs gateway.
/// Get your Client ID and Secret from the PayPal Developer Dashboard
/// (https://developer.paypal.com/dashboard/applications) - one set for Sandbox, one for Live.
/// </summary>
public class PayPalPartnerConfig
{
    public const string DefaultSandboxUrl = "https://api-m.sandbox.paypal.com";
    public const string DefaultLiveUrl = "https://api-m.paypal.com";

    /// <summary>
    /// Your app's Client ID from the PayPal Developer Dashboard.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Your app's Client Secret from the PayPal Developer Dashboard.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Sandbox (default) or Live. Determines the base URL unless <see cref="BaseUrl"/> is set explicitly.
    /// </summary>
    public PayPalEnvironment Environment { get; set; } = PayPalEnvironment.Sandbox;

    /// <summary>
    /// Overrides the base URL derived from <see cref="Environment"/>. Leave unset unless you need
    /// to point at a proxy or a PayPal API host other than the standard sandbox/live hosts.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Optional Build Notation (BN) code identifying you as a PayPal partner, sent as the
    /// <c>PayPal-Partner-Attribution-Id</c> header on every request. Required to receive revenue attribution.
    /// Find yours at https://developer.paypal.com/api/rest/responses/#partner-attribution-id
    /// </summary>
    public string? PartnerAttributionId { get; set; }

    /// <summary>
    /// The Partner Merchant ID (your own PayPal merchant/payer ID as the partner), used by some
    /// Partner Referral / seller-status endpoints. Optional - only needed for those calls.
    /// </summary>
    public string? PartnerId { get; set; }

    /// <summary>
    /// The webhook ID from the PayPal Developer Dashboard (Webhooks tab) for the webhook you
    /// created against this app. Required to use <c>MapPayPalPartnerWebhook</c> / the webhook
    /// signature verification helpers without passing a webhook ID on every call.
    /// </summary>
    public string? WebhookId { get; set; }

    /// <summary>
    /// How long an OAuth2 access token is cached before it is proactively refreshed, expressed as a
    /// safety margin subtracted from PayPal's reported <c>expires_in</c>. Defaults to 60 seconds.
    /// </summary>
    public TimeSpan TokenExpiryBuffer { get; set; } = TimeSpan.FromSeconds(60);

    public PayPalPartnerConfig()
    {
        var envClientId = System.Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID");
        if (!string.IsNullOrWhiteSpace(envClientId)) ClientId = envClientId!;

        var envSecret = System.Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET");
        if (!string.IsNullOrWhiteSpace(envSecret)) ClientSecret = envSecret!;

        var envEnvironment = System.Environment.GetEnvironmentVariable("PAYPAL_ENVIRONMENT");
        if (!string.IsNullOrWhiteSpace(envEnvironment) &&
            Enum.TryParse<PayPalEnvironment>(envEnvironment, ignoreCase: true, out var parsedEnv))
        {
            Environment = parsedEnv;
        }

        var envBaseUrl = System.Environment.GetEnvironmentVariable("PAYPAL_BASE_URL");
        if (!string.IsNullOrWhiteSpace(envBaseUrl)) BaseUrl = envBaseUrl;

        var envPartnerAttributionId = System.Environment.GetEnvironmentVariable("PAYPAL_PARTNER_ATTRIBUTION_ID");
        if (!string.IsNullOrWhiteSpace(envPartnerAttributionId)) PartnerAttributionId = envPartnerAttributionId;

        var envPartnerId = System.Environment.GetEnvironmentVariable("PAYPAL_PARTNER_ID");
        if (!string.IsNullOrWhiteSpace(envPartnerId)) PartnerId = envPartnerId;

        var envWebhookId = System.Environment.GetEnvironmentVariable("PAYPAL_WEBHOOK_ID");
        if (!string.IsNullOrWhiteSpace(envWebhookId)) WebhookId = envWebhookId;
    }

    /// <summary>
    /// Resolves the effective base URL: <see cref="BaseUrl"/> if set, otherwise the standard
    /// sandbox/live host for <see cref="Environment"/>.
    /// </summary>
    public string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl)) return BaseUrl!.TrimEnd('/');
        return Environment == PayPalEnvironment.Live ? DefaultLiveUrl : DefaultSandboxUrl;
    }

    public void AssertConfigured()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("The PayPal gateway 'ClientId' setting is required.");
        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new InvalidOperationException("The PayPal gateway 'ClientSecret' setting is required.");
    }
}
