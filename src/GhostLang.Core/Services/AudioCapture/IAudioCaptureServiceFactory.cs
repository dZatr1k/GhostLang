using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services.AudioCapture;

public interface IAudioCaptureServiceFactory
{
    IAudioCaptureService Create(AudioCaptureSource source);
}
