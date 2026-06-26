namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private Task? _startupTwinkleTrayTask;
    private Task? _cleanupTask;
    private bool _isCleaningUp;

    private void StartStartupMuteEnforcement()
    {
        _originalMuteState = _audioService.IsMuted;
        _originalVolume = _audioService.Volume;
        _didAutoMute = true;
        _startupMuteSuccessStreak = 0;

        _startupMuteTimer.Start();
        EnforceStartupMuteOnce();
    }

    private void StartStartupTwinkleTrayEnforcement()
    {
        _didDimTwinkleTray = true;
        _startupTwinkleTraySuccessStreak = 0;

        _startupTwinkleTrayTimer.Start();
        _startupTwinkleTrayTask = EnforceStartupTwinkleTrayAsync();
    }

    private void OnStartupMuteTimerTick(object? sender, EventArgs e) => EnforceStartupMuteOnce();

    private void OnStartupTwinkleTrayTimerTick(object? sender, EventArgs e)
    {
        _startupTwinkleTrayTask = EnforceStartupTwinkleTrayAsync();
    }

    private void EnforceStartupMuteOnce()
    {
        if (_audioService.IsMuted)
        {
            IsMuted = true;
            if (++_startupMuteSuccessStreak >= 3)
                _startupMuteTimer.Stop();

            return;
        }

        _audioService.SetMute(true);
        _audioService.SetVolume(0f);
        IsMuted = _audioService.IsMuted;

        if (IsMuted)
        {
            if (++_startupMuteSuccessStreak >= 3)
                _startupMuteTimer.Stop();
        }
        else
        {
            _startupMuteSuccessStreak = 0;
        }
    }

    private async Task EnforceStartupTwinkleTrayAsync()
    {
        if (_isCleaningUp || _twinkleTrayStartupInProgress)
            return;

        _twinkleTrayStartupInProgress = true;
        try
        {
            var result = await Task.Run(() => _twinkleTrayService.DetectAvailability());
            if (_isCleaningUp)
                return;

            if (!result.IsAvailable)
            {
                TwinkleTrayAvailabilityMessage = result.Message;
                return;
            }

            TwinkleTrayIsAvailable = true;
            TwinkleTrayAvailabilityMessage = result.Message;

            if (_originalTwinkleTrayStates.Count == 0)
                _originalTwinkleTrayStates = await Task.Run(() => _twinkleTrayService.CaptureCurrentStates());

            if (_isCleaningUp)
                return;

            var dimResult = await _twinkleTrayService.SetAllLowestAsync(result);
            if (_isCleaningUp)
                return;

            if (dimResult.Success)
            {
                _didDimTwinkleTray = true;
                if (++_startupTwinkleTraySuccessStreak >= 3)
                    _startupTwinkleTrayTimer.Stop();
            }
            else
            {
                _startupTwinkleTraySuccessStreak = 0;
                TwinkleTrayAvailabilityMessage = dimResult.Message;
            }
        }
        finally
        {
            _twinkleTrayStartupInProgress = false;
        }
    }

    public Task CleanupAsync() => _cleanupTask ??= CleanupCoreAsync();

    private async Task CleanupCoreAsync()
    {
        _isCleaningUp = true;
        _scheduleTimer.Stop();
        _startupMuteTimer.Stop();
        _startupTwinkleTrayTimer.Stop();
        IsSchedulePolling = false;

        if (_startupTwinkleTrayTask is not null)
            await _startupTwinkleTrayTask;

        await _webhookRelayService.StopAsync();
        RestoreOriginalMuteState();
        await RestoreOriginalTwinkleTrayStateAsync();
    }

    private void RestoreOriginalMuteState()
    {
        if (!_didAutoMute || !_audioService.IsMuted)
            return;

        _audioService.SetMute(_originalMuteState);
        _audioService.SetVolume(_originalVolume);
        AddLog("退出时已恢复原始静音状态。");
    }

    private async Task RestoreOriginalTwinkleTrayStateAsync()
    {
        if (!_didDimTwinkleTray || !TwinkleTrayIsAvailable || _originalTwinkleTrayStates.Count == 0)
            return;

        var result = await Task.Run(() => _twinkleTrayService.DetectAvailability());
        if (!result.IsAvailable)
        {
            AddLog($"退出时恢复亮度失败：{result.Message}");
            return;
        }

        var restoreResult = await _twinkleTrayService.RestoreAsync(result, _originalTwinkleTrayStates);
        AddLog(restoreResult.Success
            ? "退出时已恢复原始亮度。"
            : $"退出时恢复亮度失败：{restoreResult.Message}");
    }
}
