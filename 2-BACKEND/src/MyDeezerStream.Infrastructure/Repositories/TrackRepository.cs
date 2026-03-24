using Microsoft.EntityFrameworkCore;
using MyDeezerStream.Domain.Entities.Stats;
using MyDeezerStream.Domain.Interfaces;
using MyDeezerStream.Infrastructure.Data;

namespace MyDeezerStream.Infrastructure.Repositories;

public class TrackRepository : ITrackRepository
{
    private readonly AppDbContext _context;

    public TrackRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Track>> GetExistingTracksAsync(IEnumerable<string> trackNames, CancellationToken cancellationToken = default)
    {
        return await _context.Tracks
            .Include(t => t.TrackArtists)
            .Where(t => trackNames.Contains(t.TrackName))
            .ToListAsync(cancellationToken);
    }

    public async Task AddTracksAsync(IEnumerable<Track> tracks, CancellationToken cancellationToken = default)
    {
        await _context.Tracks.AddRangeAsync(tracks, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}