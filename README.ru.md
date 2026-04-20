<p align="center">
  <img src="src/GhostLang.WPF/Assets/logo-full.svg" alt="GhostLang" width="320" />
</p>

<p align="center">
  <strong>Мультимодальный стриминговый переводчик экрана и звука в реальном времени.</strong>
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8.0" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2B-0078D4" alt="Windows 10+" />
  <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License" />
  <img src="https://img.shields.io/badge/status-beta-orange" alt="Beta" />
  <a href="README.md"><img src="https://img.shields.io/badge/lang-en%20%7C%20ru-blue" alt="EN | RU" /></a>
</p>

---

GhostLang захватывает регион экрана или поток аудио, распознаёт в нём текст / речь, переводит на нужный язык и отображает результат на том же месте — overlay поверх захваченного региона для экрана или субтитры на рабочем столе для звука. Построен как **pipeline из взаимозаменяемых шагов**: каждый этап (OCR, ASR, translation engine, стирание текста, VAD) подменяем, замеряем и конфигурируется независимо.

## Почему GhostLang

- **Мультимодальность.** Почти все десктоп-переводчики работают *или* с экраном, *или* со звуком. GhostLang стримит оба параллельно с общим кешем переводов и глоссарием.
- **Local-first.** Можно полностью offline: Tesseract + Whisper + локальный MT-провайдер. Облачные engine'ы (Azure Vision, Azure Speech) — опциональный плагин.
- **Встроенный бенчмарк.** Ablation-пресеты, разбивка latency по шагам, метрики CER/WER/BLEU/chrF, CSV-экспорт — в табе Debug.
- **GPU-ускорение ASR.** Whisper с Vulkan runtime — в ~3–10× быстрее CPU на любой современной видеокарте (NVIDIA / AMD / Intel).
- **Адаптивный захват.** Screen pipeline замедляется до 0.5 FPS на статичном контенте, ускоряется до 5 FPS при активном — экономит CPU, подстраивается под пользователя.

## Возможности

### Перевод экрана
- Захват региона с click-through overlay — не мешает работать с захватываемым окном.
- OCR engines: **Tesseract** (локально, 20 языков), **Windows OCR**, **Azure Vision**, **OCR.space**.
- Умное стирание текста через **OpenCV inpainting** — или быстрый SolidColor.
- Переведённый текст рендерится на месте оригинала с сохранением цвета и размера.
- Адаптивная частота захвата (200–5000 мс), настраивается на сессию.
- Recording-mode: overlay исключается из захвата OBS / других recorder'ов.

### Перевод аудио
- Источник: микрофон или system loopback (WASAPI).
- VAD: RMS-gate (быстрый) или **Silero** (нейросеть, ~200 MB модель, лучше отбрасывает шум).
- ASR engines: **Whisper** (локально, 5 размеров модели, Vulkan GPU) — **Vosk** (локально, streaming) — **Azure Speech** (облако).
- Overlay субтитров: настраиваемые позиция, монитор, длительность, fade-in/out.
- Drift-индикатор — предупреждает, если перевод отстаёт от реального времени.

### Общее
- 20 поддерживаемых языков: английский, русский, испанский, немецкий, французский, японский, китайский (упрощённый/традиционный), итальянский, португальский, польский, корейский, арабский, турецкий, украинский, нидерландский, вьетнамский, хинди, тайский, иврит.
- Translation engines: **GTranslate** (Google, Yandex, Bing, Microsoft), **MyMemory**, **Lingva**, **LibreTranslate**.
- Персистентный кеш переводов (на диске, TTL настраивается).
- Глоссарий: пользовательские замены терминов, защищённые от MT.
- Global hotkeys для выбора региона, старт/стоп, toggle субтитров, перемещения окна.
- UI: русский или английский.
- Тема: тёмная / светлая.

## Скриншоты

> Скриншоты добавятся в следующем релизе.

## Требования

- Windows 10 build 19041 или новее.
- .NET 8.0 Desktop Runtime (для release-сборок вложен в инсталлятор).
- Для GPU Whisper: драйвер с поддержкой Vulkan (в Windows 10+ обычно есть по умолчанию).

## Установка

### Из исходников

```bash
git clone https://github.com/dZatr1k/GhostLang.git
cd GhostLang/src
dotnet build GhostLang.sln -c Release
dotnet run --project GhostLang.WPF/GhostLang.WPF.csproj -c Release
```

