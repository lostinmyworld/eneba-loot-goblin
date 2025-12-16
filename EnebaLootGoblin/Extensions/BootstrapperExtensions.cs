using EnebaLootGoblin.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Social.Oversharers.Extensions;
using Social.Overthinkers.Extensions;

namespace EnebaLootGoblin.Extensions;

public static class BootstrapperExtensions
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSocialOverSharers();
        services.AddSocialOverThinkers();

        services.AddSingleton<IParser, Parser>();
        services.AddSingleton<IApiClient, ApiClient>();

        return services;
    }
}
