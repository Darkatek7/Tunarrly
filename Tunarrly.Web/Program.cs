using Tunarrly.Web.Components;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using Tunarrly.Infrastructure;
using Tunarrly.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddTunarrlyInfrastructure(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TunarrlyDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/ready", async (IDbContextFactory<TunarrlyDbContext> dbFactory, CancellationToken cancellationToken) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
    return await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ok", checks = new { database = "ok" } })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
