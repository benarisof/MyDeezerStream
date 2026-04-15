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

    public async Task<IEnumerable<Stream>> GetStreamsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Streams
            .AsNoTracking()
            .Include(s => s.Track)
                .ThenInclude(t => t.Album)
            .Include(s => s.Track)
                .ThenInclude(t => t.TrackArtists)
                    .ThenInclude(ta => ta.Artist)
            .Where(s => s.UserId == userId);

        if (from.HasValue)
            query = query.Where(s => s.PlayedAt >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(s => s.PlayedAt <= to.Value.ToUniversalTime());

        if (!await query.AnyAsync())
            return Enumerable.Empty<Stream>();

        return await query
            .OrderByDescending(s => s.PlayedAt)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<IEnumerable<TopArtistResult>> GetTopArtistsAsync(int userId, int limit = 10, DateTime? since = null)
    {
        var baseQuery = _context.Streams.Where(s => s.UserId == userId);

        if (since.HasValue)
        {
            var utcSince = since.Value.ToUniversalTime();
            baseQuery = baseQuery.Where(s => s.PlayedAt >= utcSince);
        }

        if (!await baseQuery.AnyAsync())
            return Enumerable.Empty<TopArtistResult>();

        var query = from s in baseQuery
                    join ta in _context.TrackArtists on s.TrackId equals ta.TrackId
                    join a in _context.Artists on ta.ArtistId equals a.ArtistId
                    // Ajout de l'Id et de la CoverUrl dans le groupement
                    group a by new { a.ArtistId, a.ArtistName, a.CoverUrl } into g
                    select new { g.Key.ArtistId, g.Key.ArtistName, g.Key.CoverUrl, Count = g.Count() };

        var topArtists = await query
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        return topArtists.Select(x => new TopArtistResult(x.ArtistId, x.ArtistName, x.Count, x.CoverUrl));
    }

    public async Task<IEnumerable<TopTrackResult>> GetTopTracksAsync(int userId, int limit = 10, DateTime? since = null)
    {
        var baseQuery = _context.Streams.Where(s => s.UserId == userId);

        if (since.HasValue)
            baseQuery = baseQuery.Where(s => s.PlayedAt >= since.Value.ToUniversalTime());

        if (!await baseQuery.AnyAsync())
            return Enumerable.Empty<TopTrackResult>();

        var topTracksStats = await baseQuery
            .GroupBy(s => s.TrackId)
            .Select(g => new { TrackId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        if (!topTracksStats.Any())
            return Enumerable.Empty<TopTrackResult>();

        var trackIds = topTracksStats.Select(t => t.TrackId).ToList();

        var trackDetails = await _context.Tracks
            .AsNoTracking()
            .Include(t => t.TrackArtists)
                .ThenInclude(ta => ta.Artist)
            .Where(t => trackIds.Contains(t.TrackId))
            .ToDictionaryAsync(t => t.TrackId);

        return topTracksStats.Select(ts =>
        {
            var track = trackDetails[ts.TrackId];
            var artistNames = string.Join(", ", track.TrackArtists.Select(ta => ta.Artist.ArtistName));
            // Ajout de l'Id et de la CoverUrl
            return new TopTrackResult(track.TrackId, track.TrackName, artistNames, ts.Count, track.CoverUrl);
        });
    }

    
    public async Task<IEnumerable<TopAlbumResult>> GetTopAlbumsAsync(int userId, int limit = 10, DateTime? since = null)
    {
        var baseQuery = _context.Streams.Where(s => s.UserId == userId);

        if (since.HasValue)
            baseQuery = baseQuery.Where(s => s.PlayedAt >= since.Value.ToUniversalTime());

        if (!await baseQuery.AnyAsync())
            return Enumerable.Empty<TopAlbumResult>();

        var topAlbumsStats = await (from s in baseQuery
                                    join t in _context.Tracks on s.TrackId equals t.TrackId
                                    where t.AlbumId != null
                                    // Ajout de la CoverUrl au groupement
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

        if (!topAlbumsStats.Any())
            return Enumerable.Empty<TopAlbumResult>();

        var albumIds = topAlbumsStats.Select(a => a.AlbumId).ToList();

        // 1. On récupère les paires (AlbumId, ArtistName) pour tous les albums concernés
        var albumArtistsData = await _context.TrackArtists
            .AsNoTracking()
            .Where(ta => ta.Track.AlbumId != null && albumIds.Contains(ta.Track.AlbumId.Value))
            .Select(ta => new
            {
                AlbumId = ta.Track.AlbumId!.Value,
                ArtistName = ta.Artist.ArtistName
            })
            .ToListAsync();

        // 2. On groupe en mémoire pour trouver l'artiste qui apparaît le plus souvent par album
        var artistsLookup = albumArtistsData
            .GroupBy(x => x.AlbumId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.ArtistName) // On regroupe par nom d'artiste dans cet album
                      .OrderByDescending(artistGroup => artistGroup.Count()) // On trie par nombre d'apparitions
                      .Select(artistGroup => artistGroup.Key)
                      .FirstOrDefault() ?? "Artiste Inconnu"
            );

        return topAlbumsStats.Select(a => new TopAlbumResult(
            a.AlbumId,
            a.AlbumName,
            artistsLookup.GetValueOrDefault(a.AlbumId, "Artiste Inconnu"),
            a.Count,
            a.CoverUrl));
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

    // --- NOUVELLES MÉTHODES DE MISE À JOUR RAPIDE ---

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