namespace MyDeezerStream.Domain.Entities.Stats
{
    public class Stream
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TrackId { get; set; }
        public DateTime PlayedAt { get; set; }
        public int? ListeningTime { get; set; } 

        public User User { get; set; } = null!;
        public Track Track { get; set; } = null!;
    }
}
