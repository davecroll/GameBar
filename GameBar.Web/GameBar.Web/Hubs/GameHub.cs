using GameBar.Game.Models;
using GameBar.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace GameBar.Web.Hubs;

public class GameHub(GameSessionManager gameSessionManager) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await gameSessionManager.AddPlayerAsync(Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GameSessionManager.DefaultGroupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await gameSessionManager.RemovePlayerAsync(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GameSessionManager.DefaultGroupName);
        await base.OnDisconnectedAsync(exception);
    }

    public Task SendInput(InputCommand input)
    {
        input.PlayerId = Context.ConnectionId;
        gameSessionManager.EnqueueInput(Context.ConnectionId, input);
        return Task.CompletedTask;
    }
}

