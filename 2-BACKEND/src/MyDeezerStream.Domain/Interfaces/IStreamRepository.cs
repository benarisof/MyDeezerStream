using MyDeezerStream.Domain.Entities.Stats;
using Stream = MyDeezerStream.Domain.Entities.Stats.Stream;

namespace MyDeezerStream.Domain.Interfaces;

public interface IStreamRepository
{
    Task<IEnumerable<Stream>> GetStreamsAsync(int userId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<TopArtistResult>> GetTopArtistsAsync(int userId, int limit = 10, DateTime? since = null);
    Task<IEnumerable<TopTrackResult>> GetTopTracksAsync(int userId, int limit = 10, DateTime? since = null);
    Task<IEnumerable<TopAlbumResult>> GetTopAlbumsAsync(int userId, int limit = 10, DateTime? since = null);
    Task BulkInsertStreamsAsync(IEnumerable<Stream> streams, CancellationToken cancellationToken = default);

    // Nouvelles méthodes à ajouter :
    Task UpdateArtistCoverAsync(int artistId, string coverUrl);
    Task UpdateTrackCoverAsync(int trackId, string coverUrl);
    Task UpdateAlbumCoverAsync(int albumId, string coverUrl);

}