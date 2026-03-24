

using MyDeezerStream.Domain.Entities.Stats;
using Stream = MyDeezerStream.Domain.Entities.Stats.Stream;

namespace MyDeezerStream.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Auth0Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public ICollection<Stream> Streams { get; set; } = new List<Stream>();
    }
}
