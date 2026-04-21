<p align="center">
  <img src="src/GhostLang.WPF/Assets/logo-full.svg" alt="GhostLang" width="320" />
</p>

<p align="center">
  <strong>Переводите всё, что у вас на экране и в колонках - живьём, на 20 языков.</strong>
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8.0" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2B-0078D4" alt="Windows 10+" />
  <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License" />
  <img src="https://img.shields.io/badge/status-beta-orange" alt="Beta" />
  <a href="README.md"><img src="https://img.shields.io/badge/lang-en%20%7C%20ru-blue" alt="EN | RU" /></a>
</p>

---

GhostLang - приложение для перевода экрана и аудиопотока вашей системы. Программа даёт обширный функционал для транскрибации и перевода цифрового контента: манга, интерфейс игр, статьи, подкасты, YouTube, стримы - всё, с чем вы обычно сталкиваетесь за компьютером.

Вы выделяете область экрана мышкой или включаете захват системного звука - приложение накладывает перевод поверх оригинала на экране или показывает субтитры на рабочем столе для аудио. Ваши данные не попадают на чужие сервера, если вы сами не включили облачный движок.

## Что отличает GhostLang

Большинство десктоп-переводчиков ограничиваются экранным OCR. Здесь экран и звук - равноправные. Они могут работать поотдельности и при этом у них общий кеш переводов, общий глоссарий с заданными терминами, общий бенчмарк-harness. Переводите стрим - реплики и UI-tooltip'ы идут через один и тот же pipeline.

Ещё одна штука, которой нет у аналогов - **встроенный benchmark UI**: ablation-пресеты, разбивка latency по шагам, метрики CER/WER/BLEU/chrF, экспорт в CSV - всё во вкладках Debug и Banchmark.

Несколько вещей, на которые обращают внимание:

- **Работает полностью офлайн**, если хотите. Tesseract + Whisper + локальный MT-движок - и готово. Облачные движки (Azure Vision, Azure Speech) подключаются опционально.
- **Ускорение ASR через GPU** - Whisper на Vulkan. Поддерживает NVIDIA, AMD, Intel - любую видеокарту с нормальным драйвером. Обычно в 3–10 раз быстрее CPU.
- **Адаптивный захват.** Читаете мангу - screen pipeline уходит в idle на 0.5 FPS. Смотрите стрим - частота прыгает до 5 FPS, как только что-то меняется.

## Возможности

### Перевод экрана

- Выделяете прямоугольную область мышкой. Overlay - click-through, то есть не мешает работать с окном под ним.
- OCR на выбор: **Tesseract** (локально, 20 языков), **Windows OCR**, **Azure Vision**, **OCR.space**.
- Умное стирание оригинального текста через **OpenCV inpainting**, либо быстрый заливка однотонным цветом, если хотите сэкономить CPU.
- Переведённый текст рендерится на месте оригинала - цвет и размер примерно совпадают.
- Частота захвата подстраивается под контент (200–5000 мс), настраивается на сессию.
- Recording mode прячет overlay от OBS и других recorder'ов - стрим выглядит чисто.

### Перевод аудио

- Источник: микрофон или системный loopback (WASAPI).
- VAD: простой RMS-gate или нейросеть **Silero** (200 МБ, заметно лучше отбрасывает шум).
- ASR: **Whisper** (локально, 5 размеров модели, GPU через Vulkan), **Vosk** (локально, streaming), **Azure Speech** (облако).
- Субтитры на любом мониторе, в любом углу, с fade-in/out. Прячутся по хоткею.
- Drift-индикатор предупредит, если перевод начал отставать от реального времени.

### Общее

- 20 языков: английский, русский, испанский, немецкий, французский, японский, китайский (упрощённый / традиционный), итальянский, португальский, польский, корейский, арабский, турецкий, украинский, нидерландский, вьетнамский, хинди, тайский, иврит.
- Движки перевода: **GTranslate** (Google, Yandex, Bing, Microsoft), **MyMemory**, **Lingva**, **LibreTranslate**.
- Кеш переводов лежит на диске и переживает перезапуск.
- Глоссарий: отмечаете термины, которые не нужно переводить (названия продуктов, имена персонажей, жаргон) - они проходят мимо MT.
- Global hotkeys на всё: выбор региона, старт/стоп, toggle субтитров, перемещение окна.
- UI на русском или английском. Тема: тёмная или светлая.

## Скриншоты

> Скриншоты будут в следующем релизе.

## Требования

- Windows 10 build 19041 или новее.
- .NET 8.0 Desktop Runtime (прилагается к release-сборке).
- Для GPU Whisper - видеокарта с Vulkan-драйвером. В Windows 10+ он обычно уже стоит.

