using System.IO;
using ScreenRecorderLib;

namespace Game_Daily_Routine_Launcher;

public sealed partial class ScreenRecordingService : IDisposable
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(30);
    private readonly object _sync = new();
    private Recorder? _recorder;
    private TaskCompletionSource<string>? _completionSource;
    private Task<string?>? _stopTask;
    private bool _disposed;

    public event Action<string>? RecordingFailed;

    public ScreenRecordingService()
    {
        ScreenRecordingRetention.DeleteExpired(GetRecordDirectory(), DateTime.Now);
    }

    public bool IsRecording
    {
        get
        {
            lock (_sync)
                return _recorder is not null;
        }
    }

    public string Start(ScreenRecordingConfig config)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            if (_recorder is not null)
                throw new InvalidOperationException("录屏已在进行中。");

            var recordDirectory = GetRecordDirectory();
            Directory.CreateDirectory(recordDirectory);
            ScreenRecordingRetention.DeleteExpired(recordDirectory, DateTime.Now);

            var path = CreateUniqueFilePath(recordDirectory, DateTime.Now);
            var recorder = Recorder.CreateRecorder(ScreenRecorderOptionsFactory.Create(config));
            var completionSource = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            recorder.OnRecordingComplete += OnRecordingComplete;
            recorder.OnRecordingFailed += OnRecordingFailed;

            _recorder = recorder;
            _completionSource = completionSource;
            try
            {
                recorder.Record(path);
                return path;
            }
            catch
            {
                ReleaseRecorder(recorder);
                throw;
            }
        }
    }

    public Task<string?> StopAsync()
    {
        Recorder recorder;
        Task<string> completionTask;
        TaskCompletionSource<string?> stopSource;

        lock (_sync)
        {
            if (_recorder is null || _completionSource is null)
                return Task.FromResult<string?>(null);

            if (_stopTask is not null)
                return _stopTask;

            recorder = _recorder;
            completionTask = _completionSource.Task;
            stopSource = new TaskCompletionSource<string?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _stopTask = stopSource.Task;
        }

        _ = StopAndReleaseAsync(recorder, completionTask, stopSource);
        return stopSource.Task;
    }

    private async Task StopAndReleaseAsync(
        Recorder recorder,
        Task<string> completionTask,
        TaskCompletionSource<string?> stopSource)
    {
        string? completedPath = null;
        Exception? failure = null;

        try
        {
            if (!completionTask.IsCompleted)
                recorder.Stop();

            completedPath = await completionTask
                .WaitAsync(StopTimeout)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            try
            {
                ReleaseRecorder(recorder);
            }
            catch (Exception ex)
            {
                failure ??= ex;
            }

            ScreenRecordingRetention.DeleteExpired(GetRecordDirectory(), DateTime.Now);

            if (failure is null)
                stopSource.TrySetResult(completedPath);
            else
                stopSource.TrySetException(failure);
        }
    }

    private void OnRecordingComplete(object? sender, RecordingCompleteEventArgs e)
    {
        lock (_sync)
        {
            if (ReferenceEquals(sender, _recorder))
                _completionSource?.TrySetResult(e.FilePath);
        }
    }

    private void OnRecordingFailed(object? sender, RecordingFailedEventArgs e)
    {
        var message = string.IsNullOrWhiteSpace(e.Error) ? "未知错误" : e.Error;
        lock (_sync)
        {
            if (!ReferenceEquals(sender, _recorder))
                return;

            _completionSource?.TrySetException(new InvalidOperationException(message));
        }

        try
        {
            RecordingFailed?.Invoke(message);
        }
        catch (Exception ex)
        {
            RuntimeLogService.WriteException("记录录屏失败事件时发生异常", ex);
        }
    }

}
