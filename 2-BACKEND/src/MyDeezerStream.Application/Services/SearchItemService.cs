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

        /// <summary>
        /// Calcule la plage de dates (identique à celle du StreamStatisticsService)
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

            DateTime? since = days > 0 ? DateTime.UtcNow.AddDays(-days) : null;
            return (since, null);
        }

        public async Task<ArtistDto> GetArtistDetailsAsync(string artistName, int days, string? range)
        {
            var user = await _currentUserManager.GetCurrentUserAsync();
            var (start, end) = GetDateRange(days, range);

            var artist = await _artistRepository.GetByNameAsync(artistName);
            if (artist == null)
            {
                throw new KeyNotFoundException($"Artist '{artistName}' not found.");
            }

            // Note: Update de la signature du repo nécessaire pour accepter 'end'
            var trackStats = await _artistRepository.GetTrackStatsForArtistAsync(artist.ArtistId, user.Id, start, end);

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


        public async Task<AlbumDto> GetAlbumDetailsAsync(string albumName, string artistName, int days, string? range)
        {
            var user = await _currentUserManager.GetCurrentUserAsync();
            var (start, end) = GetDateRange(days, range);

            var album = await _albumRepository.GetByNameAndArtistAsync(albumName, artistName);
            if (album == null)
            {
                throw new KeyNotFoundException($"Album '{albumName}' by '{artistName}' not found.");
            }

            // Note: Update de la signature du repo nécessaire pour accepter 'end'
            var trackStats = await _albumRepository.GetTrackStatsForAlbumAsync(album.AlbumId, user.Id, start, end);

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
                return [];

            var albumProjections = await _albumRepository.SearchAlbumsAsync(query);
            var artistProjections = await _artistRepository.SearchArtistsAsync(query);

            var albumSuggestions = albumProjections.Select(a => new SearchSuggestionDto
            {
                Id = a.Id,
                DisplayName = a.Name,
                Subtitle = a.Artist,
                Type = "album",
                CoverUrl = a.CoverUrl
            });

            var artistSuggestions = artistProjections.Select(a => new SearchSuggestionDto
            {
                Id = a.Id,
                DisplayName = a.Name,
                Subtitle = "Artiste",
                Type = "artist",
                CoverUrl = a.CoverUrl
            });

            return [.. artistSuggestions
                .Concat(albumSuggestions)
                .OrderByDescending(s => s.DisplayName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(s => s.DisplayName)];
        }
    }
}