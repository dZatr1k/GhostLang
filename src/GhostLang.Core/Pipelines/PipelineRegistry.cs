using GhostLang.Core.Pipelines.Descriptors;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Erasure;
using GhostLang.Core.Settings.Ocr;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Pipelines;

public class PipelineRegistry : IPipelineRegistry
{
    public IReadOnlyList<PipelineStepDescriptor> GetImagePipelineSteps()
    {
        return new List<PipelineStepDescriptor>
        {
            new()
            {
                StepId = "step.image.motion", Order = 1,
                NameKey = "Step_MotionDetection", DescriptionKey = "Step_MotionDetectionDesc",
                Name = "Motion Detection", Description = "Compares current frame with previous. If unchanged — pipeline is aborted.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.image.preprocess", Order = 2,
                NameKey = "Step_Preprocessing", DescriptionKey = "Step_PreprocessingDesc",
                Name = "Image Preprocessing", Description = "Applies filters before OCR to improve recognition quality.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.image.ocr", Order = 3,
                NameKey = "Step_Ocr", DescriptionKey = "Step_OcrDesc",
                Name = "Text Recognition", Description = "Extracts text and coordinates from image using OCR engine.",
                IsOptional = false,
                AvailableEngines = new List<EngineDescriptor>
                {
                    new() { EngineId = "engine.ocr.tesseract", Name = "Tesseract (Local)", OptionsType = typeof(TesseractOcrOptions) },
                    new() { EngineId = "engine.ocr.windows", Name = "Windows OCR (Built-in)", OptionsType = typeof(WindowsOcrOptions) },
                    new() { EngineId = "engine.ocr.azure", Name = "Azure AI Vision", OptionsType = typeof(AzureVisionOcrOptions) },
                    new() { EngineId = "engine.ocr.ocrspace", Name = "OCR.space (Cloud)", OptionsType = typeof(OcrSpaceOptions) }
                }
            },
            new()
            {
                StepId = "step.image.text_erasure", Order = 4,
                NameKey = "Step_Erasure", DescriptionKey = "Step_ErasureDesc",
                Name = "Text Erasure", Description = "Removes original text from image by filling area with background.",
                IsOptional = true,
                AvailableEngines = new List<EngineDescriptor>
                {
                    new() { Name = "Solid Color Fill (ImageSharp)", OptionsType = typeof(SolidColorErasureOptions) },
                    new() { Name = "Smart Inpainting (OpenCV)", OptionsType = typeof(OpenCvErasureOptions) }
                }
            },
            new()
            {
                StepId = "step.image.cachecheck", Order = 5,
                NameKey = "Step_Cache", DescriptionKey = "Step_CacheDesc",
                Name = "Translation Cache", Description = "Caches translated fragments in memory.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.image.glossary", Order = 6,
                NameKey = "Step_Glossary", DescriptionKey = "Step_GlossaryDesc",
                Name = "Glossary Tokenization", Description = "Replaces glossary terms with tokens to protect them from translation.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.image.translation", Order = 7,
                NameKey = "Step_Translation", DescriptionKey = "Step_TranslationDesc",
                Name = "Machine Translation", Description = "Sends recognized text to the selected translation engine.",
                IsOptional = false,
                AvailableEngines = new List<EngineDescriptor>
                {
                    new() { EngineId = "engine.translation.googleweb", Name = "GTranslate (Free)", OptionsType = typeof(GTranslateOptions) },
                    new() { EngineId = "engine.translation.mymemory", Name = "MyMemory API (Translated.net)", OptionsType = typeof(MyMemoryOptions) },
                    new() { EngineId = "engine.translation.lingva", Name = "Lingva Translate (Self-hosted)", OptionsType = typeof(LingvaOptions) },
                    new() { EngineId = "engine.translation.libretranslate", Name = "LibreTranslate (Self-hosted, CTranslate2)", OptionsType = typeof(LibreTranslateOptions) }
                }
            },
            new()
            {
                StepId = "step.image.glossary_restore", Order = 8,
                NameKey = "Step_GlossaryRestore", DescriptionKey = "Step_GlossaryRestoreDesc",
                Name = "Glossary Restoration", Description = "Replaces tokens back with glossary translations.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.image.text_rendering", Order = 9,
                NameKey = "Step_Rendering", DescriptionKey = "Step_RenderingDesc",
                Name = "Translation Rendering", Description = "Draws translated text over the cleaned image area.",
                IsOptional = false,
                AvailableEngines = new List<EngineDescriptor>
                {
                    new() { Name = "Built-in Renderer", OptionsType = typeof(TextRenderingOptions) }
                }
            }
        };
    }

    public IReadOnlyList<PipelineStepDescriptor> GetAudioPipelineSteps()
    {
        return new List<PipelineStepDescriptor>
        {
            new()
            {
                StepId = "step.audio.vad", Order = 1,
                NameKey = "Step_Audio_Vad", DescriptionKey = "Step_Audio_VadDesc",
                Name = "Voice Activity Detection", Description = "Detects silence in audio chunk and aborts pipeline if no speech is present.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.audio.preprocess", Order = 2,
                NameKey = "Step_Audio_Preprocess", DescriptionKey = "Step_Audio_PreprocessDesc",
                Name = "Audio Preprocessing", Description = "Resampling, normalization, and optional noise suppression before ASR.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.audio.asr", Order = 3,
                NameKey = "Step_Audio_Asr", DescriptionKey = "Step_Audio_AsrDesc",
                Name = "Speech Recognition", Description = "Converts audio to text fragments with timing and confidence.",
                IsOptional = false,
                AvailableEngines = new List<EngineDescriptor>
                {
                    new() { EngineId = "engine.asr.whisper", Name = "Whisper.net (Local)", OptionsType = typeof(WhisperAsrOptions) },
                    new() { EngineId = "engine.asr.vosk", Name = "Vosk (Local)", OptionsType = typeof(VoskAsrOptions) },
                    new() { EngineId = "engine.asr.azure", Name = "Azure AI Speech (Cloud)", OptionsType = typeof(AzureAsrOptions) }
                }
            },
            new()
            {
                StepId = "step.audio.cachecheck", Order = 4,
                NameKey = "Step_Audio_Cache", DescriptionKey = "Step_Audio_CacheDesc",
                Name = "Translation Cache", Description = "Resolves previously translated fragments from cache.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.audio.glossary", Order = 5,
                NameKey = "Step_Audio_Glossary", DescriptionKey = "Step_Audio_GlossaryDesc",
                Name = "Glossary Tokenization", Description = "Replaces glossary terms with tokens before translation.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.audio.translation", Order = 6,
                NameKey = "Step_Audio_Translation", DescriptionKey = "Step_Audio_TranslationDesc",
                Name = "Machine Translation", Description = "Translates recognized fragments via the selected translation engine.",
                IsOptional = false, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.audio.glossary_restore", Order = 7,
                NameKey = "Step_Audio_GlossaryRestore", DescriptionKey = "Step_Audio_GlossaryRestoreDesc",
                Name = "Glossary Restoration", Description = "Replaces tokens back with target-side glossary terms.",
                IsOptional = true, AvailableEngines = new List<EngineDescriptor>()
            },
            new()
            {
                StepId = "step.audio.subtitle_render", Order = 8,
                NameKey = "Step_Audio_Rendering", DescriptionKey = "Step_Audio_RenderingDesc",
                Name = "Subtitle Dispatch", Description = "Emits translated fragments to the subtitle overlay consumer.",
                IsOptional = false, AvailableEngines = new List<EngineDescriptor>()
            }
        };
    }
}
