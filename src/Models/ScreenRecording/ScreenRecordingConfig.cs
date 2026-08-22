namespace Game_Daily_Routine_Launcher;

public sealed class ScreenRecordingConfig
{
    public bool Enabled { get; set; }

    public bool Expanded { get; set; } = true;

    public ScreenRecordingResolution Resolution { get; set; } = ScreenRecordingResolution.Hd720p;

    public int Framerate { get; set; } = 30;

    public bool RecordSystemAudio { get; set; }
}
