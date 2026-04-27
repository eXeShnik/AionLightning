using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace AionLightning.Commons.Database;

public static class AionDataSourceExtensions
{
    public static IServiceCollection AddAionDataSource(
        this IServiceCollection services,
        IConfiguration config,
        string connectionStringName,
        object? serviceKey = null)
    {
        var cs = config.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' not found in configuration.");

        var dataSource = new MySqlDataSourceBuilder(cs).Build();

        if (serviceKey is null)
            services.AddSingleton(dataSource);
        else
            services.AddKeyedSingleton(serviceKey, dataSource);

        return services;
    }
}
