using MyDeezerStream.Application.DTOs;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Domain.Interfaces;

namespace MyDeezerStream.Application.Services
{
    public class SearchItemService : ISearchItem
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IArtistRepository _artistRepository;
        private readonly CurrentUserManager _currentUserManager;
        private readonly IDeezerApiService _deezerApiService;

        public SearchItemService(
            IAlbumRepository albumRepository,
            IArtistRepository artistRepository,
            CurrentUserManager currentUserManager,
            IDeezerApiService deezerApiService)
        {
            _albumRepository = albumRepository;
            _artistRepository = artistRepository;
            _currentUserManager = currentUserManager;
            _deezerApiService = deezerApiService;
        }

        public async Task<ArtistDto> GetArtistDetailsAsync(string artistName, int days)
        {
            var user = await _currentUserManager.GetCurrentUserAsync();
            DateTime? since = days > 0 ? DateTime.UtcNow.AddDays(-days) : null;

            var artist = await _artistRepository.GetByNameAsync(artistName);
            if (artist == null)
            {
                throw new KeyNotFoundException($"Artist '{artistName}' not found.");
            }

            var trackStats = await _artistRepository.GetTrackStatsForArtistAsync(artist.ArtistId, user.Id, since);

            string? coverUrl = artist.CoverUrl;
            if (string.IsNullOrEmpty(coverUrl))
            {
                coverUrl = await _deezerApiService.GetArtistCoverAsync(artistName);
                if (!string.IsNullOrEmpty(coverUrl))
                {
                    await _artistRepository.UpdateCoverAsync(artist.ArtistId, coverUrl);
                }
            }

            return new ArtistDto
            {
                Name = artist.ArtistName,
                Count = trackStats.Sum(ts => ts.Count),
                ListeningTime = trackStats.Sum(ts => ts.TotalListeningTime), 
                CoverUrl = coverUrl ?? string.Empty,
                trackDtos = trackStats.Select(ts => new TrackDto
                {
                    Name = ts.TrackName,
                    Album = ts.AlbumName,
                    ListeningTime = ts.TotalListeningTime,
                    Count = ts.Count
                }).ToList()
            };
        }


        public async Task<AlbumDto> GetAlbumDetailsAsync(string albumName, string artistName, int days)
        {
            var user = await _currentUserManager.GetCurrentUserAsync();
            DateTime? since = days > 0 ? DateTime.UtcNow.AddDays(-days) : null;

            var album = await _albumRepository.GetByNameAndArtistAsync(albumName, artistName);
            if (album == null)
            {
                throw new KeyNotFoundException($"Album '{albumName}' by '{artistName}' not found.");
            }

            var trackStats = await _albumRepository.GetTrackStatsForAlbumAsync(album.AlbumId, user.Id, since);

            string? coverUrl = album.CoverUrl;
            if (string.IsNullOrEmpty(coverUrl))
            {
                coverUrl = await _deezerApiService.GetAlbumCoverAsync(albumName, artistName);
                if (!string.IsNullOrEmpty(coverUrl))
                {
                    await _albumRepository.UpdateCoverAsync(album.AlbumId, coverUrl);
                }
            }

            return new AlbumDto
            {
                Name = album.AlbumName,
                Artist = artistName,
                Count = trackStats.Sum(ts => ts.Count),
                ListeningTime = trackStats.Sum(ts => ts.TotalListeningTime),
                CoverUrl = coverUrl ?? string.Empty,
                //ReleaseDate = album.ReleaseDate,
                trackDtos = trackStats.Select(ts => new TrackDto
                {
                    Name = ts.TrackName,
                    Album = ts.AlbumName,
                    ListeningTime = ts.TotalListeningTime,
                    Count = ts.Count
                }).ToList()
            };
        }

        public async Task<List<SearchSuggestionDto>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<SearchSuggestionDto>();

            // Récupération depuis les repos (projections)
            var albumProjections = await _albumRepository.SearchAlbumsAsync(query);
            var artistProjections = await _artistRepository.SearchArtistsAsync(query);

            // Mapping vers DTO
            var albumSuggestions = albumProjections
                .Select(a => new SearchSuggestionDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Artist = a.Artist,
                    Type = "album",
                    CoverUrl = a.CoverUrl
                })
                .ToList();

            var artistSuggestions = artistProjections
                .Select(a => new SearchSuggestionDto
                {
                    Id = a.Id,
                    Artist = a.Name,
                    Type = "artist",
                    CoverUrl = a.CoverUrl
                })
                .ToList();

            // Fusion et tri
            return albumSuggestions
                .Concat(artistSuggestions)
                .OrderByDescending(s => s.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(s => s.Name)
                .ToList();
        }
    }
}
