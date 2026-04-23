using System.Runtime.InteropServices;

namespace Game_Daily_Routine_Launcher.Services;

public class AudioService
{
    public bool IsMuted
    {
        get
        {
            try
            {
                var device = GetDefaultAudioDevice();
                if (device == null) return false;
                var volume = (IAudioEndpointVolume)device;
                volume.GetMute(out var muted);
                return muted;
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
            var device = GetDefaultAudioDevice();
            if (device == null) return;
            var volume = (IAudioEndpointVolume)device;
            volume.SetMute(mute, Guid.Empty);
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
            var device = GetDefaultAudioDevice();
            if (device == null) return;
            var volume = (IAudioEndpointVolume)device;
            volume.SetMasterVolumeLevelScalar(Math.Clamp(level, 0f, 1f), Guid.Empty);
        }
        catch
        {
            // Gracefully ignore audio errors
        }
    }

    private static object? GetDefaultAudioDevice()
    {
        var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
        enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
        if (device == null) return null;
        var iidVolume = typeof(IAudioEndpointVolume).GUID;
        device.Activate(ref iidVolume, 0, IntPtr.Zero, out var obj);
        return obj;
    }

    #region COM Interop

    private enum EDataFlow { eRender = 0 }
    private enum ERole { eMultimedia = 1 }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int NotImpl1();
        int NotImpl2();
        int NotImpl3();
        int NotImpl4();
        int NotImpl5();
        int NotImpl6();
        int NotImpl7();
        int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int NotImpl8();
        int NotImpl9();
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    #endregion
}
