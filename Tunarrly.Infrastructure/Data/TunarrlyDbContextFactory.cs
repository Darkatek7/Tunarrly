using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tunarrly.Infrastructure.Data;

public sealed class TunarrlyDbContextFactory : IDesignTimeDbContextFactory<TunarrlyDbContext>
{
    public TunarrlyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TunarrlyDbContext>()
            .UseSqlite("Data Source=data/tunarrly.db")
            .Options;
        return new TunarrlyDbContext(options);
    }
}
