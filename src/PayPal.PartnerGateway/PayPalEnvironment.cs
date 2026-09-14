namespace PayPal.PartnerGateway;

/// <summary>
/// The PayPal API environment to send requests to.
/// </summary>
public enum PayPalEnvironment
{
    /// <summary>
    /// https://api-m.sandbox.paypal.com - safe for development and testing.
    /// </summary>
    Sandbox,

    /// <summary>
    /// https://api-m.paypal.com - real money, real merchants.
    /// </summary>
    Live
}
