using NAudio.CoreAudioApi;

namespace Game_Daily_Routine_Launcher;

public class AudioService
{
    public bool IsMuted
    {
        get
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (device.AudioEndpointVolume == null) return false;
                return device.AudioEndpointVolume.Mute;
            }
            catch
            {
                return false;
            }
        }
    }

    public float Volume
    {
        get
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return device.AudioEndpointVolume?.MasterVolumeLevelScalar ?? 0f;
            }
            catch
            {
                return 0f;
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

            volume.Mute = mute;
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
