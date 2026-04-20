using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;

namespace GhostLang.Core.Settings.Ocr;

public class OcrSpaceEngine(OcrSpaceOptions options) : IOcrEngine
{
    private const string Endpoint = "https://api.ocr.space/parse/image";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static readonly Dictionary<SupportedLanguage, string> LanguageCodes = new()
    {
        { SupportedLanguage.English, "eng" },
        { SupportedLanguage.Russian, "rus" },
        { SupportedLanguage.Japanese, "jpn" },
        { SupportedLanguage.ChineseSimplified, "chs" },
        { SupportedLanguage.ChineseTraditional, "cht" },
        { SupportedLanguage.French, "fre" },
        { SupportedLanguage.German, "ger" },
        { SupportedLanguage.Spanish, "spa" },
        { SupportedLanguage.Italian, "ita" },
        { SupportedLanguage.Portuguese, "por" },
        { SupportedLanguage.Polish, "pol" },
        { SupportedLanguage.Korean, "kor" },
        { SupportedLanguage.Arabic, "ara" },
        { SupportedLanguage.Turkish, "tur" },
        { SupportedLanguage.Ukrainian, "ukr" },
        { SupportedLanguage.Dutch, "dut" },
        { SupportedLanguage.Vietnamese, "vnm" },
        { SupportedLanguage.Thai, "tha" }
    };

    public IReadOnlySet<SupportedLanguage> SupportedLanguages => LanguageCapabilitySets.OcrSpace;

    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        return Task.FromResult(LanguageCodes.ContainsKey(language));
    }

    public async Task<List<TextFragment>> RecognizeTextAsync(TranslationContext context, List<SupportedLanguage> languages)
    {
        var imageBytes = context.ProcessedImage ?? context.OriginalImage;
        if (imageBytes is not { Length: > 0 })
            return [];

        var lang = languages.FirstOrDefault(l => LanguageCodes.ContainsKey(l));
        var langCode = lang != SupportedLanguage.Unknown && LanguageCodes.TryGetValue(lang, out var code)
            ? code
            : "eng";

        var base64 = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("apikey", options.ApiKey);

        var formData = new Dictionary<string, string>
        {
            { "base64Image", base64 },
            { "language", langCode },
            { "isOverlayRequired", "true" },
            { "OCREngine", options.OcrEngine.ToString() },
            { "scale", "true" }
        };

        request.Content = new FormUrlEncodedContent(formData);

        var response = await Http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<OcrSpaceResponse>(json);
        if (result is null || result.IsErroredOnProcessing || result.ParsedResults is not { Count: > 0 })
            return [];

        var fragments = new List<TextFragment>();

        foreach (var parsed in result.ParsedResults)
        {
            if (parsed.FileParseExitCode != 1)
                continue;

            var lines = parsed.TextOverlay?.Lines;
            if (lines == null)
                continue;

            foreach (var line in lines)
            {
                var text = line.LineText?.Trim();
                if (string.IsNullOrWhiteSpace(text) || line.Words is not { Count: > 0 })
                    continue;

                var minX = line.Words.Min(w => w.Left);
                var minY = line.Words.Min(w => w.Top);
                var maxX = line.Words.Max(w => w.Left + w.Width);
                var maxY = line.Words.Max(w => w.Top + w.Height);

                fragments.Add(new TextFragment
                {
                    OriginalText = text,
                    Bounds = new BoundingBox
                    {
                        X = (int)minX,
                        Y = (int)minY,
                        Width = (int)(maxX - minX),
                        Height = (int)(maxY - minY)
                    }
                });
            }
        }

        return fragments;
    }

    #region DTOs

    private class OcrSpaceResponse
    {
        [JsonPropertyName("ParsedResults")]
        public List<OcrParsedResult>? ParsedResults { get; set; }

        [JsonPropertyName("OCRExitCode")]
        public int OcrExitCode { get; set; }

        [JsonPropertyName("IsErroredOnProcessing")]
        public bool IsErroredOnProcessing { get; set; }

        [JsonPropertyName("ProcessingTimeInMilliseconds")]
        public string? ProcessingTimeInMilliseconds { get; set; }
    }

    private class OcrParsedResult
    {
        [JsonPropertyName("TextOverlay")]
        public OcrTextOverlay? TextOverlay { get; set; }

        [JsonPropertyName("FileParseExitCode")]
        public int FileParseExitCode { get; set; }

        [JsonPropertyName("ParsedText")]
        public string? ParsedText { get; set; }
    }

    private class OcrTextOverlay
    {
        [JsonPropertyName("Lines")]
        public List<OcrLine>? Lines { get; set; }

        [JsonPropertyName("HasOverlay")]
        public bool HasOverlay { get; set; }
    }

    private class OcrLine
    {
        [JsonPropertyName("LineText")]
        public string? LineText { get; set; }

        [JsonPropertyName("Words")]
        public List<OcrWord>? Words { get; set; }
    }

    private class OcrWord
    {
        [JsonPropertyName("WordText")]
        public string? WordText { get; set; }

        [JsonPropertyName("Left")]
        public double Left { get; set; }

        [JsonPropertyName("Top")]
        public double Top { get; set; }

        [JsonPropertyName("Height")]
        public double Height { get; set; }

        [JsonPropertyName("Width")]
        public double Width { get; set; }
    }

    #endregion
}
