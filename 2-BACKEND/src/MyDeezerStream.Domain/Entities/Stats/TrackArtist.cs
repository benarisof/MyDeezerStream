using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Entities.Stats
{
    public class TrackArtist
    {
        public int TrackId { get; set; }
        public int ArtistId { get; set; }
        public Track Track { get; set; } = null!;
        public Artist Artist { get; set; } = null!;
    }
}
