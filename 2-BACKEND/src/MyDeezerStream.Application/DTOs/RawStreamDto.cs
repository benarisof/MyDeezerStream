using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.DTOs
{
    public class RawStreamDto
    {
        public string SongTitle { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string AlbumTitle { get; set; } = string.Empty;
        public int? ListeningTime { get; set; } 
        public DateTime Date { get; set; }
    }
}
