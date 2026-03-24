using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.DTOs
{
    public class TopAlbumDto
    {
        public string Album { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? CoverUrl { get; set; }
    }

    public class AlbumDto
    {
        public string Name { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        //Nombre d'écoutes
        public int Count { get; set; }
        //Temps d'écoute en secondes
        public int ListeningTime { get; set; }
        public string CoverUrl { get; set; } = string.Empty;
        public string? ReleaseDate { get; set; }
        public IEnumerable<TrackDto>? trackDtos { get; set; }
    }
}
