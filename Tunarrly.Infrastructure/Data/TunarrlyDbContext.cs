using Microsoft.EntityFrameworkCore;

namespace Tunarrly.Infrastructure.Data;

public sealed class TunarrlyDbContext(DbContextOptions<TunarrlyDbContext> options) : DbContext(options)
{
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<LibraryArtist> LibraryArtists => Set<LibraryArtist>();
    public DbSet<LibraryAlbum> LibraryAlbums => Set<LibraryAlbum>();
    public DbSet<LibraryTrack> LibraryTracks => Set<LibraryTrack>();
    public DbSet<TrackArtistCredit> TrackArtistCredits => Set<TrackArtistCredit>();
    public DbSet<LidarrArtist> LidarrArtists => Set<LidarrArtist>();
    public DbSet<ArtistRelation> ArtistRelations => Set<ArtistRelation>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<AiRecommendationRun> AiRecommendationRuns => Set<AiRecommendationRun>();
    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<LibraryArtist>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<LibraryAlbum>().HasIndex(x => new { x.ArtistId, x.NormalizedTitle });
        modelBuilder.Entity<LibraryTrack>().HasIndex(x => x.Path).IsUnique();
        modelBuilder.Entity<TrackArtistCredit>().HasIndex(x => x.NormalizedArtistName);
        modelBuilder.Entity<LidarrArtist>().HasIndex(x => x.LidarrId).IsUnique();
        modelBuilder.Entity<LidarrArtist>().HasIndex(x => x.NormalizedName);
        modelBuilder.Entity<Recommendation>().HasIndex(x => x.NormalizedArtistName).IsUnique();
        modelBuilder.Entity<ArtistRelation>().HasIndex(x => new { x.SourceNormalizedName, x.TargetNormalizedName, x.RelationType });
    }
}
