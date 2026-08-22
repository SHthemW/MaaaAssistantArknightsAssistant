namespace Game_Daily_Routine_Launcher;

public readonly record struct ScreenRecordingProfile(
    int Width,
    int Height,
    int Framerate,
    int Bitrate)
{
    public static readonly IReadOnlyList<int> SupportedFramerates = [15, 24, 30, 60];

    public static ScreenRecordingProfile FromConfig(ScreenRecordingConfig config)
    {
        var framerate = NormalizeFramerate(config.Framerate);
        var (width, height, bitrate30, bitrate60) = config.Resolution switch
        {
            ScreenRecordingResolution.FullHd1080p => (1920, 1080, 8_000_000, 12_000_000),
            ScreenRecordingResolution.QuadHd1440p => (2560, 1440, 16_000_000, 24_000_000),
            ScreenRecordingResolution.UltraHd2160p => (3840, 2160, 45_000_000, 68_000_000),
            _ => (1280, 720, 5_000_000, 7_500_000)
        };

        var bitrate = framerate <= 30
            ? bitrate30
            : (int)Math.Round(bitrate30 + (bitrate60 - bitrate30) * ((framerate - 30) / 30d));

        return new ScreenRecordingProfile(width, height, framerate, bitrate);
    }

    public static int NormalizeFramerate(int framerate)
    {
        return SupportedFramerates.Contains(framerate) ? framerate : 30;
    }
}
