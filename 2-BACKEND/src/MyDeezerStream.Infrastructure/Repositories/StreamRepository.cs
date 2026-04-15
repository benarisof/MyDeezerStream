using Microsoft.EntityFrameworkCore;
using MyDeezerStream.Domain.Entities.Stats;
using MyDeezerStream.Domain.Interfaces;
using MyDeezerStream.Infrastructure.Data;
using Stream = MyDeezerStream.Domain.Entities.Stats.Stream;

namespace MyDeezerStream.Infrastructure.Repositories;

public class StreamRepository : IStreamRepository
{
    private readonly AppDbContext _context;

    public StreamRepository(AppDbContext context)
    {
        _context = context;
    }

    // Helper privé pour filtrer par date sur n'importe quelle requête de Streams
    private IQueryable<Stream> FilterByDate(IQueryable<Stream> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue)
            query = query.Where(s => s.PlayedAt >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(s => s.PlayedAt <= to.Value.ToUniversalTime());

        return query;
    }

    public async Task<IEnumerable<Stream>> GetStreamsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Streams
            .AsNoTracking()
            .Include(s => s.Track).ThenInclude(t => t.Album)
            .Include(s => s.Track).ThenInclude(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .Where(s => s.UserId == userId);

        query = FilterByDate(query, from, to);

        if (!await query.AnyAsync())
            return Enumerable.Empty<Stream>();

        return await query
            .OrderByDescending(s => s.PlayedAt)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<IEnumerable<TopArtistResult>> GetTopArtistsAsync(int userId, int limit = 10, DateTime? from = null, DateTime? to = null)
    {
        var baseQuery = _context.Streams.Where(s => s.UserId == userId);
        baseQuery = FilterByDate(baseQuery, from, to);

        if (!await baseQuery.AnyAsync())
            return Enumerable.Empty<TopArtistResult>();

        var query = from s in baseQuery
                    join ta in _context.TrackArtists on s.TrackId equals ta.TrackId
                    join a in _context.Artists on ta.ArtistId equals a.ArtistId
                    group a by new { a.ArtistId, a.ArtistName, a.CoverUrl } into g
                    select new { g.Key.ArtistId, g.Key.ArtistName, g.Key.CoverUrl, Count = g.Count() };

        var topArtists = await query
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        return topArtists.Select(x => new TopArtistResult(x.ArtistId, x.ArtistName, x.Count, x.CoverUrl));
    }

    public async Task<IEnumerable<TopTrackResult>> GetTopTracksAsync(int userId, int limit = 10, DateTime? from = null, DateTime? to = null)
    {
        var baseQuery = _context.Streams.Where(s => s.UserId == userId);
        baseQuery = FilterByDate(baseQuery, from, to);

        if (!await baseQuery.AnyAsync())
            return Enumerable.Empty<TopTrackResult>();

        var topTracksStats = await baseQuery
            .GroupBy(s => s.TrackId)
            .Select(g => new { TrackId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        var trackIds = topTracksStats.Select(t => t.TrackId).ToList();

        var trackDetails = await _context.Tracks
            .AsNoTracking()
            .Include(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
            .Where(t => trackIds.Contains(t.TrackId))
            .ToDictionaryAsync(t => t.TrackId);

        return topTracksStats.Select(ts =>
        {
            var track = trackDetails[ts.TrackId];
            var artistNames = string.Join(", ", track.TrackArtists.Select(ta => ta.Artist.ArtistName));
            return new TopTrackResult(track.TrackId, track.TrackName, artistNames, ts.Count, track.CoverUrl);
        });
    }

    public async Task<IEnumerable<TopAlbumResult>> GetTopAlbumsAsync(int userId, int limit = 10, DateTime? from = null, DateTime? to = null)
    {
        var baseQuery = _context.Streams.Where(s => s.UserId == userId);
        baseQuery = FilterByDate(baseQuery, from, to);

        if (!await baseQuery.AnyAsync())
            return Enumerable.Empty<TopAlbumResult>();

        var topAlbumsStats = await (from s in baseQuery
                                    join t in _context.Tracks on s.TrackId equals t.TrackId
                                    where t.AlbumId != null
                                    group t by new { t.AlbumId, t.Album!.AlbumName, t.Album.CoverUrl } into g
                                    select new
                                    {
                                        AlbumId = g.Key.AlbumId!.Value,
                                        g.Key.AlbumName,
                                        g.Key.CoverUrl,
                                        Count = g.Count()
                                    })
                                   .OrderByDescending(x => x.Count)
                                   .Take(limit)
                                   .ToListAsync();

        var albumIds = topAlbumsStats.Select(a => a.AlbumId).ToList();

        var albumArtistsData = await _context.TrackArtists
            .AsNoTracking()
            .Where(ta => ta.Track.AlbumId != null && albumIds.Contains(ta.Track.AlbumId.Value))
            .Select(ta => new { AlbumId = ta.Track.AlbumId!.Value, ArtistName = ta.Artist.ArtistName })
            .ToListAsync();

        var artistsLookup = albumArtistsData
            .GroupBy(x => x.AlbumId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.ArtistName)
                      .OrderByDescending(ag => ag.Count())
                      .Select(ag => ag.Key)
                      .FirstOrDefault() ?? "Artiste Inconnu"
            );

        return topAlbumsStats.Select(a => new TopAlbumResult(
            a.AlbumId, a.AlbumName, artistsLookup.GetValueOrDefault(a.AlbumId, "Artiste Inconnu"), a.Count, a.CoverUrl));
    }

    public async Task BulkInsertStreamsAsync(IEnumerable<Stream> streams, CancellationToken cancellationToken = default)
    {
        var batches = streams.Chunk(500);
        foreach (var batch in batches)
        {
            await _context.Streams.AddRangeAsync(batch, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }
    }

    public async Task UpdateArtistCoverAsync(int artistId, string coverUrl)
    {
        await _context.Artists
            .Where(a => a.ArtistId == artistId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.CoverUrl, coverUrl));
    }

    public async Task UpdateTrackCoverAsync(int trackId, string coverUrl)
    {
        await _context.Tracks
            .Where(t => t.TrackId == trackId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CoverUrl, coverUrl));
    }

    public async Task UpdateAlbumCoverAsync(int albumId, string coverUrl)
    {
        await _context.Albums
            .Where(a => a.AlbumId == albumId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.CoverUrl, coverUrl));
    }
}