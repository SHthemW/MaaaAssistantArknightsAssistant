using System.Text.Json.Serialization;

namespace Game_Daily_Routine_Launcher.Models;

public enum LaunchMode
{
    File,
    Uri
}

public class GameTaskConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ToolPath { get; set; } = string.Empty;
    public string ToolArgs { get; set; } = string.Empty;
    public string GameProcessName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LaunchMode LaunchMode { get; set; } = LaunchMode.File;

    public int DelayBeforeStartMs { get; set; }
}
