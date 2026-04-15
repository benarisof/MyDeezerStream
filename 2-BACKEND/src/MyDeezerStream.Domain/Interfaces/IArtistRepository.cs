using MyDeezerStream.Domain.Entities.Search;
using MyDeezerStream.Domain.Entities.Stats;

public interface IArtistRepository
{
    Task<Dictionary<string, int>> GetExistingArtistsAsync(IEnumerable<string> artistNames, CancellationToken cancellationToken = default);
    Task AddArtistsAsync(IEnumerable<Artist> artists, CancellationToken cancellationToken = default);
    Task<Artist?> GetByNameAsync(string artistName);
    Task<List<TrackStat>> GetTrackStatsForArtistAsync(int artistId, int userId, DateTime? from, DateTime? to);
    Task UpdateCoverAsync(int artistId, string coverUrl);
    Task<IEnumerable<ArtistSearchProjection>> SearchArtistsAsync(string query);
}