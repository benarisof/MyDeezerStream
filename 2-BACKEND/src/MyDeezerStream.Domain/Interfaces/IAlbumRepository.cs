using MyDeezerStream.Domain.Entities.Search;
using MyDeezerStream.Domain.Entities.Stats;

namespace MyDeezerStream.Domain.Interfaces
{
    public interface IAlbumRepository
    {
        Task<Dictionary<string, int>> GetExistingAlbumsAsync(IEnumerable<string> albumNames, CancellationToken cancellationToken = default);
        Task AddAlbumsAsync(IEnumerable<Album> albums, CancellationToken cancellationToken = default);
        Task<Album?> GetByNameAndArtistAsync(string albumName, string artistName);
        Task<List<TrackStat>> GetTrackStatsForAlbumAsync(int albumId, int userId, DateTime? since);
        Task UpdateCoverAsync(int albumId, string coverUrl);
        Task<List<AlbumSearchProjection>> SearchAlbumsAsync(string query);
    }

}
