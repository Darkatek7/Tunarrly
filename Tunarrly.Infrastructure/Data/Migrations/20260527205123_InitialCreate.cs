using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tunarrly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiRecommendationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderBaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    InputSummaryJson = table.Column<string>(type: "TEXT", nullable: false),
                    OutputJson = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRecommendationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtistRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    SourceNormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    TargetArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    TargetNormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    RelationType = table.Column<string>(type: "TEXT", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    EvidenceJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistRelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryArtists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    MusicBrainzId = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryArtists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LidarrArtists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LidarrId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    MusicBrainzId = table.Column<string>(type: "TEXT", nullable: true),
                    ForeignArtistId = table.Column<string>(type: "TEXT", nullable: true),
                    Monitored = table.Column<bool>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    QualityProfileId = table.Column<int>(type: "INTEGER", nullable: true),
                    MetadataProfileId = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LidarrArtists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<int>(type: "INTEGER", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonJson = table.Column<string>(type: "TEXT", nullable: false),
                    RelatedArtistsJson = table.Column<string>(type: "TEXT", nullable: false),
                    GenresJson = table.Column<string>(type: "TEXT", nullable: false),
                    LastCalculatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    LibraryPath = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentFile = table.Column<string>(type: "TEXT", nullable: true),
                    FilesDiscovered = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesScanned = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesFailed = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryAlbums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedTitle = table.Column<string>(type: "TEXT", nullable: false),
                    ArtistId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryAlbums_LibraryArtists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "LibraryArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibraryTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedTitle = table.Column<string>(type: "TEXT", nullable: false),
                    AlbumId = table.Column<int>(type: "INTEGER", nullable: true),
                    MainArtistId = table.Column<int>(type: "INTEGER", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Genre = table.Column<string>(type: "TEXT", nullable: true),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    FileModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryTracks_LibraryAlbums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "LibraryAlbums",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LibraryTracks_LibraryArtists_MainArtistId",
                        column: x => x.MainArtistId,
                        principalTable: "LibraryArtists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrackArtistCredits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    CreditType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackArtistCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackArtistCredits_LibraryTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "LibraryTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArtistRelations_SourceNormalizedName_TargetNormalizedName_RelationType",
                table: "ArtistRelations",
                columns: new[] { "SourceNormalizedName", "TargetNormalizedName", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryAlbums_ArtistId_NormalizedTitle",
                table: "LibraryAlbums",
                columns: new[] { "ArtistId", "NormalizedTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryArtists_NormalizedName",
                table: "LibraryArtists",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryTracks_AlbumId",
                table: "LibraryTracks",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryTracks_MainArtistId",
                table: "LibraryTracks",
                column: "MainArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryTracks_Path",
                table: "LibraryTracks",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LidarrArtists_LidarrId",
                table: "LidarrArtists",
                column: "LidarrId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LidarrArtists_NormalizedName",
                table: "LidarrArtists",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_NormalizedArtistName",
                table: "Recommendations",
                column: "NormalizedArtistName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackArtistCredits_NormalizedArtistName",
                table: "TrackArtistCredits",
                column: "NormalizedArtistName");

            migrationBuilder.CreateIndex(
                name: "IX_TrackArtistCredits_TrackId",
                table: "TrackArtistCredits",
                column: "TrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiRecommendationRuns");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "ArtistRelations");

            migrationBuilder.DropTable(
                name: "LidarrArtists");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "ScanJobs");

            migrationBuilder.DropTable(
                name: "TrackArtistCredits");

            migrationBuilder.DropTable(
                name: "LibraryTracks");

            migrationBuilder.DropTable(
                name: "LibraryAlbums");

            migrationBuilder.DropTable(
                name: "LibraryArtists");
        }
    }
}
