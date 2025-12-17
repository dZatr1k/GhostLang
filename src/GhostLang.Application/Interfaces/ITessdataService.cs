using GhostLang.Application.Models;

namespace GhostLang.Application.Interfaces;

public interface ITessdataService
{
    List<TessdataModel> GetAvailableLanguages();
    Task DownloadLanguageAsync(TessdataModel model);
}