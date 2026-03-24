using Microsoft.EntityFrameworkCore;
using MyDeezerStream.Domain.Entities.Search;
using MyDeezerStream.Domain.Entities.Stats;
using MyDeezerStream.Domain.Interfaces;
using MyDeezerStream.Infrastructure.Data;

namespace MyDeezerStream.Infrastructure.Repositories
{
    public class ArtistRepository : IArtistRepository
    {
        private readonly AppDbContext _context;

        public ArtistRepository(AppDbContext context)
        {
            _context = context;
        }

        // Méthodes existantes...
        public async Task<Dictionary<string, int>> GetExistingArtistsAsync(IEnumerable<string> artistNames, CancellationToken cancellationToken = default)
        {
            return await _context.Artists
                .Where(a => artistNames.Contains(a.ArtistName))
                .ToDictionaryAsync(a => a.ArtistName, a => a.ArtistId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        }

        public async Task AddArtistsAsync(IEnumerable<Artist> artists, CancellationToken cancellationToken = default)
        {
            await _context.Artists.AddRangeAsync(artists, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Nouvelles méthodes
        public async Task<Artist?> GetByNameAsync(string artistName)
        {
            return await _context.Artists
                .FirstOrDefaultAsync(a => EF.Functions.ILike(a.ArtistName, artistName)); // Case-insensitive avec ILike pour PostgreSQL
        }

        public async Task<List<TrackStat>> GetTrackStatsForArtistAsync(int artistId, int userId, DateTime? since)
        {
            var query = _context.Streams
                .Where(s => s.UserId == userId)
                .Where(s => s.Track.TrackArtists.Any(ta => ta.ArtistId == artistId))
                .Where(s => !since.HasValue || s.PlayedAt >= since.Value)
                .GroupBy(s => s.TrackId)
                .Select(g => new TrackStat
                {
                    TrackId = g.Key,
                    TrackName = g.First().Track.TrackName,
                    AlbumName = g.First().Track.Album != null
                        ? g.First().Track.Album!.AlbumName
                        : "Single",

                    Count = g.Count(),
                    TotalListeningTime = g.Sum(s => (int?)s.ListeningTime) ?? 0
                })
                .OrderByDescending(ts => ts.Count);

            return await query.ToListAsync();
        }

        public async Task UpdateCoverAsync(int artistId, string coverUrl)
        {
            var artist = await _context.Artists.FindAsync(artistId);
            if (artist != null)
            {
                artist.CoverUrl = coverUrl;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ArtistSearchProjection>> SearchArtistsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<ArtistSearchProjection>();

            return await _context.Artists
                .AsNoTracking()
                .Where(a => EF.Functions.ILike(a.ArtistName, $"%{query}%"))
                .OrderBy(a => a.ArtistName)
                .Select(a => new ArtistSearchProjection
                {
                    Id = a.ArtistId,
                    Name = a.ArtistName,
                    CoverUrl = a.CoverUrl
                })
                .ToListAsync();
        }
    }
}