using GhostLang.Core.Services.Erasure;

namespace GhostLang.Core.Settings.Erasure;

public interface ITextErasureEngineFactory
{
    ITextErasureEngine CreateEngine(ErasureEngineOptions options);
}
