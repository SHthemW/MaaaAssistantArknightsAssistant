namespace Game_Daily_Routine_Launcher.Tests;

public sealed class ScreenRecordingProfileTests
{
    [Fact]
    public void DefaultConfigUses720p30WithStandardBitrate()
    {
        var config = new ScreenRecordingConfig();

        var profile = ScreenRecordingProfile.FromConfig(config);

        Assert.Equal(1280, profile.Width);
        Assert.Equal(720, profile.Height);
        Assert.Equal(30, profile.Framerate);
        Assert.Equal(5_000_000, profile.Bitrate);
    }

    [Theory]
    [InlineData(ScreenRecordingResolution.Hd720p, 60, 1280, 720, 7_500_000)]
    [InlineData(ScreenRecordingResolution.FullHd1080p, 30, 1920, 1080, 8_000_000)]
    [InlineData(ScreenRecordingResolution.QuadHd1440p, 60, 2560, 1440, 24_000_000)]
    [InlineData(ScreenRecordingResolution.UltraHd2160p, 30, 3840, 2160, 45_000_000)]
    public void ConfigMapsToExpectedStandardProfile(
        ScreenRecordingResolution resolution,
        int framerate,
        int width,
        int height,
        int bitrate)
    {
        var profile = ScreenRecordingProfile.FromConfig(new ScreenRecordingConfig
        {
            Resolution = resolution,
            Framerate = framerate
        });

        Assert.Equal(width, profile.Width);
        Assert.Equal(height, profile.Height);
        Assert.Equal(framerate, profile.Framerate);
        Assert.Equal(bitrate, profile.Bitrate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(120)]
    public void UnsupportedFramerateFallsBackTo30(int framerate)
    {
        Assert.Equal(30, ScreenRecordingProfile.NormalizeFramerate(framerate));
    }
}
