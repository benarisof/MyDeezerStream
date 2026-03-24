

namespace MyDeezerStream.Application.DTOs
{
    public class TopTrackDto
    {
        public string Track { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? CoverUrl { get; set; }
    }

    public class TrackDto
    {
        public string Name { get; set; } = string.Empty;
        public string Album {  get; set; } = string.Empty;  
        public int ListeningTime { get; set; }  
        public int Count { get; set; }
    }
}
