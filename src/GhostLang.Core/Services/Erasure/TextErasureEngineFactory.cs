using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Erasure;

namespace GhostLang.Core.Services.Erasure;

public class TextErasureEngineFactory : ITextErasureEngineFactory
{
    public ITextErasureEngine CreateEngine(ErasureEngineOptions options)
    {
        return options switch
        {
            SolidColorErasureOptions solid => new SolidColorErasureEngine(solid),
            OpenCvErasureOptions cv => new OpenCvErasureEngine(cv),
            _ => throw new ArgumentException("Unknown Text Erasure options type.")
        };
    }
}
