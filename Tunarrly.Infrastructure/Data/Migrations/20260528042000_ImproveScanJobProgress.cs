using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tunarrly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TunarrlyDbContext))]
    [Migration("20260528042000_ImproveScanJobProgress")]
    public partial class ImproveScanJobProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureSummaryJson",
                table: "ScanJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "FilesSkipped",
                table: "ScanJobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureSummaryJson",
                table: "ScanJobs");

            migrationBuilder.DropColumn(
                name: "FilesSkipped",
                table: "ScanJobs");
        }
    }
}
