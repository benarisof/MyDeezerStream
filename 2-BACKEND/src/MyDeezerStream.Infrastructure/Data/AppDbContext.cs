using Microsoft.EntityFrameworkCore;
using MyDeezerStream.Domain.Entities;
using MyDeezerStream.Domain.Entities.Stats;
using Stream = MyDeezerStream.Domain.Entities.Stats.Stream;

namespace MyDeezerStream.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Album> Albums { get; set; }
    public DbSet<Track> Tracks { get; set; }
    public DbSet<TrackArtist> TrackArtists { get; set; }
    public DbSet<Stream> Streams { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- USER (Table: user) ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("userid").UseIdentityByDefaultColumn();
            entity.Property(e => e.Auth0Id).HasColumnName("username").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Ignore(e => e.DisplayName);
            entity.HasIndex(e => e.Auth0Id).IsUnique();
        });

        // --- ARTIST (Table: artist) ---
        modelBuilder.Entity<Artist>(entity =>
        {
            entity.ToTable("artist");
            entity.HasKey(e => e.ArtistId);
            entity.Property(e => e.ArtistId).HasColumnName("artistid").UseIdentityByDefaultColumn();
            entity.Property(e => e.ArtistName).HasColumnName("artistname").IsRequired();
            // AJOUT : Mapping explicite vers la minuscule cover_url
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
        });

        // --- ALBUM (Table: album) ---
        modelBuilder.Entity<Album>(entity =>
        {
            entity.ToTable("album");
            entity.HasKey(e => e.AlbumId);
            entity.Property(e => e.AlbumId).HasColumnName("albumid").UseIdentityByDefaultColumn();
            entity.Property(e => e.AlbumName).HasColumnName("albumname").IsRequired();
            // AJOUT : Mapping explicite
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
        });

        // --- TRACK (Table: track) ---
        modelBuilder.Entity<Track>(entity =>
        {
            entity.ToTable("track");
            entity.HasKey(e => e.TrackId);
            entity.Property(e => e.TrackId).HasColumnName("trackid").UseIdentityByDefaultColumn();
            entity.Property(e => e.TrackName).HasColumnName("trackname").IsRequired();
            entity.Property(e => e.AlbumId).HasColumnName("albumid");
            // AJOUT : Mapping explicite
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");

            entity.HasOne(e => e.Album)
                  .WithMany(a => a.Tracks)
                  .HasForeignKey(e => e.AlbumId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // --- TRACKARTIST (Table: track_artist) ---
        modelBuilder.Entity<TrackArtist>(entity =>
        {
            entity.ToTable("track_artist");
            entity.HasKey(e => new { e.TrackId, e.ArtistId });
            entity.Property(e => e.TrackId).HasColumnName("trackid");
            entity.Property(e => e.ArtistId).HasColumnName("artistid");

            entity.HasOne(e => e.Track).WithMany(t => t.TrackArtists).HasForeignKey(e => e.TrackId);
            entity.HasOne(e => e.Artist).WithMany(a => a.TrackArtists).HasForeignKey(e => e.ArtistId);
        });

        // --- STREAM (Table: stream) ---
        modelBuilder.Entity<Stream>(entity =>
        {
            entity.ToTable("stream");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            entity.Property(e => e.UserId).HasColumnName("userid").IsRequired();
            entity.Property(e => e.TrackId).HasColumnName("trackid").IsRequired();
            entity.Property(e => e.PlayedAt).HasColumnName("played_at").IsRequired();
            entity.Property(e => e.ListeningTime).HasColumnName("listening_time");

            entity.HasOne(e => e.User).WithMany(u => u.Streams).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Track).WithMany(t => t.Streams).HasForeignKey(e => e.TrackId);
        });
    }
}