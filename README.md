<p align="center">
  <img src="src/GhostLang.WPF/Assets/logo-full.svg" alt="GhostLang" width="320" />
</p>

<p align="center">
  <strong>Real-time multimodal streaming translator for screen and audio.</strong>
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8.0" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2B-0078D4" alt="Windows 10+" />
  <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License" />
  <img src="https://img.shields.io/badge/status-beta-orange" alt="Beta" />
  <a href="README.ru.md"><img src="https://img.shields.io/badge/lang-en%20%7C%20ru-blue" alt="EN | RU" /></a>
</p>

---

GhostLang captures a region of your screen or a stream of audio, recognizes the text / speech in it, translates to your target language, and renders the result back in place - an overlay on the captured region for screen, or subtitles on top of the desktop for audio. Built as a **pipeline of interchangeable steps** so every stage (OCR, ASR, translation engine, erasure, voice-activity detection) is replaceable, benchmarkable, and configurable independently.

## Why GhostLang

- **Multimodal.** Almost every desktop translator handles *either* screen *or* audio. GhostLang streams both in parallel with a shared translation cache and glossary.
- **Local-first option.** Run fully offline with Tesseract OCR + Whisper ASR + local translation providers. Cloud engines (Azure Vision, Azure Speech) are optional plug-ins.
- **Benchmark harness built in.** Ablation presets, per-step latency breakdown, CER/WER/BLEU/chrF metrics, CSV export - shipped in the Debug tab.
- **GPU-accelerated ASR.** Whisper with Vulkan runtime - ~3–10× faster than CPU on any modern GPU (NVIDIA / AMD / Intel).
- **Adaptive capture.** Screen pipeline slows to 0.5 FPS on static content, speeds to 5 FPS on active content - matches user intent, saves CPU.

## Features

### Screen translation
- Region-based screen capture with click-through overlay that doesn't interfere with the captured window.
- OCR engines: **Tesseract** (local, 20 languages), **Windows OCR**, **Azure Vision**, **OCR.space**.
- Smart text erasure via **OpenCV inpainting** - or fast solid-color erase.
- Translated text rendered in the original location, preserving color and approximate size.
- Adaptive capture rate (200–5000 ms), configurable per-session.
- Recording-mode overlay-exclusion - overlay stays invisible to OBS/screen recorders.

### Audio translation
- Capture source: microphone or system loopback (WASAPI).
- VAD: RMS gate (fast) or **Silero** (neural, ~200MB model, better noise rejection).
- ASR engines: **Whisper** (local, 5 model sizes, GPU via Vulkan) - **Vosk** (local, streaming) - **Azure Speech** (cloud).
- Subtitle overlay with configurable position, monitor, duration, fade-in/out.
- Drift indicator - warns when translation lags real-time capture.

### Shared
- 20 supported languages: English, Russian, Spanish, German, French, Japanese, Chinese (Simplified/Traditional), Italian, Portuguese, Polish, Korean, Arabic, Turkish, Ukrainian, Dutch, Vietnamese, Hindi, Thai, Hebrew.
- Translation engines: **GTranslate** (Google, Yandex, Bing, Microsoft), **MyMemory**, **Lingva**, **LibreTranslate**.
- Persistent translation cache (disk-backed, TTL-configurable).
- Glossary: user-defined term substitutions protected from MT.
- Global hotkeys for region selection, start/stop, subtitle toggle, window move/resize.
- UI language: English or Russian.
- Dark / Light theme.

## Screenshots

> Screenshots will be added in a follow-up release.

## Requirements

- Windows 10 build 19041 or newer.
- .NET 8.0 Desktop Runtime (installer bundles it for release builds).
- For GPU Whisper: Vulkan-capable GPU driver (Windows 10+ default drivers usually suffice).

## Installation

### From source

```bash
git clone https://github.com/dZatr1k/GhostLang.git
cd GhostLang/src
dotnet build GhostLang.sln -c Release
dotnet run --project GhostLang.WPF/GhostLang.WPF.csproj -c Release
```

