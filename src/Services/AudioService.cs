using NAudio.CoreAudioApi;

namespace Game_Daily_Routine_Launcher;

public class AudioService
{
    private float _lastVolume = 0.5f;
    private bool _hasSavedVolume = false;

    public bool IsMuted
    {
        get
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (device.AudioEndpointVolume == null) return false;
                return device.AudioEndpointVolume.MasterVolumeLevelScalar < 0.01f;
            }
            catch
            {
                return false;
            }
        }
    }

    public void SetMute(bool mute)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var volume = device.AudioEndpointVolume;
            if (volume == null) return;

            if (mute)
            {
                // 保存当前音量
                _lastVolume = volume.MasterVolumeLevelScalar;
                _hasSavedVolume = true;
                // 设置音量为 0
                volume.MasterVolumeLevelScalar = 0f;
            }
            else
            {
                // 恢复音量
                volume.MasterVolumeLevelScalar = _hasSavedVolume ? Math.Clamp(_lastVolume, 0.01f, 1f) : 0.5f;
            }
        }
        catch
        {
            // Gracefully ignore audio errors
        }
    }

    public void SetVolume(float level)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var volume = device.AudioEndpointVolume;
            if (volume == null) return;
            volume.MasterVolumeLevelScalar = Math.Clamp(level, 0f, 1f);
        }
        catch
        {
            // Gracefully ignore audio errors
        }
    }
}
