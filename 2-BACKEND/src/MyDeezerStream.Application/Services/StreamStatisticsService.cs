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

    public async Task<List<TopArtistDto>> GetTopArtistsAsync(int limit, int days)
    {
        var user = await _currentUserManager.GetCurrentUserAsync();

        // MODIFICATION : Si days vaut -1 (paramètre 'all'), since devient null.
        DateTime? since = days > 0 ? DateTime.UtcNow.AddDays(-days) : null;

        var results = await _streamRepository.GetTopArtistsAsync(user.Id, limit, since);

        // 1. Isoler ceux qui n'ont pas de cover
        var missingCovers = results.Where(r => string.IsNullOrEmpty(r.CoverUrl)).ToList();

        // 2. Lancer les appels à l'API Deezer en PARALLÈLE
        var fetchTasks = missingCovers.Select(async r =>
        {
            var url = await _deezerApiService.GetArtistCoverAsync(r.ArtistName);
            return new { r.ArtistId, Url = url };
        });

        var fetchedResults = await Task.WhenAll(fetchTasks);

        // On crée un dictionnaire rapide pour retrouver les nouvelles URL
        var newCoversDict = fetchedResults
            .Where(x => !string.IsNullOrEmpty(x.Url))
            .ToDictionary(x => x.ArtistId, x => x.Url);

        // 3. Mettre à jour la BDD SÉQUENTIELLEMENT (car le DbContext n'est pas thread-safe)
        foreach (var newCover in newCoversDict)
        {
            await _streamRepository.UpdateArtistCoverAsync(newCover.Key, newCover.Value!);
        }

        // 4. Mapper vers les DTO finaux
        return results.Select(r => new TopArtistDto
        {
            Artist = r.ArtistName,
            Count = r.Count,
            // On prend la cover en BDD, ou la nouvelle si on vient de la fetcher
            CoverUrl = !string.IsNullOrEmpty(r.CoverUrl) ? r.CoverUrl : newCoversDict.GetValueOrDefault(r.ArtistId)
        }).ToList();
    }

    public async Task<List<TopTrackDto>> GetTopTracksAsync(int limit, int days)
    {
        var user = await _currentUserManager.GetCurrentUserAsync();
        DateTime? since = days > 0 ? DateTime.UtcNow.AddDays(-days) : null;

        var results = await _streamRepository.GetTopTracksAsync(user.Id, limit, since);

        var missingCovers = results.Where(r => string.IsNullOrEmpty(r.CoverUrl)).ToList();

        var fetchTasks = missingCovers.Select(async r =>
        {
            // On peut passer l'artiste pour affiner la recherche sur l'API Deezer
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

    public async Task<List<TopAlbumDto>> GetTopAlbumsAsync(int limit, int days)
    {
        var user = await _currentUserManager.GetCurrentUserAsync();
        DateTime? since = days > 0 ? DateTime.UtcNow.AddDays(-days) : null;

        var results = await _streamRepository.GetTopAlbumsAsync(user.Id, limit, since);

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
        // 1. Récupérer l'utilisateur courant
        var user = await _currentUserManager.GetCurrentUserAsync();

        // 2. Récupérer les streams via le repository 
        // (La méthode existante GetStreamsAsync trie déjà par OrderByDescending(s => s.PlayedAt))
        var streams = await _streamRepository.GetStreamsAsync(user.Id);

        // 3. Mapper vers RawStreamDto en respectant la limite
        return streams
            .Take(limit)
            .Select(s => new RawStreamDto
            {
                SongTitle = s.Track.TrackName,
                // On concatène les artistes s'il y en a plusieurs (ex: "Artiste A, Artiste B")
                Artist = string.Join(", ", s.Track.TrackArtists.Select(ta => ta.Artist.ArtistName)),
                AlbumTitle = s.Track.Album?.AlbumName ?? "Single",
                ListeningTime = s.ListeningTime,
                Date = s.PlayedAt
            })
            .ToList();
    }
}