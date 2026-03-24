using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Entities.Stats
{
    public class Track
    {
        public int TrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public int? AlbumId { get; set; }
        public string? CoverUrl { get; set; }
        public Album? Album { get; set; }
        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
        public ICollection<Stream> Streams { get; set; } = new List<Stream>();
    }
}