### Prebuilt (once released)

Download the latest `GhostLang-v*-win-x64.zip` from [Releases](https://github.com/dZatr1k/GhostLang/releases), extract, run `GhostLang.WPF.exe`.

## Configuration

All settings are stored in `appsettings.user.json` next to the executable (auto-created on first launch). Autosave is enabled - the Settings page writes changes on a 400 ms debounce.

Model downloads live under:
```
<exe>/Models/
├── Tesseract/           - language packs, auto-download on request
├── Whisper/             - ggml models (base/small/medium/large-v3)
├── Vosk/                - manually unpack archives here, app auto-discovers
└── Silero/              - silero_vad.onnx, download from Settings
```

## Benchmarks (v0.1 beta)

Latency measurements on a representative 3-sample screen corpus (manga JA→RU, game UI EN→RU, website EN→RU):

| Metric | v1 baseline | v3 (latest) | Speedup |
|--------|-------------|-------------|---------|
| Avg total latency | 1371 ms | **418 ms** | **3.3×** |
| Manga panel | 2418 ms | 762 ms | 3.2× |
| Game UI | 798 ms | 129 ms | 6.2× |
| Website | 897 ms | 364 ms | 2.5× |

Full benchmark corpus (60 samples, 3 categories × 2 pipelines) and ablation data are attached to each release as `tests-*.zip`. Reproduce locally via **Debug → Benchmark → Ablation**.

## Architecture

Two framework-agnostic projects:
- **GhostLang.Core** - pipelines, engine abstractions, benchmark runner (targets `net8.0-windows`).
- **GhostLang.WPF** - WPF UI, MVVM (CommunityToolkit.Mvvm), DI via Microsoft.Extensions.Hosting.

Pipeline steps implement `IMandatoryPipelineStep` or `IOptionalPipelineStep` and run sequentially via `IPipelineBuilder`. Pipelines are singleton-per-session and cache expensive native resources (Tesseract engine handle, Whisper factory, Vosk model) across ticks - tear-down happens only on Stop or engine swap.

## Comparison to alternatives

| | GhostLang | QTranslate | ScreenTranslator | LunaTranslator | Translumo | MORT |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Screen translation | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Audio translation | ✅ | ❌ | ❌ | partial (text hook) | ❌ | ❌ |
| Local ASR (Whisper) | ✅ | - | - | - | - | - |
| GPU acceleration | ✅ | ❌ | ❌ | partial | ❌ | ❌ |
| Adaptive FPS | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Neural VAD | ✅ | - | - | - | - | - |
| Persistent cache | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Ablation benchmark | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

## Tech stack

| Purpose | Package |
|---------|---------|
| Translation | GTranslate, Azure.AI.Vision.ImageAnalysis |
| OCR | Tesseract 5.x, Windows.Media.Ocr |
| ASR | Whisper.net (+Vulkan runtime), Vosk, Microsoft.CognitiveServices.Speech |
| Image inpainting | OpenCvSharp4 |
| Image rendering | SixLabors.ImageSharp |
| Neural VAD | Microsoft.ML.OnnxRuntime + Silero VAD v5 |
| Audio capture | NAudio (WaveInEvent, WasapiLoopbackCapture) |
| UI | WPF, HandyControl, CommunityToolkit.Mvvm |
| DI / Hosting | Microsoft.Extensions.Hosting |

## Roadmap

Beta (v0.1) covers the core pipeline and benchmark harness. Planned for subsequent releases:
- Complete UI redesign from spec.
- CUDA runtime for Whisper (optional, +1 GB install).
- Streaming ASR with partial-result subtitles.
- Cache file portability across machines.
- Linux / macOS ports (non-WPF UI).

## License

MIT. See [LICENSE](LICENSE) once added.

## Author

dZatr1k - diploma thesis project, MIREA, 2026.
