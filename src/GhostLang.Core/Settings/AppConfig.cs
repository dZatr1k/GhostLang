using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Audio;
using GhostLang.Core.Settings.Erasure;
using GhostLang.Core.Settings.ImagePreProcessing;
using GhostLang.Core.Settings.Ocr;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Settings;

public class AppConfig
{
    public bool IsImagePipelineEnabled { get; set; } = true;

    public Dictionary<string, bool> OptionalStepStates { get; set; } = new();

    public OcrEngineOptions? ActiveOcrEngine { get; set; }

    public ImagePreProcessOptions PreProcessOptions { get; set; } = new();

    public TextRenderingOptions TextRendering { get; set; } = new();

    public ErasureEngineOptions ActiveErasureEngine { get; set; } = new SolidColorErasureOptions();

    public TranslationEngineOptions ActiveTranslationEngine { get; set; } = new GTranslateOptions();

    public List<GlossaryRule> GlossaryRules { get; set; } = [];

    public GlossaryTokenMode GlossaryTokenMode { get; set; } = GlossaryTokenMode.Placeholder;

    public int CacheTtlMinutes { get; set; } = 60;

    public int CacheMaxCharacters { get; set; } = 10000;

    public string Theme { get; set; } = "Dark";

    public string Language { get; set; } = "en";

    public List<HotKeyBinding> HotKeys { get; set; } = DefaultHotKeys();

    public static List<HotKeyBinding> GetDefaultHotKeys() => DefaultHotKeys();

    public bool IsAudioPipelineEnabled { get; set; } = false;

    public AsrEngineOptions? ActiveAsrEngine { get; set; }

    public AudioCaptureSource ActiveAudioCaptureSource { get; set; } = AudioCaptureSource.SystemLoopback;

    public AudioPreProcessOptions AudioPreProcessOptions { get; set; } = new();

    public VadOptions VadOptions { get; set; } = new();

    public SubtitleOptions SubtitleOptions { get; set; } = new();

    public bool RecordingMode { get; set; } = false;

    public double MajorContentChangeThreshold { get; set; } = 0.30;

    public bool AdaptiveFpsEnabled { get; set; } = true;

    public int ScreenFastIntervalMs { get; set; } = 200;

    public int ScreenSlowIntervalMs { get; set; } = 2000;

    public int ScreenStableFramesToSlowDown { get; set; } = 3;

    public int LoopbackResamplerQuality { get; set; } = 60;

    public bool TranslationDeduplicationEnabled { get; set; } = true;

    private static List<HotKeyBinding> DefaultHotKeys() =>
    [
        new() { ActionId = "select_region", DisplayName = "HotKey_SelectRegion", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0002, Key = 0x51 },
        new() { ActionId = "screen_start", DisplayName = "HotKey_ScreenStart", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0003, Key = 0x51 },
        new() { ActionId = "screen_stop", DisplayName = "HotKey_ScreenStop", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0003, Key = 0x58 },
        new() { ActionId = "toggle_visibility", DisplayName = "HotKey_ToggleVisibility", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0002, Key = 0x48 },
        new() { ActionId = "move_up", DisplayName = "HotKey_MoveUp", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0002, Key = 0x26 },
        new() { ActionId = "move_down", DisplayName = "HotKey_MoveDown", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0002, Key = 0x28 },
        new() { ActionId = "move_left", DisplayName = "HotKey_MoveLeft", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0002, Key = 0x25 },
        new() { ActionId = "move_right", DisplayName = "HotKey_MoveRight", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0002, Key = 0x27 },
        new() { ActionId = "resize_up", DisplayName = "HotKey_ResizeUp", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0006, Key = 0x26 },
        new() { ActionId = "resize_down", DisplayName = "HotKey_ResizeDown", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0006, Key = 0x28 },
        new() { ActionId = "resize_left", DisplayName = "HotKey_ResizeLeft", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0006, Key = 0x25 },
        new() { ActionId = "resize_right", DisplayName = "HotKey_ResizeRight", GroupKey = "HotKeyGroup_Screen", Modifiers = 0x0006, Key = 0x27 },
        new() { ActionId = "start_stop_audio", DisplayName = "HotKey_StartStopAudio", GroupKey = "HotKeyGroup_Audio", Modifiers = 0x0003, Key = 0x41 },
        new() { ActionId = "toggle_subtitle_visibility", DisplayName = "HotKey_ToggleSubtitleVisibility", GroupKey = "HotKeyGroup_Audio", Modifiers = 0x0003, Key = 0x53 }
    ];
}
