using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;
using GhostLang.Core.Services.Ocr;
using Tesseract;

namespace GhostLang.Core.Settings.Ocr;

public class TesseractOcrEngine(TesseractOcrOptions options, ITesseractModelManager modelManager)
    : IOcrEngine, IDisposable
{
    private TesseractEngine? _engine;
    private string _currentLoadedLanguages = string.Empty;

    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
    }
    
    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        var isDownloaded = modelManager.IsModelDownloaded(language, options.ModelType);
        return Task.FromResult(isDownloaded);
    }

    public async Task<List<TextFragment>> RecognizeTextAsync(TranslationContext context, List<SupportedLanguage> sourceLanguages)
    {
        return await Task.Run(() =>
        {
            if (sourceLanguages == null || sourceLanguages.Count == 0)
            {
                throw new ArgumentException("No source language selected for recognition.");
            }

            var fragments = new List<TextFragment>();

            if (sourceLanguages.Any(lang => !modelManager.IsModelDownloaded(lang, options.ModelType)))
            {
                return fragments;
            }

            var tesseractLangCodes = sourceLanguages
                .Select(lang => lang.ToTesseractCode())
                .Distinct()
                .ToList();

            var combinedLanguageString = string.Join("+", tesseractLangCodes);

            if (_engine == null || _currentLoadedLanguages != combinedLanguageString)
            {
                _engine?.Dispose();

                var tessDataPath = modelManager.GetModelDirectoryPath(options.ModelType);
                _engine = new TesseractEngine(tessDataPath, combinedLanguageString, EngineMode.Default);
                
                _currentLoadedLanguages = combinedLanguageString; 
            }

            _engine.DefaultPageSegMode = PageSegMode.SparseText;

            var imageToProcess = context.ProcessedImage ?? context.OriginalImage;
            if (imageToProcess == null || imageToProcess.Length == 0)
                return fragments;

            using var pix = Pix.LoadFromMemory(imageToProcess);
            using var page = _engine.Process(pix);
            using var iter = page.GetIterator();

            iter.Begin();

            do
            {
                if (!iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect)) continue;
                
                var text = iter.GetText(PageIteratorLevel.TextLine)?.Trim();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    fragments.Add(new TextFragment
                    {
                        OriginalText = text,
                        TranslatedText = string.Empty,
                        Bounds = new BoundingBox
                        {
                            X = (int)(rect.X1 / context.ScaleFactor),
                            Y = (int)(rect.Y1 / context.ScaleFactor),
                            Width = (int)(rect.Width / context.ScaleFactor),
                            Height = (int)(rect.Height / context.ScaleFactor)
                        }
                    });
                }
            } while (iter.Next(PageIteratorLevel.TextLine));

            return fragments;
        });
    }
}