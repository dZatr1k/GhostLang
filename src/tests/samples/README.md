# Benchmark samples

This folder is the benchmark corpus root. It's intentionally empty in the v0.1 beta release — the corpus is under active construction.

## Target corpus (v0.2)

60 samples total, 3 categories × 10 per pipeline:

- `screen/` — 30 samples
  - Manga JA→RU (10)
  - Game UI EN→RU (10)
  - Website article EN→RU (10)
- `audio/` — 30 samples
  - Podcast / Lecture EN→RU (10)
  - Game stream EN→RU (10)
  - Music + vocals EN→RU (10)

Detailed type list, source recommendations, and preparation workflow: [`docs/sample-collection-guide.md`](../../docs/sample-collection-guide.md) (repository-local, not in releases).

## Format

```
{pipeline}/{NN-category-lang-NN-slug}/
├── region.png  OR  audio.wav
├── expected.txt
└── meta.json
```

See `meta.json` template in the collection guide.

## Running the benchmark

1. Drop sample folders into `screen/` or `audio/`.
2. Open the app → Debug → Benchmark.
3. Point "Samples folder" to this directory.
4. Click **Warm up** → then **Run Screen** / **Run Audio** / **Ablation batch**.
5. **Export CSV** / **Export batch JSONs** from the toolbar.

Results are saved under `../results/`.
