using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tunarrly.Core.Options;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Tests;

internal static class InfrastructureTestHelpers
{
    public static IDbContextFactory<TunarrlyDbContext> CreateDbFactory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tunarrly-tests-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TunarrlyDbContext>().UseSqlite($"Data Source={path}").Options;
        var factory = new TestDbFactory(options);
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();
        return factory;
    }

    public static IOptions<T> Options<T>(T value) where T : class => Microsoft.Extensions.Options.Options.Create(value);

    private sealed class TestDbFactory(DbContextOptions<TunarrlyDbContext> options) : IDbContextFactory<TunarrlyDbContext>
    {
        public TunarrlyDbContext CreateDbContext() => new(options);
    }
}
