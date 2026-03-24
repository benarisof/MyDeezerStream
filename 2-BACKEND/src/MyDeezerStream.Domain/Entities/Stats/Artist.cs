using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Entities.Stats
{
    public class Artist
    {
        public int ArtistId { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public string? CoverUrl { get; set; }

        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
    }
}
