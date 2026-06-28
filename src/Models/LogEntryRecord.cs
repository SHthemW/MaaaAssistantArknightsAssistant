namespace Game_Daily_Routine_Launcher;

public sealed record LogEntryRecord(DateTime Timestamp, string Message)
{
    public string DisplayText => string.IsNullOrWhiteSpace(RawBody)
        ? $"[{Timestamp:HH:mm:ss}] {Message}"
        : $"[{Timestamp:HH:mm:ss}] {Message}\n{RawBody}";

    public string? RawBody { get; init; }

    public WebhookPushContentCategory Category { get; init; } = WebhookPushContentCategory.Other;
}
