using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Domain.Interfaces;
using MyDeezerStream.Infrastructure.Data;
using MyDeezerStream.Infrastructure.Repositories;

namespace MyDeezerStream.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' non configurée.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    // Stratégie de réessai en cas d'échec de connexion
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }
            ));

        // Enregistrement des repositories
        services.AddScoped<IStreamRepository, StreamRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<ITrackRepository, TrackRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}