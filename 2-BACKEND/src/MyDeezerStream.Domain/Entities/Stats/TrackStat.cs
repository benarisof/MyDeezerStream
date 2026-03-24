namespace MyDeezerStream.Domain.Entities.Stats
{
    public class TrackStat
    {
        public int TrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public string AlbumName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalListeningTime { get; set; }
    }
}
