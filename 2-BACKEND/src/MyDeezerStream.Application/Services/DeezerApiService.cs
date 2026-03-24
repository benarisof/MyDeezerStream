using System.Text.Json;
using MyDeezerStream.Application.Interfaces;

namespace MyDeezerStream.Infrastructure.Services;

public class DeezerApiService : IDeezerApiService
{
    private readonly HttpClient _httpClient;

    public DeezerApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.deezer.com/");
    }

    public async Task<string?> GetArtistCoverAsync(string artistName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"search/artist?q={Uri.EscapeDataString(artistName)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            // On prend le premier résultat et on récupère la "picture_medium"
            if (json.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
            {
                return data[0].GetProperty("picture_medium").GetString();
            }
        }
        catch (Exception) { /* Gérer les logs ici */ }

        return null;
    }

    public async Task<string?> GetAlbumCoverAsync(string albumName, string artistName)
    {
        try
        {
            // Recherche de l'album combiné avec le nom de l'artiste pour plus de précision
            var query = $"{albumName} {artistName}";
            var response = await _httpClient.GetAsync($"search/album?q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
            {
                return data[0].GetProperty("cover_medium").GetString();
            }
        }
        catch (Exception) { /* Gérer les logs ici */ }

        return null;
    }

    public async Task<string?> GetTrackCoverAsync(string trackName, string artistName)
    {
        try
        {
            var query = $"{trackName} {artistName}";
            var response = await _httpClient.GetAsync($"search/track?q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonDocument.Parse(content);

            // Chez Deezer, la cover d'un track se trouve dans l'objet "album" rattaché au track
            if (json.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
            {
                var album = data[0].GetProperty("album");
                return album.GetProperty("cover_medium").GetString();
            }
        }
        catch (Exception) { /* Gérer les logs */ }

        return null;
    }
}