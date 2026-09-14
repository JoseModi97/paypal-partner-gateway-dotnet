using Microsoft.Extensions.DependencyInjection;
using PayPal.PartnerGateway.AspNetCore;
using Xunit;

namespace PayPal.PartnerGateway.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPayPalPartnerGateway_ResolvesConfiguredClient()
    {
        var services = new ServiceCollection();
        services.AddPayPalPartnerGateway(config =>
        {
            config.ClientId = "id";
            config.ClientSecret = "secret";
            config.Environment = PayPalEnvironment.Live;
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<PayPalPartnerClient>();

        Assert.Equal("id", client.Gateway.Config.ClientId);
        Assert.Equal(PayPalPartnerConfig.DefaultLiveUrl, client.Gateway.BaseUrl);
        Assert.NotNull(client.Orders);
        Assert.NotNull(client.PartnerReferrals);
        Assert.NotNull(client.Webhooks);
    }
}
