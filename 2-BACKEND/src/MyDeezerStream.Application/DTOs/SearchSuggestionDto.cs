using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.DTOs
{
    public class SearchSuggestionDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty; // Nom de l'album ou de l'artiste
        public string? Subtitle { get; set; }    // Nom de l'artiste (si c'est un album)
        public string? CoverUrl { get; set; }
        public string Type { get; set; } = string.Empty; // "artist" ou "album"
    }
}
