using Microsoft.EntityFrameworkCore;
using MyDeezerStream.Domain.Entities.Search;
using MyDeezerStream.Domain.Entities.Stats;
using MyDeezerStream.Domain.Interfaces;
using MyDeezerStream.Infrastructure.Data;

namespace MyDeezerStream.Infrastructure.Repositories;

public class AlbumRepository : IAlbumRepository
{
    private readonly AppDbContext _context;

    public AlbumRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, int>> GetExistingAlbumsAsync(IEnumerable<string> albumNames, CancellationToken cancellationToken = default)
    {
        return await _context.Albums
            .Where(a => albumNames.Contains(a.AlbumName))
            .ToDictionaryAsync(a => a.AlbumName, a => a.AlbumId, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    public async Task AddAlbumsAsync(IEnumerable<Album> albums, CancellationToken cancellationToken = default)
    {
        await _context.Albums.AddRangeAsync(albums, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Nouvelles méthodes
    public async Task<Album?> GetByNameAndArtistAsync(string albumName, string artistName)
    {
        return await _context.Albums
            .FirstOrDefaultAsync(a => EF.Functions.ILike(a.AlbumName, albumName)
                && a.Tracks.Any(t => t.TrackArtists.Any(ta => EF.Functions.ILike(ta.Artist.ArtistName, artistName))));
    }

    public async Task<List<TrackStat>> GetTrackStatsForAlbumAsync(int albumId, int userId, DateTime? since)
    {
        var query = _context.Streams
            .Where(s => s.UserId == userId)
            .Where(s => s.Track.AlbumId == albumId)
            .Where(s => !since.HasValue || s.PlayedAt >= since.Value)
            .GroupBy(s => s.TrackId)
            .Select(g => new TrackStat
            {
                TrackId = g.Key,
                TrackName = g.First().Track.TrackName,
                AlbumName = g.First().Track.Album!.AlbumName,

                Count = g.Count(),
                TotalListeningTime = g.Sum(s => (int?)s.ListeningTime) ?? 0
            })
            .OrderByDescending(ts => ts.Count);

        return await query.ToListAsync();
    }

    public async Task UpdateCoverAsync(int albumId, string coverUrl)
    {
        var album = await _context.Albums.FindAsync(albumId);
        if (album != null)
        {
            album.CoverUrl = coverUrl;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<AlbumSearchProjection>> SearchAlbumsAsync(string query)
    {
        return await _context.Albums
            .AsNoTracking()
            .Where(a => EF.Functions.ILike(a.AlbumName, $"%{query}%"))
            .Select(a => new AlbumSearchProjection
            {
                Id = a.AlbumId,
                Name = a.AlbumName,
                CoverUrl = a.CoverUrl,
                Artist = a.Tracks
                          .SelectMany(t => t.TrackArtists)
                          .Select(ta => ta.Artist.ArtistName)
                          .FirstOrDefault() ?? "Inconnu" // Premier artiste lié à une piste
            })
            .Take(5)
            .ToListAsync();
    }
}