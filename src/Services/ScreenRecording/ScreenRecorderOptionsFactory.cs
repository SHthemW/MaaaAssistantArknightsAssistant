using ScreenRecorderLib;

namespace Game_Daily_Routine_Launcher;

public static class ScreenRecorderOptionsFactory
{
    public static RecorderOptions Create(ScreenRecordingConfig config)
    {
        var profile = ScreenRecordingProfile.FromConfig(config);

        return new RecorderOptions
        {
            SourceOptions = new SourceOptions
            {
                RecordingSources =
                [
                    new DisplayRecordingSource(DisplayRecordingSource.MainMonitor)
                ]
            },
            OutputOptions = new OutputOptions
            {
                RecorderMode = RecorderMode.Video,
                OutputFrameSize = new ScreenSize(profile.Width, profile.Height),
                Stretch = StretchMode.Uniform
            },
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = config.RecordSystemAudio,
                IsInputDeviceEnabled = false,
                IsOutputDeviceEnabled = config.RecordSystemAudio,
                Bitrate = AudioBitrate.bitrate_128kbps,
                Channels = AudioChannels.Stereo
            },
            VideoEncoderOptions = new VideoEncoderOptions
            {
                Bitrate = profile.Bitrate,
                Framerate = profile.Framerate,
                IsFixedFramerate = true,
                Encoder = new H264VideoEncoder
                {
                    BitrateMode = H264BitrateControlMode.CBR,
                    EncoderProfile = H264Profile.Main
                },
                IsFragmentedMp4Enabled = false,
                IsHardwareEncodingEnabled = true,
                IsLowLatencyEnabled = false,
                IsMp4FastStartEnabled = false,
                IsThrottlingDisabled = false
            },
            MouseOptions = new MouseOptions
            {
                IsMousePointerEnabled = true,
                IsMouseClicksDetected = false
            }
        };
    }
}
