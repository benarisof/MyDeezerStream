using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.Interfaces;

public interface IDeezerApiService
{
    Task<string?> GetArtistCoverAsync(string artistName);
    Task<string?> GetAlbumCoverAsync(string albumName, string artistName);
    Task<string?> GetTrackCoverAsync(string trackName, string artistName);
}