## Установка

### Из исходников

```bash
git clone https://github.com/dZatr1k/GhostLang.git
cd GhostLang/src
dotnet build GhostLang.sln -c Release
dotnet run --project GhostLang.WPF/GhostLang.WPF.csproj -c Release
```

### Готовая сборка

Качайте свежий `GhostLang-v*-win-x64.zip` со страницы [Releases](https://github.com/dZatr1k/GhostLang/releases), распаковываете, запускаете `GhostLang.WPF.exe`.

## Конфигурация

Настройки лежат в `appsettings.user.json` рядом с exe (создаётся при первом запуске). Страница Settings сохраняет изменения автоматически через 400 мс debounce - кнопки «Save» нет.

Модели живут здесь:

```
<exe>/Models/
├── Tesseract/           - языковые паки, скачиваются по запросу из Settings
├── Whisper/             - ggml-модели (base/small/medium/large-v3)
├── Vosk/                - распакуйте архив сюда, приложение само найдёт
└── Silero/              - silero_vad.onnx, скачивание из Settings
```

## Бенчмарки (v0.1 beta)

Замеры на репрезентативном корпусе из 3 screen-семплов (манга JA→RU, game UI EN→RU, сайт EN→RU):

| Метрика | До оптимизаций | v0.1 | Ускорение |
|---------|----------------|------|-----------|
| Средняя latency | 1371 мс | **418 мс** | **3.3×** |
| Манга-панель | 2418 мс | 762 мс | 3.2× |
| Game UI | 798 мс | 129 мс | 6.2× |
| Сайт | 897 мс | 364 мс | 2.5× |

Полный корпус (60 семплов, 3 категории × 2 пайплайна) сейчас собирается и войдёт в v0.2. Чтобы воспроизвести цифры у себя - **Debug → Benchmark → Ablation**.

## Коротко об архитектуре

Два проекта:

- **GhostLang.Core** - пайплайны, абстракции движков, benchmark runner. Framework-agnostic (`net8.0-windows`).
- **GhostLang.WPF** - UI, MVVM через CommunityToolkit.Mvvm, DI через Microsoft.Extensions.Hosting.

Шаги пайплайна реализуют `IMandatoryPipelineStep` или `IOptionalPipelineStep`, выполняются по порядку через `IPipelineBuilder`. Пайплайн строится один раз на сессию и живёт до Stop'а - тяжёлые нативные ресурсы (handle Tesseract, WhisperFactory, модель Vosk) кешируются, а не пересоздаются на каждом кадре.

## Сравнение с аналогами

| | GhostLang | QTranslate | ScreenTranslator | LunaTranslator | Translumo | MORT |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Перевод экрана | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Перевод аудио | ✅ | ❌ | ❌ | частично (text hook) | ❌ | ❌ |
| Локальный ASR (Whisper) | ✅ | - | - | - | - | - |
| Ускорение через GPU | ✅ | ❌ | ❌ | частично | ❌ | ❌ |
| Адаптивная частота захвата | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Нейросетевой VAD | ✅ | - | - | - | - | - |
| Персистентный кеш | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Встроенный бенчмарк | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

## Что под капотом

| Назначение | Пакет |
|------------|-------|
| Перевод | GTranslate, Azure.AI.Vision.ImageAnalysis |
| OCR | Tesseract 5.x, Windows.Media.Ocr |
| ASR | Whisper.net (+ Vulkan runtime), Vosk, Microsoft.CognitiveServices.Speech |
| Inpainting | OpenCvSharp4 |
| Рендер изображений | SixLabors.ImageSharp |
| Нейросетевой VAD | Microsoft.ML.OnnxRuntime + Silero VAD v5 |
| Захват аудио | NAudio (WaveInEvent, WasapiLoopbackCapture) |
| UI | WPF, HandyControl, CommunityToolkit.Mvvm |
| DI / Hosting | Microsoft.Extensions.Hosting |

## Roadmap

В v0.1 - основной пайплайн и бенчмарк-harness. Дальше по плану:

- Редизайн UI согласно внутренней спецификации.
- Опциональный CUDA runtime для Whisper (около 1 ГБ дополнительной установки).
- Streaming ASR с partial-субтитрами, которые обновляются по слову.
- Переносимый формат кеша, чтобы можно было перетаскивать историю переводов между машинами.
- Порты на Linux и macOS на non-WPF UI-стеке.

## Лицензия

MIT. См. [LICENSE](LICENSE).

## Автор

Затримайлов Дании (dZatr1k) - выпускная квалификационная работа, ЛЭТИ, 2026.
