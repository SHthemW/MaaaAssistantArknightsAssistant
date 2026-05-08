namespace Game_Daily_Routine_Launcher;

public class AppConfig
{
    public List<GameTaskConfig> Tasks { get; set; } = [];
    public bool MuteOnStart { get; set; } = true;
    public bool MuteOnlyOnAutoRun { get; set; }
    public bool ShutdownOnComplete { get; set; }
    public int ScheduledHour { get; set; } = 4;
    public int ScheduledMinute { get; set; }
    public int ScheduledEndHour { get; set; } = 6;
    public int ScheduledEndMinute { get; set; }
    public int PollIntervalSeconds { get; set; } = 60;
    public bool WebhookEnabled { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookBody { get; set; } = "{\"time\":\"__TIME__\",\"content\":\"__CONTENT__\"}";

    public bool IsInScheduledTimeRange(TimeOnly now)
    {
        var start = new TimeOnly(ScheduledHour, ScheduledMinute);
        var end = new TimeOnly(ScheduledEndHour, ScheduledEndMinute);

        return start <= end
            ? now >= start && now < end
            : now >= start || now < end;
    }
}
