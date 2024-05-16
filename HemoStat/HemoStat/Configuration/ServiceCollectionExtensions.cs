using HemoStat.Services;

namespace HemoStat.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserTimeProvider(this IServiceCollection services)
        => services.AddTransient<TimeProvider, BrowserTimeProvider>();
}
