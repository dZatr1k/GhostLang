using Azure;
using Azure.AI.Vision.ImageAnalysis;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;

namespace GhostLang.Core.Settings.Ocr;

public class AzureVisionOcrEngine(AzureVisionOcrOptions options) : IOcrEngine
{
    private static readonly HashSet<SupportedLanguage> SupportedLanguages =
    [
        SupportedLanguage.English,
        SupportedLanguage.Russian,
        SupportedLanguage.Japanese,
        SupportedLanguage.Chinese,
        SupportedLanguage.French,
        SupportedLanguage.German,
        SupportedLanguage.Spanish
    ];

    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        return Task.FromResult(SupportedLanguages.Contains(language));
    }

    public async Task<List<TextFragment>> RecognizeTextAsync(TranslationContext context, List<SupportedLanguage> languages)
    {
        var imageBytes = context.ProcessedImage ?? context.OriginalImage;
        if (imageBytes is not { Length: > 0 })
            return [];

        var client = new ImageAnalysisClient(
            new Uri(options.EndpointUrl),
            new AzureKeyCredential(options.ApiKey));

        var imageData = BinaryData.FromBytes(imageBytes);

        var analysisOptions = new ImageAnalysisOptions
        {
            Language = languages.FirstOrDefault(l => l != SupportedLanguage.Unknown).ToIsoLanguageCode()
        };

        var result = await client.AnalyzeAsync(
            imageData,
            VisualFeatures.Read,
            analysisOptions);

        var fragments = new List<TextFragment>();

        if (result.Value.Read?.Blocks == null)
            return fragments;

        foreach (var block in result.Value.Read.Blocks)
        {
            foreach (var line in block.Lines)
            {
                var text = line.Text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var points = line.BoundingPolygon;
                if (points == null || points.Count < 4)
                    continue;

                var minX = points.Min(p => p.X);
                var minY = points.Min(p => p.Y);
                var maxX = points.Max(p => p.X);
                var maxY = points.Max(p => p.Y);

                fragments.Add(new TextFragment
                {
                    OriginalText = text,
                    Bounds = new BoundingBox
                    {
                        X = minX,
                        Y = minY,
                        Width = maxX - minX,
                        Height = maxY - minY
                    }
                });
            }
        }

        return fragments;
    }
}