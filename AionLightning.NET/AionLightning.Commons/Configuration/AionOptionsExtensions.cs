using Microsoft.Extensions.DependencyInjection;

namespace AionLightning.Commons.Configuration;

public static class AionOptionsExtensions
{
    public static IServiceCollection AddAionOptions<T>(
        this IServiceCollection services,
        string sectionName) where T : class
    {
        services.AddOptions<T>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
