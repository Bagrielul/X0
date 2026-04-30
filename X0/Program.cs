using X0.Components;
using X0.Components;
using X0.Hubs;
using X0.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind to the PORT env-var injected by cloud hosts (Render, Railway, Fly…)
// Fall back to 8080 locally if not set.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HTTPS is handled by the cloud host's reverse proxy — no redirect needed here
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<GameHub>("/gamehub");

app.Run();

