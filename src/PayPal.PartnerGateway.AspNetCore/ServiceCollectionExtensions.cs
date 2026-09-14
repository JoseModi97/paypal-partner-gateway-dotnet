using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PayPal.PartnerGateway;

namespace PayPal.PartnerGateway.AspNetCore;

/// <summary>
/// Extension methods for setting up the PayPal Partner APIs client in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "PayPalPartner";

    /// <summary>
    /// Adds <see cref="PayPalPartnerClient"/> to the DI container, configured via a delegate (or
    /// left to environment-variable defaults if <paramref name="configure"/> is omitted).
    /// </summary>
    public static IServiceCollection AddPayPalPartnerGateway(
        this IServiceCollection services,
        Action<PayPalPartnerConfig>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<PayPalPartnerConfig>();
        }

        return services.AddPayPalPartnerGatewayCore();
    }

    /// <summary>
    /// Adds <see cref="PayPalPartnerClient"/> to the DI container, binding configuration from the
    /// given <see cref="IConfiguration"/> section (default: "PayPalPartner").
    /// </summary>
    public static IServiceCollection AddPayPalPartnerGateway(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DefaultConfigurationSection)
    {
        services.Configure<PayPalPartnerConfig>(configuration.GetSection(sectionName));
        return services.AddPayPalPartnerGatewayCore();
    }

    private static IServiceCollection AddPayPalPartnerGatewayCore(this IServiceCollection services)
    {
        services.AddHttpClient(nameof(PayPalPartnerClient));

        services.AddSingleton(sp =>
        {
            var options = sp.GetService<IOptions<PayPalPartnerConfig>>()?.Value ?? new PayPalPartnerConfig();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(PayPalPartnerClient));
            return new PayPalPartnerClient(options, httpClient);
        });

        return services;
    }
}
