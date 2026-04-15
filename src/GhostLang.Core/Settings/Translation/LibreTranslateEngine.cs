using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Utilities;

namespace GhostLang.Core.Settings.Translation;

public class LibreTranslateEngine(LibreTranslateOptions options) : ITranslationEngine
{
    private static readonly HttpClient HttpClient = new();

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
            var requestBody = new Dictionary<string, string>
            {
                { "q", text },
                { "source", sourceCode },
                { "target", targetCode },
                { "format", "text" }
            };

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                requestBody["api_key"] = options.ApiKey;

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await HttpClient.PostAsync($"{baseUrl}/translate", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"[LibreTranslate error ({response.StatusCode}): {errorBody}]";
            }

            var result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>();

            return result?.TranslatedText ?? text;
        }
        catch (Exception ex)
        {
            return $"[Translation error (LibreTranslate): {ex.Message}]";
        }
    }

    private class LibreTranslateResponse
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }
}