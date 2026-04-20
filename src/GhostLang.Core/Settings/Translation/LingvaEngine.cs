using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Utilities;

namespace GhostLang.Core.Settings.Translation;

public class LingvaEngine(LingvaOptions options) : ITranslationEngine
{
    private static readonly HttpClient HttpClient = new();

    public IReadOnlySet<SupportedLanguage> SupportedLanguages => LanguageCapabilitySets.AllTwenty;

    public async Task<string> TranslateAsync(string text, SupportedLanguage targetLanguage,
        List<SupportedLanguage> sourceLanguages)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var targetCode = targetLanguage.ToIsoLanguageCode();

        var sourceCode = sourceLanguages is { Count: 1 }
            ? sourceLanguages[0].ToIsoLanguageCode()
            : "auto";

        var baseUrl = options.InstanceUrl.TrimEnd('/');

        try
        {
            var url = $"{baseUrl}/api/v1/{sourceCode}/{targetCode}/{Uri.EscapeDataString(text)}";

            var response = await HttpClient.GetFromJsonAsync<LingvaResponse>(url);

            return response?.Translation ?? text;
        }
        catch (Exception ex)
        {
            return $"[Translation error (Lingva): {ex.Message}]";
        }
    }

    private class LingvaResponse
    {
        [JsonPropertyName("translation")]
        public string? Translation { get; set; }
    }
}
