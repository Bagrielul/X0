namespace X0.Models;

public enum CellState { Empty, X, O }
public enum GameStatus { Waiting, Playing, Finished }

public class PlayerInfo
{
    public string ConnectionId { get; set; } = "";
    public string Name { get; set; } = "";
    public CellState Symbol { get; set; }
    public int Score { get; set; }
}

public class ChatMessage
{
    public string PlayerName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class GameRoom
{
    public string RoomId { get; set; } = "";
    public PlayerInfo?[] Players { get; set; } = new PlayerInfo?[2];
    public CellState[] Board { get; set; } = new CellState[9];
    public int CurrentTurn { get; set; } = 0;
    public GameStatus Status { get; set; } = GameStatus.Waiting;
    public int[]? WinningLine { get; set; }
    public string? WinnerId { get; set; }
    public bool[] RematchRequested { get; set; } = new bool[2];
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public int MoveCount { get; set; } = 0;
}

public class GameStateDto
{
    public string RoomId { get; set; } = "";
    public string[] Board { get; set; } = new string[9];
    public string CurrentTurnName { get; set; } = "";
    public string CurrentTurnSymbol { get; set; } = "";
    public string Status { get; set; } = "";
    public string? WinnerName { get; set; }
    public string? WinnerId { get; set; }
    public int[]? WinningLine { get; set; }
    public PlayerDto[] Players { get; set; } = Array.Empty<PlayerDto>();
    public bool[] RematchRequested { get; set; } = new bool[2];
    public List<ChatMessage> ChatMessages { get; set; } = new();
}

public class PlayerDto
{
    public string ConnectionId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public int Score { get; set; }
}
