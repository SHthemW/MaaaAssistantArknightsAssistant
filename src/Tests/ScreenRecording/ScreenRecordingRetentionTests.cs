using System.IO;

namespace Game_Daily_Routine_Launcher.Tests;

public sealed class ScreenRecordingRetentionTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"maaa-recording-retention-{Guid.NewGuid():N}");

    [Fact]
    public void DeleteExpiredUsesSevenDayBoundaryAndOnlyDeletesMp4Files()
    {
        Directory.CreateDirectory(_directory);
        var now = new DateTime(2026, 8, 21, 12, 0, 0);
        var oldRecording = CreateFile("20260801-120000.mp4", now);
        var justExpiredRecording = CreateFile("20260814-115959.mp4", now);
        var suffixedExpiredRecording = CreateFile("20260814-115958-01.mp4", now);
        var boundaryRecording = CreateFile("20260814-120000.mp4", now);
        var recentRecording = CreateFile("20260821-110000.mp4", now);
        var unrelatedFile = CreateFile("20260801-120000.txt", now);

        ScreenRecordingRetention.DeleteExpired(_directory, now);

        Assert.False(File.Exists(oldRecording));
        Assert.False(File.Exists(justExpiredRecording));
        Assert.False(File.Exists(suffixedExpiredRecording));
        Assert.True(File.Exists(boundaryRecording));
        Assert.True(File.Exists(recentRecording));
        Assert.True(File.Exists(unrelatedFile));
    }

    private string CreateFile(string name, DateTime lastWriteTime)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "test");
        File.SetLastWriteTime(path, lastWriteTime);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
