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
        public string Name { get; set; } = string.Empty;
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? CoverUrl { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