### Готовая сборка (после первого релиза)

Скачать свежий `GhostLang-v*-win-x64.zip` со страницы [Releases](https://github.com/dZatr1k/GhostLang/releases), распаковать, запустить `GhostLang.WPF.exe`.

## Конфигурация

Все настройки в `appsettings.user.json` рядом с исполняемым файлом (создаётся при первом запуске). Autosave включён — Settings-страница сохраняет изменения через 400 мс debounce.

Модели лежат здесь:
```
<exe>/Models/
├── Tesseract/           — языковые паки, auto-download при запросе
├── Whisper/             — ggml модели (base/small/medium/large-v3)
├── Vosk/                — архивы распаковать сюда, app автоматически их найдёт
└── Silero/              — silero_vad.onnx, скачивание из Settings
```

## Бенчмарки (v0.1 beta)

Latency на репрезентативном screen-корпусе из 3 семплов (manga JA→RU, game UI EN→RU, website EN→RU):

| Метрика | v1 baseline | v3 (latest) | Ускорение |
|---------|-------------|-------------|-----------|
| Средняя total latency | 1371 мс | **418 мс** | **3.3×** |
| Манга-панель | 2418 мс | 762 мс | 3.2× |
| Game UI | 798 мс | 129 мс | 6.2× |
| Сайт | 897 мс | 364 мс | 2.5× |

Полный корпус (60 семплов, 3 категории × 2 пайплайна) и ablation-данные прикладываются к каждому релизу как `tests-*.zip`. Воспроизвести локально — **Debug → Benchmark → Ablation**.

## Архитектура

Два framework-agnostic проекта:
- **GhostLang.Core** — pipelines, engine abstractions, benchmark runner (таргетит `net8.0-windows`).
- **GhostLang.WPF** — WPF UI, MVVM (CommunityToolkit.Mvvm), DI через Microsoft.Extensions.Hosting.

Шаги pipeline реализуют `IMandatoryPipelineStep` или `IOptionalPipelineStep` и выполняются последовательно через `IPipelineBuilder`. Pipeline — singleton на сессию, кеширует тяжёлые native-ресурсы (Tesseract engine handle, Whisper factory, Vosk model) между тиками — teardown только на Stop или смене engine.

## Сравнение с аналогами

| | GhostLang | QTranslate | ScreenTranslator | LunaTranslator | Translumo | MORT |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Перевод экрана | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Перевод аудио | ✅ | ❌ | ❌ | частично (text hook) | ❌ | ❌ |
| Локальный ASR (Whisper) | ✅ | — | — | — | — | — |
| GPU-ускорение | ✅ | ❌ | ❌ | частично | ❌ | ❌ |
| Адаптивный FPS | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Нейросетевой VAD | ✅ | — | — | — | — | — |
| Персистентный кеш | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Встроенный ablation-бенчмарк | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

## Технологический стек

| Назначение | Пакет |
|------------|-------|
| Перевод | GTranslate, Azure.AI.Vision.ImageAnalysis |
| OCR | Tesseract 5.x, Windows.Media.Ocr |
| ASR | Whisper.net (+Vulkan runtime), Vosk, Microsoft.CognitiveServices.Speech |
| Inpainting | OpenCvSharp4 |
| Рендер изображений | SixLabors.ImageSharp |
| Нейросетевой VAD | Microsoft.ML.OnnxRuntime + Silero VAD v5 |
| Захват аудио | NAudio (WaveInEvent, WasapiLoopbackCapture) |
| UI | WPF, HandyControl, CommunityToolkit.Mvvm |
| DI / Hosting | Microsoft.Extensions.Hosting |

## Roadmap

Beta (v0.1) покрывает базовый pipeline и бенчмарк-harness. Запланировано на следующие релизы:
- Полный редизайн UI по спецификации.
- CUDA runtime для Whisper (опционально, +1 GB установка).
- Streaming ASR с partial-result субтитрами.
- Переносимость cache-файла между машинами.
- Порты на Linux / macOS (не-WPF UI).

## Лицензия

MIT. См. [LICENSE](LICENSE) (будет добавлен).

## Автор

dZatr1k — выпускная квалификационная работа, МИРЭА, 2026.