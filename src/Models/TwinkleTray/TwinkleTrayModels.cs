namespace Game_Daily_Routine_Launcher;

public sealed record TwinkleTrayMonitorState(
    string SelectorKind,
    string SelectorValue,
    int Brightness,
    string? DisplayName = null)
{
    public string BuildSetArguments()
    {
        return SelectorKind == "MonitorID"
            ? $"--MonitorID=\"{SelectorValue}\" --Set={Brightness}"
            : $"--MonitorNum={SelectorValue} --Set={Brightness}";
    }
}

public sealed record TwinkleTrayAvailability(
    bool IsAvailable,
    string Message,
    string? ExecutablePath,
    IReadOnlyList<TwinkleTrayMonitorState> Monitors)
{
    public static TwinkleTrayAvailability Unavailable(string message) =>
        new(false, message, null, Array.Empty<TwinkleTrayMonitorState>());
}

public sealed record TwinkleTrayCommandResult(bool Success, string Message);
