using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GameBar.Web.Client.Services;

/// <summary>
/// Encapsulates JS interop with the gameBarPixi module providing typed, cached access.
/// </summary>
public sealed class GameBarPixiInterop : IAsyncDisposable
{
    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private bool _destroyed;

    public GameBarPixiInterop(NavigationManager navigation, IJSRuntime jsRuntime)
    {
        _navigation = navigation;
        _jsRuntime = jsRuntime;
    }

    private string GetModuleUrl()
    {
        var url = new Uri(new Uri(_navigation.BaseUri), "dist/gameBarPixi.js");
        return url.ToString();
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (_module is null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", GetModuleUrl());
        }
        return _module;
    }

    /// <summary>
    /// Initializes Pixi with the provided container element.
    /// Safe to call multiple times; subsequent calls are ignored.
    /// </summary>
    public async Task InitAsync(ElementReference container)
    {
        if (_destroyed) // prevent re-init after destroy
            return;
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("init", container);
    }
    
    public async Task LoadAssetsAsync()
    {
        if (_destroyed)
            return;

        var assetDict = new Dictionary<string, Uri>()
        {
            { "idle", new Uri(new Uri(_navigation.BaseUri), "assets/Player_Idle.png") },
            { "run", new Uri(new Uri(_navigation.BaseUri), "assets/Player_Run.png") }
        };

        var module = await GetModuleAsync();
        foreach (var kvp in assetDict)
        {
            await module.InvokeVoidAsync("loadAsset", kvp.Key, kvp.Value.ToString());
        }
    }

    /// <summary>
    /// Renders the current list of players.
    /// </summary>
    public async Task RenderAsync(IEnumerable<PixiPlayer> players)
    {
        if (_destroyed)
            return;
        var module = await GetModuleAsync();
        var jsPlayers = players.Select(p => new { id = p.Id, x = p.X, y = p.Y }).ToArray();
        await module.InvokeVoidAsync("render", new { players = jsPlayers });
    }

    /// <summary>
    /// Destroys Pixi and disposes module.
    /// Safe to call multiple times.
    /// </summary>
    public async Task DestroyAsync()
    {
        if (_destroyed)
            return;
        _destroyed = true;
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("destroy");
            }
            catch
            {
                // swallow teardown errors
            }
            finally
            {
                await _module.DisposeAsync();
                _module = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DestroyAsync();
    }
}

