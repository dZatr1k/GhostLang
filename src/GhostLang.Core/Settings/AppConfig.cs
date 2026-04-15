using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
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

    public string Language { get; set; } = "ru";

    public List<HotKeyBinding> HotKeys { get; set; } = DefaultHotKeys();

    private static List<HotKeyBinding> DefaultHotKeys() =>
    [
        new() { ActionId = "toggle_visibility", DisplayName = "HotKey_ToggleVisibility", Modifiers = 0x0002, Key = 0x48 },
        new() { ActionId = "move_up", DisplayName = "HotKey_MoveUp", Modifiers = 0x0002, Key = 0x26 },
        new() { ActionId = "move_down", DisplayName = "HotKey_MoveDown", Modifiers = 0x0002, Key = 0x28 },
        new() { ActionId = "move_left", DisplayName = "HotKey_MoveLeft", Modifiers = 0x0002, Key = 0x25 },
        new() { ActionId = "move_right", DisplayName = "HotKey_MoveRight", Modifiers = 0x0002, Key = 0x27 },
        new() { ActionId = "resize_up", DisplayName = "HotKey_ResizeUp", Modifiers = 0x0006, Key = 0x26 },
        new() { ActionId = "resize_down", DisplayName = "HotKey_ResizeDown", Modifiers = 0x0006, Key = 0x28 },
        new() { ActionId = "resize_left", DisplayName = "HotKey_ResizeLeft", Modifiers = 0x0006, Key = 0x25 },
        new() { ActionId = "resize_right", DisplayName = "HotKey_ResizeRight", Modifiers = 0x0006, Key = 0x27 }
    ];
}