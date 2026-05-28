using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using Tunarrly.Core.Options;
using Tunarrly.Infrastructure;
using Tunarrly.Infrastructure.Data;
using Tunarrly.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddTunarrlyInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.Cookie.Name = "Tunarrly.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

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

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWhen(context => RequiresAuthRedirect(context), branch =>
{
    branch.Use(async (context, next) =>
    {
        var auth = context.RequestServices.GetRequiredService<IOptions<AuthOptions>>().Value;
        if (auth.Enabled && context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(context.Request.PathBase + context.Request.Path + context.Request.QueryString)}");
            return;
        }

        await next();
    });
});
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/ready", async (IDbContextFactory<TunarrlyDbContext> dbFactory, CancellationToken cancellationToken) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
    return await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ok", checks = new { database = "ok" } })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/login-submit", async (HttpContext context, IOptions<AuthOptions> authOptions) =>
{
    var auth = authOptions.Value;
    if (!auth.Enabled)
    {
        return Results.Redirect("/");
    }

    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = SafeReturnUrl(form["returnUrl"].ToString());
    if (!string.Equals(username, auth.Username, StringComparison.Ordinal) || !string.Equals(password, auth.Password, StringComparison.Ordinal))
    {
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect(returnUrl);
}).DisableAntiforgery();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool RequiresAuthRedirect(HttpContext context)
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/login") ||
        path.StartsWithSegments("/login-submit") ||
        path.StartsWithSegments("/logout") ||
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/ready")) return false;
    if (path.StartsWithSegments("/_framework") || path.StartsWithSegments("/_content")) return false;
    if (path.Value is "/favicon.png" or "/app.css" or "/Tunarrly.Web.styles.css") return false;
    return true;
}

static string SafeReturnUrl(string? returnUrl)
    => string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') || returnUrl.StartsWith("//") ? "/" : returnUrl;
