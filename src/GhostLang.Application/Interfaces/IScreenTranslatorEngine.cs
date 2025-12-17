
using GhostLang.Application.Models;

namespace GhostLang.Application.Interfaces;

public interface IScreenTranslatorEngine : IDisposable
{
    event EventHandler<TranslationResultArgs> ResultReceived;
    event EventHandler<string> ErrorOccurred;
    bool IsRunning { get; }
    void Start();
    void Stop();
}