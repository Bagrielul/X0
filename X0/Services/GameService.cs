using System.Collections.Concurrent;
using X0.Models;

namespace X0.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly ConcurrentDictionary<string, string> _playerToRoom = new();

    private static readonly int[][] WinPatterns =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    // ?? Create Room ??????????????????????????????????????????????????????????
    public (bool success, string message, GameRoom? room) CreateRoom(string connectionId, string playerName)
    {
        var roomId = GenerateRoomId();
        var room = new GameRoom
        {
            RoomId = roomId,
            Status = GameStatus.Waiting
        };
        room.Players[0] = new PlayerInfo
        {
            ConnectionId = connectionId,
            Name = playerName,
            Symbol = CellState.X,
            Score = 0
        };
        _rooms[roomId] = room;
        _playerToRoom[connectionId] = roomId;
        return (true, roomId, room);
    }

    // ?? Join Room ?????????????????????????????????????????????????????????????
    public (bool success, string message, GameRoom? room) JoinRoom(string connectionId, string roomId, string playerName)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return (false, "Room not found. Check the code and try again.", null);

        if (room.Status != GameStatus.Waiting)
            return (false, "This room is already in progress or full.", null);

        if (room.Players[0]?.ConnectionId == connectionId)
            return (false, "You created this room — share the code with a friend!", null);

        room.Players[1] = new PlayerInfo
        {
            ConnectionId = connectionId,
            Name = playerName,
            Symbol = CellState.O,
            Score = 0
        };
        room.Status = GameStatus.Playing;
        _playerToRoom[connectionId] = roomId;
        return (true, "Joined successfully.", room);
    }

    // ?? Make Move ?????????????????????????????????????????????????????????????
    public (bool success, string message, GameRoom? room) MakeMove(string connectionId, int cellIndex)
    {
        if (!_playerToRoom.TryGetValue(connectionId, out var roomId))
            return (false, "Not in a room.", null);

        if (!_rooms.TryGetValue(roomId, out var room))
            return (false, "Room not found.", null);

        if (room.Status != GameStatus.Playing)
            return (false, "Game is not in progress.", null);

        var playerIndex = Array.FindIndex(room.Players, p => p?.ConnectionId == connectionId);
        if (playerIndex < 0)
            return (false, "Player not found.", null);

        if (room.CurrentTurn != playerIndex)
            return (false, "Not your turn.", null);

        if (cellIndex < 0 || cellIndex > 8)
            return (false, "Invalid cell index.", null);

        if (room.Board[cellIndex] != CellState.Empty)
            return (false, "Cell already taken.", null);

        room.Board[cellIndex] = room.Players[playerIndex]!.Symbol;
        room.MoveCount++;

        var winLine = CheckWin(room.Board, room.Players[playerIndex]!.Symbol);
        if (winLine != null)
        {
            room.WinningLine = winLine;
            room.WinnerId = connectionId;
            room.Status = GameStatus.Finished;
            room.Players[playerIndex]!.Score++;
        }
        else if (room.MoveCount >= 9)
        {
            room.WinnerId = "draw";
            room.Status = GameStatus.Finished;
        }
        else
        {
            room.CurrentTurn = 1 - room.CurrentTurn;
        }

        return (true, "Move accepted.", room);
    }

    // ?? Rematch ???????????????????????????????????????????????????????????????
    public (bool success, GameRoom? room) RequestRematch(string connectionId)
    {
        if (!_playerToRoom.TryGetValue(connectionId, out var roomId))
            return (false, null);

        if (!_rooms.TryGetValue(roomId, out var room))
            return (false, null);

        var playerIndex = Array.FindIndex(room.Players, p => p?.ConnectionId == connectionId);
        if (playerIndex < 0) return (false, null);

        room.RematchRequested[playerIndex] = true;

        if (room.RematchRequested[0] && room.RematchRequested[1])
        {
            // Reset board, swap starting turn and symbols for fairness
            room.Board = new CellState[9];
            room.WinningLine = null;
            room.WinnerId = null;
            room.Status = GameStatus.Playing;
            room.RematchRequested = new bool[2];
            room.MoveCount = 0;
            room.CurrentTurn = 1 - room.CurrentTurn;

            if (room.Players[0] != null && room.Players[1] != null)
            {
                (room.Players[0]!.Symbol, room.Players[1]!.Symbol) =
                    (room.Players[1]!.Symbol, room.Players[0]!.Symbol);
            }
        }

        return (true, room);
    }

    // ?? Chat ??????????????????????????????????????????????????????????????????
    public (bool success, GameRoom? room) AddChatMessage(string connectionId, string message)
    {
        if (!_playerToRoom.TryGetValue(connectionId, out var roomId))
            return (false, null);

        if (!_rooms.TryGetValue(roomId, out var room))
            return (false, null);

        var player = room.Players.FirstOrDefault(p => p?.ConnectionId == connectionId);
        if (player == null) return (false, null);

        var chatMsg = new ChatMessage
        {
            PlayerName = player.Name,
            Message = message.Length > 200 ? message[..200] : message,
            Timestamp = DateTime.UtcNow
        };
        room.ChatMessages.Add(chatMsg);
        if (room.ChatMessages.Count > 100)
            room.ChatMessages.RemoveAt(0);

        return (true, room);
    }

    // ?? Disconnect ????????????????????????????????????????????????????????????
    public (bool removed, string? roomId, GameRoom? room, int playerIndex) RemovePlayer(string connectionId)
    {
        if (!_playerToRoom.TryRemove(connectionId, out var roomId))
            return (false, null, null, -1);

        if (!_rooms.TryGetValue(roomId, out var room))
            return (false, roomId, null, -1);

        var playerIndex = Array.FindIndex(room.Players, p => p?.ConnectionId == connectionId);

        if (playerIndex >= 0)
        {
            if (room.Status == GameStatus.Playing)
            {
                room.Status = GameStatus.Finished;
                // Opponent wins by forfeit
                var opponentConn = room.Players[1 - playerIndex]?.ConnectionId;
                room.WinnerId = opponentConn ?? "disconnect";
                if (opponentConn != null)
                    room.Players[1 - playerIndex]!.Score++;
            }
            room.Players[playerIndex] = null;
        }

        if (room.Players[0] == null && room.Players[1] == null)
            _rooms.TryRemove(roomId, out _);

        return (true, roomId, room, playerIndex);
    }

    // ?? DTO Projection ????????????????????????????????????????????????????????
    public static GameStateDto ToDto(GameRoom room) => new()
    {
        RoomId = room.RoomId,
        Board = room.Board.Select(c => c == CellState.X ? "X" : c == CellState.O ? "O" : "").ToArray(),
        CurrentTurnName = room.Players[room.CurrentTurn]?.Name ?? "",
        CurrentTurnSymbol = room.Players[room.CurrentTurn]?.Symbol == CellState.X ? "X" : "O",
        Status = room.Status.ToString(),
        WinnerId = room.WinnerId,
        WinnerName = room.WinnerId == "draw" ? "Draw"
            : room.Players.FirstOrDefault(p => p?.ConnectionId == room.WinnerId)?.Name,
        WinningLine = room.WinningLine,
        Players = room.Players
            .Where(p => p != null)
            .Select(p => new PlayerDto
            {
                ConnectionId = p!.ConnectionId,
                Name = p.Name,
                Symbol = p.Symbol == CellState.X ? "X" : "O",
                Score = p.Score
            }).ToArray(),
        RematchRequested = room.RematchRequested,
        ChatMessages = room.ChatMessages
    };

    // ?? Helpers ???????????????????????????????????????????????????????????????
    private static int[]? CheckWin(CellState[] board, CellState symbol)
    {
        foreach (var pattern in WinPatterns)
            if (board[pattern[0]] == symbol && board[pattern[1]] == symbol && board[pattern[2]] == symbol)
                return pattern;
        return null;
    }

    private static string GenerateRoomId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        return new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}
