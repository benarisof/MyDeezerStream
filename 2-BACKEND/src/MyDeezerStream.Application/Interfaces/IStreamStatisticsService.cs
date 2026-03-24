using MyDeezerStream.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.Interfaces
{
    public interface IStreamStatisticsService
    {
        Task<List<TopArtistDto>> GetTopArtistsAsync(int limit, int days);
        Task<List<TopTrackDto>> GetTopTracksAsync(int limit, int days);
        Task<List<TopAlbumDto>> GetTopAlbumsAsync(int limit, int days);
        Task<List<RawStreamDto>> GetLastStreamAsync(int limit);
    }
}
