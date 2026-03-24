using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Domain.Entities.Search
{
    public class ArtistSearchProjection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CoverUrl { get; set; }
    }
}
