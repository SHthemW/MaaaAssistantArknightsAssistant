namespace Game_Daily_Routine_Launcher.Models;

public class AppConfig
{
    public List<GameTaskConfig> Tasks { get; set; } = [];
    public bool MuteOnStart { get; set; } = true;
    public bool ShutdownOnComplete { get; set; }
    public int ScheduledHour { get; set; } = 4;
    public int ScheduledMinute { get; set; }
    public int PollIntervalSeconds { get; set; } = 60;
}
