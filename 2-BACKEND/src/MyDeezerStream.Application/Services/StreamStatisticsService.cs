using MyDeezerStream.Application.DTOs;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Application.Services;
using MyDeezerStream.Domain.Interfaces;

namespace MyDeezer.Application.Services;

public class StreamStatisticsService : IStreamStatisticsService
{
    private readonly IStreamRepository _streamRepository;
    private readonly CurrentUserManager _currentUserManager;
    private readonly IDeezerApiService _deezerApiService;

    public StreamStatisticsService(
        IStreamRepository streamRepository,
        CurrentUserManager currentUserManager,
        IDeezerApiService deezerApiService)
    {
        _streamRepository = streamRepository;
        _currentUserManager = currentUserManager;
        _deezerApiService = deezerApiService;
    }

    /// <summary>
    /// Calcule la plage de dates en fonction des jours ou du range nommé.
    /// Retourne un tuple (DateDébut, DateFin).
    /// </summary>
    private (DateTime? start, DateTime? end) GetDateRange(int days, string? range)
    {
        if (!string.IsNullOrEmpty(range))
        {
            var now = DateTime.UtcNow;
            if (range == "this_year")
            {
                return (new DateTime(now.Year, 1, 1), now);
            }
            if (range == "last_year")
            {
                var lastYear = now.Year - 1;
                return (new DateTime(lastYear, 1, 1), new DateTime(lastYear, 12, 31, 23, 59, 59));
            }
        }

        // Si pas de range, on retombe sur la logique "days" (glissant)
        DateTime? since = days > 0 ? DateTime.UtcNow.AddDays(-days) : null;
        return (since, null);
    }

    public async Task<List<TopArtistDto>> GetTopArtistsAsync(int limit, int days, string? range = null)
    {
        var user = await _currentUserManager.GetCurrentUserAsync();
        var (start, end) = GetDateRange(days, range);

        // Note: Il faudra s'assurer que le Repository accepte désormais 'end' en paramètre optionnel
        var results = await _streamRepository.GetTopArtistsAsync(user.Id, limit, start, end);

        var missingCovers = results.Where(r => string.IsNullOrEmpty(r.CoverUrl)).ToList();

        var fetchTasks = missingCovers.Select(async r =>
        {
            var url = await _deezerApiService.GetArtistCoverAsync(r.ArtistName);
            return new { r.ArtistId, Url = url };
        });

        var fetchedResults = await Task.WhenAll(fetchTasks);

        var newCoversDict = fetchedResults
            .Where(x => !string.IsNullOrEmpty(x.Url))
            .ToDictionary(x => x.ArtistId, x => x.Url);

        foreach (var newCover in newCoversDict)
        {
            await _streamRepository.UpdateArtistCoverAsync(newCover.Key, newCover.Value!);
        }

        return results.Select(r => new TopArtistDto
        {
            Artist = r.ArtistName,
            Count = r.Count,
            CoverUrl = !string.IsNullOrEmpty(r.CoverUrl) ? r.CoverUrl : newCoversDict.GetValueOrDefault(r.ArtistId)
        }).ToList();
    }

    public async Task<List<TopTrackDto>> GetTopTracksAsync(int limit, int days, string? range = null)
    {
        var user = await _currentUserManager.GetCurrentUserAsync();
        var (start, end) = GetDateRange(days, range);

        var results = await _streamRepository.GetTopTracksAsync(user.Id, limit, start, end);

        var missingCovers = results.Where(r => string.IsNullOrEmpty(r.CoverUrl)).ToList();

        var fetchTasks = missingCovers.Select(async r =>
        {
            var url = await _deezerApiService.GetTrackCoverAsync(r.TrackName, r.ArtistNames);
            return new { r.TrackId, Url = url };
        });

        var fetchedResults = await Task.WhenAll(fetchTasks);

        var newCoversDict = fetchedResults
            .Where(x => !string.IsNullOrEmpty(x.Url))
            .ToDictionary(x => x.TrackId, x => x.Url);

        foreach (var newCover in newCoversDict)
        {
            await _streamRepository.UpdateTrackCoverAsync(newCover.Key, newCover.Value!);
        }

        return results.Select(r => new TopTrackDto
        {
            Track = r.TrackName,
            Artist = r.ArtistNames,
            Count = r.Count,
            CoverUrl = !string.IsNullOrEmpty(r.CoverUrl) ? r.CoverUrl : newCoversDict.GetValueOrDefault(r.TrackId)
        }).ToList();
    }

    public async Task<List<TopAlbumDto>> GetTopAlbumsAsync(int limit, int days, string? range = null)
    {
        var user = await _currentUserManager.GetCurrentUserAsync();
        var (start, end) = GetDateRange(days, range);

        var results = await _streamRepository.GetTopAlbumsAsync(user.Id, limit, start, end);

        var missingCovers = results.Where(r => string.IsNullOrEmpty(r.CoverUrl)).ToList();

        var fetchTasks = missingCovers.Select(async r =>
        {
            var url = await _deezerApiService.GetAlbumCoverAsync(r.AlbumName, r.ArtistName);
            return new { r.AlbumId, Url = url };
        });

        var fetchedResults = await Task.WhenAll(fetchTasks);

        var newCoversDict = fetchedResults
            .Where(x => !string.IsNullOrEmpty(x.Url))
            .ToDictionary(x => x.AlbumId, x => x.Url);

        foreach (var newCover in newCoversDict)
        {
            await _streamRepository.UpdateAlbumCoverAsync(newCover.Key, newCover.Value!);
        }

        return results.Select(r => new TopAlbumDto
        {
            Album = r.AlbumName,
            Artist = r.ArtistName,
            Count = r.Count,
            CoverUrl = !string.IsNullOrEmpty(r.CoverUrl) ? r.CoverUrl : newCoversDict.GetValueOrDefault(r.AlbumId)
        }).ToList();
    }

    public async Task<List<RawStreamDto>> GetLastStreamAsync(int limit)
    {
        var user = await _currentUserManager.GetCurrentUserAsync();
        var streams = await _streamRepository.GetStreamsAsync(user.Id);

        return streams
            .Take(limit)
            .Select(s => new RawStreamDto
            {
                SongTitle = s.Track.TrackName,
                Artist = string.Join(", ", s.Track.TrackArtists.Select(ta => ta.Artist.ArtistName)),
                AlbumTitle = s.Track.Album?.AlbumName ?? "Single",
                ListeningTime = s.ListeningTime,
                Date = s.PlayedAt
            })
            .ToList();
    }
}