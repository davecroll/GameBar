using GameBar.Game.Simulation;
using GameBar.Web.Client.Pages;
using GameBar.Web.Components;
using GameBar.Web.HostedServices;
using GameBar.Web.Hubs;
using GameBar.Web.Services;
using GameBar.Web.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSignalR();

builder.Services.AddSingleton<IGameSimulation, GameSimulation>();
builder.Services.AddSingleton<GameSessionManager>();
builder.Services.AddHostedService<GameLoopHostedService>();

builder.Services.AddScoped<GameClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GameBar.Web.Client._Imports).Assembly);

app.MapHub<GameHub>("/hubs/game");

app.Run();