using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;

namespace GhostLang.Infrastructure.Services.OCR.Tesseract;

public class TessdataService : ITessdataService
{
    private const string FastRepo = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/master/";
    private const string BestRepo = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_best/master/";
    
    private readonly string _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

    public TessdataService()
    {
        if (!Directory.Exists(_tessDataPath))
        {
            Directory.CreateDirectory(_tessDataPath);
        }
    }

    public List<TessdataModel> GetAvailableLanguages()
    {
        var languages = new List<(string Name, string Code)>
        {
            ("English", "eng"),
            ("Russian", "rus"),
            ("Ukrainian", "ukr"),
            ("German", "deu"),
            ("French", "fra"),
            ("Spanish", "spa")
        };

        var result = new List<TessdataModel>();

        foreach (var lang in languages)
        {
            result.Add(CreateLink(lang.Name, lang.Code, "Fast", FastRepo));
            
            result.Add(CreateLink(lang.Name, lang.Code, "Best", BestRepo));
        }

        return result;
    }

    private TessdataModel CreateLink(string name, string code, string type, string baseUrl)
    {
        var fileName = $"{code}.traineddata";
        var filePath = Path.Combine(_tessDataPath, fileName);
        
        return new TessdataModel
        {
            LanguageName = name,
            Code = code,
            Type = type,
            DownloadUrl = $"{baseUrl}{fileName}",
            IsDownloaded = File.Exists(filePath)
        };
    }

    public async Task DownloadLanguageAsync(TessdataModel model)
    {
        using var client = new HttpClient();
        
        var filePath = Path.Combine(_tessDataPath, $"{model.Code}.traineddata");
        
        var data = await client.GetByteArrayAsync(model.DownloadUrl);
        
        await File.WriteAllBytesAsync(filePath, data);
    }
}