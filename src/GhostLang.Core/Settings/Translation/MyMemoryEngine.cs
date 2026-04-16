using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Utilities;

namespace GhostLang.Core.Settings.Translation;

public class MyMemoryEngine(MyMemoryOptions options) : ITranslationEngine
{
    private static readonly HttpClient HttpClient = new();
    private const string BaseUrl = "https://api.mymemory.translated.net/get";

    public async Task<string> TranslateAsync(string text, SupportedLanguage targetLanguage,
        List<SupportedLanguage> sourceLanguages)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var targetCode = targetLanguage.ToIsoLanguageCode();

        var sourceCode = sourceLanguages is { Count: 1 }
            ? sourceLanguages[0].ToIsoLanguageCode()
            : "autodetect";

        if (sourceCode == targetCode)
            return text;

        var langPair = $"{sourceCode}|{targetCode}";

        try
        {
            var query = $"?q={Uri.EscapeDataString(text)}&langpair={Uri.EscapeDataString(langPair)}";

            if (!string.IsNullOrWhiteSpace(options.Email))
                query += $"&de={Uri.EscapeDataString(options.Email)}";

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                query += $"&key={Uri.EscapeDataString(options.ApiKey)}";

            var response = await HttpClient.GetFromJsonAsync<MyMemoryResponse>(BaseUrl + query);

            if (response?.ResponseStatus is not 200)
            {
                if (response?.ResponseDetails != null &&
                    response.ResponseDetails.Contains("select two distinct languages", StringComparison.OrdinalIgnoreCase))
                    return text;

                return $"[MyMemory error: {response?.ResponseDetails}]";
            }

            return response.ResponseData?.TranslatedText ?? text;
        }
        catch (Exception ex)
        {
            return $"[Translation error (MyMemory): {ex.Message}]";
        }
    }

    private class MyMemoryResponse
    {
        [JsonPropertyName("responseStatus")]
        public int ResponseStatus { get; set; }

        [JsonPropertyName("responseDetails")]
        public string? ResponseDetails { get; set; }

        [JsonPropertyName("responseData")]
        public MyMemoryResponseData? ResponseData { get; set; }
    }

    private class MyMemoryResponseData
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }
}