using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services.AudioCapture;

public class AudioCaptureServiceFactory : IAudioCaptureServiceFactory
{
    public IAudioCaptureService Create(AudioCaptureSource source)
    {
        return source switch
        {
            AudioCaptureSource.Microphone => new MicrophoneCaptureService(),
            AudioCaptureSource.SystemLoopback => new SystemLoopbackCaptureService(),
            _ => throw new NotSupportedException($"Audio capture source '{source}' is not supported.")
        };
    }
}