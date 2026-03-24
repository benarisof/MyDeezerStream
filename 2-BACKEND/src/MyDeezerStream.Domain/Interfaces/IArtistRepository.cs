using MyDeezerStream.Domain.Entities.Search;
using MyDeezerStream.Domain.Entities.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Interfaces
{
    public interface IArtistRepository
    {
        Task<Dictionary<string, int>> GetExistingArtistsAsync(IEnumerable<string> artistNames, CancellationToken cancellationToken = default);
        Task AddArtistsAsync(IEnumerable<Artist> artists, CancellationToken cancellationToken = default);

        Task<Artist?> GetByNameAsync(string artistName);
        Task<List<TrackStat>> GetTrackStatsForArtistAsync(int artistId, int userId, DateTime? since);
        Task UpdateCoverAsync(int artistId, string coverUrl);
        Task<IEnumerable<ArtistSearchProjection>> SearchArtistsAsync(string query);
    }
}
