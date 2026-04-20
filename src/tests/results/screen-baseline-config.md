# Screen baseline — config snapshot

**Date:** 2026-04-20
**Run:** `screen-baseline.json`

## Pipeline config
- **Motion detection:** default (enabled)
- **Image preprocess:** всё off (никаких фильтров)
- **OCR engine:** Tesseract, ModelType = Fast
- **Tesseract models:** EN, JA, RU (Fast)
- **Text erasure:** SolidColor #000000
- **Translation cache check:** default (enabled)
- **Glossary tokenization:** off
- **Translation engine:** GTranslate, provider = Google
- **Glossary restoration:** off
- **Text rendering:** default (TextRenderingOptions defaults)

## Environment
- OS: Windows 11
- .NET: 8.0
- Build: Debug

## Samples
1. `01-manga-jp` — One Punch Man vol 36 page, JP → RU
2. `02-game-en` — Terraria NPC dialog, EN → RU
3. `03-site-ru` — Russian landing page article, RU → EN

## Results summary
- Passed: 3/3
- Failed: 0
- Avg CER: 50.3% (misleading — one failure drags avg; see per-sample)
- Avg WER: 141.5%
- Avg BLEU: 0.32
- Avg chrF: 0.52
- Avg latency: 1371 ms

## Per-sample notes
| Sample | CER | chrF | Latency | Note |
|---|---|---|---|---|
| manga-jp | 117% | 0.09 | 2418 ms | Tesseract fails on vertical CJK layout; panels read out of order |
| game-en | 30% | 0.55 | 798 ms | Small UI labels mis-read (digits vs letters, menu items corrupted) |
| site-ru | 3.6% | 0.94 | 897 ms | Near-perfect for horizontal Russian text |

## Run history

### 2026-04-20 run v2 (`screen-baseline-v2.json`)
After P0-S5 (min bounds 5→3), P0-S7 (glossary leak fix), P0-S6 (descender padding + overlay crop), P0-S4 (Unicode punct filter).

| Sample | CER | chrF | Latency | Δ latency | Note |
|---|---|---|---|---|---|
| manga-jp | 118% | 0.087 | 2622 ms | +204 ms | OCR text slightly different (more fragments passed filter) |
| game-en | 30% | 0.55 | 780 ms | −18 ms | ≈ same |
| site-ru | 3.6% | 0.94 | 1200 ms | +303 ms | Erasure padding made patches larger |

- **CER/chrF/BLEU:** flat (expected — recent fixes were visual/defensive, not accuracy-moving).
- **Latency:** +12% avg, mostly from TextErasureStep (larger padded area to inpaint / encode PNG).
- **Visual wins** (not in benchmark): descenders preserved, overlay patches don't overlap.
