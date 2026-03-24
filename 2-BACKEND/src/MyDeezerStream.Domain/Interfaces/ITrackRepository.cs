using MyDeezerStream.Domain.Entities.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Interfaces
{
    public interface ITrackRepository
    {
        Task<List<Track>> GetExistingTracksAsync(IEnumerable<string> trackNames, CancellationToken cancellationToken = default);
        Task AddTracksAsync(IEnumerable<Track> tracks, CancellationToken cancellationToken = default);
    }
}
