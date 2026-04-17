using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace GhostLang.Core.Settings.Asr;

public class AzureAsrEngine : IAsrEngine
{
    private readonly AzureAsrOptions _options;

    public AzureAsrEngine(AzureAsrOptions options)
    {
        _options = options;
    }

    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        return Task.FromResult(language != SupportedLanguage.Unknown);
    }

    public async Task<List<AudioFragment>> RecognizeAsync(AudioTranslationContext context, List<SupportedLanguage> languages)
    {
        if (context.OriginalAudio is null || context.OriginalAudio.Length == 0)
            return new List<AudioFragment>();

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Azure Speech API key is not set. Configure AzureAsrOptions.ApiKey and Region in settings.");
        }

        var speechConfig = SpeechConfig.FromSubscription(_options.ApiKey, _options.Region);

        var sourceLang = languages.FirstOrDefault(l => l != SupportedLanguage.Unknown);
        speechConfig.SpeechRecognitionLanguage = sourceLang != SupportedLanguage.Unknown
            ? sourceLang.ToWindowsLanguageTag()
            : "en-US";

        var pushFormat = AudioStreamFormat.GetWaveFormatPCM((uint)context.SampleRate, 16, (byte)context.ChannelCount);
        using var pushStream = AudioInputStream.CreatePushStream(pushFormat);
        pushStream.Write(context.OriginalAudio);
        pushStream.Close();

        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason != ResultReason.RecognizedSpeech || string.IsNullOrWhiteSpace(result.Text))
            return new List<AudioFragment>();

        return new List<AudioFragment>
        {
            new AudioFragment
            {
                OriginalText = result.Text,
                StartMs = result.OffsetInTicks / 10_000,
                EndMs = (result.OffsetInTicks + result.Duration.Ticks) / 10_000,
                Confidence = 1.0f
            }
        };
    }
}