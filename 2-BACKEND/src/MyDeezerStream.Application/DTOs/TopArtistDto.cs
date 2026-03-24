

namespace MyDeezerStream.Application.DTOs;

public class TopArtistDto
{
    public string Artist { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? CoverUrl { get; set; }
}

public class ArtistDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public int ListeningTime { get; set; }
    public string CoverUrl { get; set; } = string.Empty;
    public IEnumerable<TrackDto>? trackDtos { get; set; }
}