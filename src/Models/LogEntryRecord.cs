namespace Game_Daily_Routine_Launcher;

public sealed record LogEntryRecord(DateTime Timestamp, string Message)
{
    public string DisplayText => $"[{Timestamp:HH:mm:ss}] {Message}";

    public string? RawBody { get; init; }
}
