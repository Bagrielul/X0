using Microsoft.AspNetCore.SignalR;
using X0.Services;

namespace X0.Hubs;

public class GameHub : Hub
{
    private readonly GameService _gameService;

    public GameHub(GameService gameService) => _gameService = gameService;

    // ?? Client ? Server ???????????????????????????????????????????????????????

    public async Task CreateRoom(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            await Clients.Caller.SendAsync("Error", "Player name cannot be empty.");
            return;
        }

        var (success, message, room) = _gameService.CreateRoom(Context.ConnectionId, playerName.Trim());
        if (!success || room == null)
        {
            await Clients.Caller.SendAsync("Error", message);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
        await Clients.Caller.SendAsync("RoomCreated", room.RoomId);
        await Clients.Group(room.RoomId).SendAsync("GameStateUpdated", GameService.ToDto(room));
    }

    public async Task JoinRoom(string roomId, string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(roomId))
        {
            await Clients.Caller.SendAsync("Error", "Room code and player name are required.");
            return;
        }

        var (success, message, room) = _gameService.JoinRoom(
            Context.ConnectionId, roomId.Trim().ToUpper(), playerName.Trim());

        if (!success || room == null)
        {
            await Clients.Caller.SendAsync("Error", message);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
        await Clients.Caller.SendAsync("RoomJoined", room.RoomId);
        await Clients.Group(room.RoomId).SendAsync("GameStateUpdated", GameService.ToDto(room));
    }

    public async Task MakeMove(int cellIndex)
    {
        var (success, message, room) = _gameService.MakeMove(Context.ConnectionId, cellIndex);
        if (!success || room == null)
        {
            await Clients.Caller.SendAsync("Error", message);
            return;
        }
        await Clients.Group(room.RoomId).SendAsync("GameStateUpdated", GameService.ToDto(room));
    }

    public async Task RequestRematch()
    {
        var (success, room) = _gameService.RequestRematch(Context.ConnectionId);
        if (!success || room == null) return;
        await Clients.Group(room.RoomId).SendAsync("GameStateUpdated", GameService.ToDto(room));
    }

    public async Task SendChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        var (success, room) = _gameService.AddChatMessage(Context.ConnectionId, message);
        if (!success || room == null) return;
        await Clients.Group(room.RoomId).SendAsync("GameStateUpdated", GameService.ToDto(room));
    }

    // ?? Disconnect ????????????????????????????????????????????????????????????
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (removed, roomId, room, _) = _gameService.RemovePlayer(Context.ConnectionId);
        if (removed && roomId != null && room != null)
        {
            var dto = GameService.ToDto(room);
            await Clients.Group(roomId).SendAsync("PlayerDisconnected", dto);
            await Clients.Group(roomId).SendAsync("GameStateUpdated", dto);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
