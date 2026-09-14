using System;
using System.Net.Http;
using PayPal.PartnerGateway.Resources;

namespace PayPal.PartnerGateway;

/// <summary>
/// The beginner-friendly, idiomatic C# client for the PayPal Partner APIs. Handles OAuth2 token
/// acquisition/refresh and request signing for you - construct one and call its resource groups:
/// <c>client.PartnerReferrals</c> (onboard sellers), <c>client.Orders</c> (checkout/capture),
/// <c>client.Payments</c> (captures/authorizations/refunds), <c>client.Webhooks</c>,
/// <c>client.PaymentTokens</c> (vault), <c>client.ApplePay</c>, <c>client.ShipmentTracking</c>,
/// and <c>client.Onboarding</c> (Managed Path, limited release).
/// </summary>
public class PayPalPartnerClient
{
    private static readonly HttpClient SharedHttpClient = new();

    /// <summary>The low-level engine (token management, header building, generic SendAsync) behind every resource.</summary>
    public PayPalPartnerGateway Gateway { get; }

    public PartnerReferralsResource PartnerReferrals { get; }
    public OnboardingResource Onboarding { get; }
    public OrdersResource Orders { get; }
    public PaymentsResource Payments { get; }
    public ApplePayResource ApplePay { get; }
    public PaymentTokensResource PaymentTokens { get; }
    public ShipmentTrackingResource ShipmentTracking { get; }
    public WebhooksResource Webhooks { get; }

    /// <summary>Initializes a client reading settings from environment variables (PAYPAL_*).</summary>
    public PayPalPartnerClient() : this(new PayPalPartnerConfig())
    {
    }

    /// <summary>Initializes a client with explicit configuration options.</summary>
    public PayPalPartnerClient(PayPalPartnerConfig config, HttpClient? httpClient = null)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        Gateway = new PayPalPartnerGateway(config, httpClient ?? SharedHttpClient);

        PartnerReferrals = new PartnerReferralsResource(Gateway);
        Onboarding = new OnboardingResource(Gateway);
        Orders = new OrdersResource(Gateway);
        Payments = new PaymentsResource(Gateway);
        ApplePay = new ApplePayResource(Gateway);
        PaymentTokens = new PaymentTokensResource(Gateway);
        ShipmentTracking = new ShipmentTrackingResource(Gateway);
        Webhooks = new WebhooksResource(Gateway);
    }

    /// <summary>Initializes a client using a configuration action.</summary>
    public PayPalPartnerClient(Action<PayPalPartnerConfig> configure, HttpClient? httpClient = null)
        : this(Configure(configure), httpClient)
    {
    }

    private static PayPalPartnerConfig Configure(Action<PayPalPartnerConfig> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var config = new PayPalPartnerConfig();
        configure(config);
        return config;
    }
}
