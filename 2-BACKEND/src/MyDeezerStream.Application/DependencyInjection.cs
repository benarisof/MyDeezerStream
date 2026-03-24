using Microsoft.Extensions.DependencyInjection;
using MyDeezer.Application.Services;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Application.Services;
using MyDeezerStream.Infrastructure.Services; 

namespace MyDeezer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Services métier
        services.AddScoped<IStreamStatisticsService, StreamStatisticsService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<ISearchItem, SearchItemService>();
        // Gestionnaire d'utilisateur courant
        services.AddScoped<CurrentUserManager>();

        // Injection du service d'API Deezer avec configuration du HttpClient
        services.AddHttpClient<IDeezerApiService, DeezerApiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.deezer.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyDeezerStream-App");
        });

        return services;
    }
}