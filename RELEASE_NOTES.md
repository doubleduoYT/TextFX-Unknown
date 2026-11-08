# TextFX Unknown v0.7.3 Preview 1 — Dialogue JSON FX Converter

This release adds an opt-in tool to apply TextFX's **human-curated dialogue effects directly to the user's local original C:U 7.0 language files**, rather than only matching text at runtime.

## Included
- `source/apply_effects_to_locale.py`: generates a **new** UTF-8 JSON whose character dialogue strings contain TextFX animation tags; accepts EN.json and ko-KR.json and supports `--dry-run`.
- Mapping is read from `source/TextFXUnknown.Curated.cs`, which contains 177 event groups, 194 specific line overrides and 61 unique handpicked effects.
- Preserves literal dialogue, rich text, and all unrelated locale content. Idempotent and refuses overwrite of the source.
- CI automated tests exercise the full event index using synthetic strings, verify valid JSON output and ensure no duplicate tags when run twice.
- The compiled DLL has no functional renderer changes beyond version 0.7.3; retains v0.7.2's tighter giant-glyph safety checks, F7 emergency suspension and F8 diagnostic.
- Release archive includes DLL, source, conversion tool and tests **but no original dialogue files or README**.

## Usage after extracting the ZIP

```bash
python source/apply_effects_to_locale.py ko-KR.json -o ko-KR.TextFX.json
python source/apply_effects_to_locale.py EN.json -o EN.TextFX.json
```

Replace only the locale JSON used by your translation plugin **after backing it up**, and use the filename it expects. The `*.TextFX.json` output is not auto-loaded by name. Do not install raw animation tags without the corresponding TextFX Unknown DLL.

The direct converter is tested in CI with synthetic source data, not actual in-game visual rendering. This is an unofficial fan-made Windows Mono/BepInEx 5 mod for the 7.0 demo.