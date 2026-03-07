using GameBar.Game;
using GameBar.Game.StateMachine;
using GameBar.Game.Characters.Movement;
using GameBar.Game.Characters.Action;
using GameBar.Server.Components;
using GameBar.Server.HostedServices;
using GameBar.Server.Hubs;
using GameBar.Server.Services;
using GameBar.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSignalR();

builder.Services.AddSingleton<IGameSimulation, GameSimulation>(sp =>
{
    var playerStates = new Dictionary<string, IPlayerState>
    {
        { "Idle", new IdleState() },
        { "Run", new RunState() },
        { "Fall", new FallState() },
        { "Jump", new JumpState() },
        { "Jab", new JabState() }
    };

    var logger = sp.GetRequiredService<ILogger<GameSimulation>>();
    return new GameSimulation(playerStates, logger);
});
builder.Services.AddSingleton<GameSessionManager>();
builder.Services.AddHostedService<GameLoopHostedService>();

builder.Services.AddScoped<GameBarPixiInterop>();
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
    .AddAdditionalAssemblies(typeof(GameBar.Client._Imports).Assembly);

app.MapHub<GameHub>("/hubs/game");

app.Run();