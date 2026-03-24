using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Entities.Stats
{
    public record TopArtistResult(int ArtistId, string ArtistName, int Count, string? CoverUrl = null);

    public record TopTrackResult(int TrackId, string TrackName, string ArtistNames, int Count, string? CoverUrl = null);

    public record TopAlbumResult(int AlbumId, string AlbumName, string ArtistName, int Count, string? CoverUrl = null);
}
