using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tunarrly.Core.Options;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.AI;
using Tunarrly.Infrastructure.Data;
using Tunarrly.Infrastructure.Lidarr;
using Tunarrly.Infrastructure.Recommendations;
using Tunarrly.Infrastructure.Scanning;
using Tunarrly.Infrastructure.Settings;

namespace Tunarrly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTunarrlyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LidarrOptions>(configuration.GetSection("Lidarr"));
        services.Configure<LibraryOptions>(configuration.GetSection("Library"));
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.Configure<AiOptions>(configuration.GetSection("AI"));
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.Configure<SecretsOptions>(configuration.GetSection("Secrets"));

        var databasePath = configuration["DATABASE:PATH"] ?? configuration["Database:Path"] ?? "data/tunarrly.db";
        var dataDirectory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        services.AddDbContextFactory<TunarrlyDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddHttpClient<ILidarrClient, LidarrClient>();
        services.AddHttpClient<IAiProviderClient, AiProviderClient>();
        services.AddScoped<IAiContextService, AiContextService>();
        services.AddSingleton<SecretProtector>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<ILidarrSyncService, LidarrSyncService>();
        services.AddScoped<ILibraryScanner, LibraryScanner>();
        services.AddSingleton<IScanJobQueue, ScanJobQueue>();
        services.AddSingleton<IScanCancellationCoordinator, ScanCancellationCoordinator>();
        services.AddScoped<IScanJobService, ScanJobService>();
        services.AddHostedService<ScanWorker>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
