using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Entities.Stats
{
    public class Album
    {
        public int AlbumId { get; set; }
        public string AlbumName { get; set; } = string.Empty;
        public string? CoverUrl { get; set; }
        public ICollection<Track> Tracks { get; set; } = new List<Track>();
    }
}
