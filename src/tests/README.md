# GhostLang test samples

Тестовые наборы для benchmark-харнесса (`Debug → Metrics → Benchmark`). Используются для объективного измерения CER/WER/BLEU/chrF/latency до и после правок pipeline'ов.

## Структура

```
tests/
├── samples/
│   ├── screen/
│   │   ├── 01-{name}/
│   │   │   ├── meta.json         (REQUIRED)
│   │   │   ├── *.png             (REQUIRED — screenshot; "region.png" preferred, any .png works)
│   │   │   └── expected.txt      (REQUIRED — ground-truth OCR text)
│   │   ├── 02-{name}/
│   │   └── ...
│   └── audio/
│       ├── 01-{name}/
│       │   ├── meta.json         (REQUIRED)
│       │   ├── *.wav|*.mp3|*.m4a|*.flac|*.ogg  (REQUIRED — "audio.wav" preferred)
│       │   └── expected.txt      (REQUIRED — ground-truth transcript)
│       └── ...
└── results/
    ├── screen-baseline.json
    └── audio-baseline.json
```

**Имена файлов:** runner сначала ищет `region.png` / `audio.wav`, а если не находит — берёт **любой** `.png` / `.wav`/`.mp3`/`.m4a`/`.flac`/`.ogg` в папке. То есть `my-screenshot.png` или `terraria-dialog.png` тоже подойдут — не обязательно переименовывать.

## Формат `meta.json`

```json
{
  "name": "Manga page in Japanese",
  "source_langs": ["Japanese"],
  "target_lang": "Russian",
  "description": "One-shot manga speech bubble with character dialog",
  "tags": ["cjk", "vertical-text", "speech-bubble"]
}
```

- **`name`** — человекочитаемое название, отображается в benchmark UI.
- **`source_langs`** — список (для совместимости; **используется только первый** — у нас single-source policy). Имя enum: `English`, `Russian`, `Japanese`, etc.
- **`target_lang`** — целевой язык перевода.
- **`description`** — описание сценария (для документации).
- **`tags`** — массив строк, для группировки/фильтрации (необязательно).

## Что нужно собрать

### Screen — 10 samples (приоритет высокий)

| # | Категория | Что нужно | Откуда взять |
|---|---|---|---|
| 01 | Manga | Speech bubble, JA → RU, чёрный текст на белом, ~400×300 | Скрин из открытой манги, например MangaDex |
| 02 | Game UI | Диалог из игры, EN → RU, орнаментальный шрифт, тёмный фон | Скрин из старой RPG (Steam screenshots tab) |
| 03 | Subtitle | Полоса субтитров YouTube, EN → RU, ~1000×100 | Pause + screenshot YouTube video с CC |
| 04 | Website | Параграф из новостной статьи, RU → EN, ~800×600 | Скрин с lenta.ru / RBC |
| 05 | Chat | Сообщение Discord/Slack, RU → EN, ~500×400 | Свой Discord |
| 06 | Photo | Фото вывески / уличного знака на JA → EN | Туристические сайты, Wikipedia commons |
| 07 | Whiteboard | Рукописный текст на доске (challenge), EN → RU | Учебный материал |
| 08 | Code | Исходный код + комментарии EN → RU | GitHub любого open-source проекта |
| 09 | Manga complex | Многострочные пузыри JA → RU, 1000×1500 | Та же манга, page с >3 bubbles |
| 10 | Gradient bg | Белый текст на градиенте, EN → RU | Презентация/баннер с текстом |

### Audio — 8 samples (приоритет средний)

| # | Категория | Что нужно | Длина | Откуда взять |
|---|---|---|---|---|
| 01 | Lecture EN | Чистая лекция, ясная речь | 30s | YouTube lecture (TED/MIT/Stanford) |
| 02 | Dialogue RU | Разговор двух человек | 20s | Подкаст или собственная запись |
| 03 | Noisy mic EN | Микрофон + фоновый шум | 15s | Запись с голосом + кондиционером |
| 04 | Game cutscene JA→RU | Диалог из игры (loopback) | 30s | Скрин-запись игры с японской озвучкой |
| 05 | Stream EN | Стример/подкаст, casual | 60s | Любой Twitch/YouTube cast |
| 06 | Music+voice EN | Голос поверх фоновой музыки | 30s | Радио / vlog |
| 07 | Mixed langs EN+RU | Code-switching | 20s | Tech-блог с английскими терминами |
| 08 | Fast speech RU | Быстрая речь (ведущий новостей) | 15s | Россия 1 / РБК |

## Как создавать sample

### Screen sample

1. Сделай скриншот **только** нужной области (Win+Shift+S → выделить → Ctrl+S как PNG).
2. Сохрани в `tests/samples/screen/{NN-name}/region.png`.
3. Создай `expected.txt` — **ровно то**, что должен распознать OCR. **Без** перевода. Каждый видимый текстовый блок — на новой строке. Например:
   ```
   こんにちは
   元気ですか？
   ```
4. Создай `meta.json` (см. выше).

### Audio sample

1. Запиши/обрежь аудио до нужной длины (например, через **Audacity**: open → Edit → Trim → Export as WAV).
2. Параметры WAV (рекомендуется): **16kHz**, **mono**, **16-bit PCM**. Если другой формат — приложение всё равно конвертирует, но с потерей качества.
3. Сохрани как `tests/samples/audio/{NN-name}/audio.wav`.
4. Создай `expected.txt` — **точная транскрипция** того, что говорят. **Без** перевода. Знаки препинания — как услышишь.
5. Создай `meta.json`.

## Ограничения и tips

- **Размер.** Для начала 10+8 samples ≈ 20MB. Если набор разрастётся >100MB — мигрируем на Git LFS.
- **Авторские права.** Используй только public domain / Creative Commons / собственный контент. Для манги — открытые публикации MangaDex; для аудио — Creative Commons на YouTube.
- **Качество reference.** Чем точнее `expected.txt`, тем валиднее CER/WER. Для аудио — лучше использовать самостоятельную транскрипцию или официальные субтитры.
- **Разнообразие.** Хорошо когда есть лёгкие (clean text) и сложные (gradient/handwriting/noisy) — это даёт реальную картину устойчивости.

## Как использовать

1. Скопировать собранные sample-папки в `tests/samples/screen/` или `audio/`.
2. Запустить приложение → Debug → Screen tab → Metrics → раскрыть Expander **Benchmark**.
3. Выбрать папку `tests/samples/` → нажать **Run Screen** (или **Run Audio** для аудио-таба).
4. После завершения — **Export JSON** → сохранить в `tests/results/screen-{date}.json`.
5. Сравнить с `tests/results/screen-baseline.json` (после первой baseline run в B-4).

## Пример структуры sample (скопируй и адаптируй)

См. `tests/samples/screen/01-example-en/` — placeholder с meta.json + expected.txt. Замени на свои реальные данные:
- Положи `region.png` (скриншот области).
- Обнови `expected.txt` чтобы совпадал с текстом на твоей картинке.
- Обнови `meta.json` если нужно (langs, name).

После добавления своих samples — example можно удалить.
