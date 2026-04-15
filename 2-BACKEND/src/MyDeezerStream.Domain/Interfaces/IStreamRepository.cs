using MyDeezerStream.Domain.Entities.Stats;
using Stream = MyDeezerStream.Domain.Entities.Stats.Stream;

namespace MyDeezerStream.Domain.Interfaces;

public interface IStreamRepository
{
    Task<IEnumerable<Stream>> GetStreamsAsync(int userId, DateTime? from = null, DateTime? to = null);

    // Ajout du paramètre 'to' sur les méthodes de Top
    Task<IEnumerable<TopArtistResult>> GetTopArtistsAsync(int userId, int limit = 10, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<TopTrackResult>> GetTopTracksAsync(int userId, int limit = 10, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<TopAlbumResult>> GetTopAlbumsAsync(int userId, int limit = 10, DateTime? from = null, DateTime? to = null);

    Task BulkInsertStreamsAsync(IEnumerable<Stream> streams, CancellationToken cancellationToken = default);

    Task UpdateArtistCoverAsync(int artistId, string coverUrl);
    Task UpdateTrackCoverAsync(int trackId, string coverUrl);
    Task UpdateAlbumCoverAsync(int albumId, string coverUrl);
}