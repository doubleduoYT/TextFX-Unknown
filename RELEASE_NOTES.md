# TextFX Unknown v0.6.0 Preview 1

This release introduces **hand-curated text effects using real C:U dialogue** instead of broadly attaching Wave to every line.

## Dialogue curation
- Reviewed the supplied demo's official English language file and aligned Korean JSON, which have the same character/event/index structure.
- **177 situation groups** spanning **1,895 character speech lines**: Experiment 1,604; Milky 146; Dune 145.
- **194 individual line-specific overrides** for exceptions, plus **61 manually authored animation presets**.
- Dialogue groups include new area, food, pain, panic, wounds, fatigue, grief, recovery, friendly traders, hostile traders and Dune's unusual speaking patterns.
- Multiple types of movement — fade, entrance bounce, tremor, short impact, tinted color, occasional wave and glitch — instead of defaulting every line to a wave.

## Implementation
- Runtime matches actual spoken sentences against the game's installed official/translated `character[event][index]` text, with curated styles taking priority over the existing heuristics.
- Reads locale JSON using the game's own Newtonsoft.Json; **no original game lines, game files or third-party DLLs are distributed** in the repository/release.
- Searches the game's Lang and BepInEx folders and persistent-data location for compatible locales. For Korean, you can put your own `ko-KR.json` under `BepInEx/config/TextFXUnknown/` when needed.
- Original typewriter timing, safe glyph-height displacement limits, F8 diagnostic, and `[Visuals]` configuration remain available.
- Automatic fallback remains for unrecognized/new/modded lines.

## Install
Replace `BepInEx/plugins/TextFXUnknown.dll` with this compiled release. Verify `TextFX Unknown 0.6.0 loaded` and `[TextFX] Curated dialogue:` in the BepInEx log, then look for `curated=True` as characters speak.

**Note:** CI tests compile/build packaging and metadata integrity, not an actual gameplay session. This is an unofficial, experimental Windows Unity Mono/BepInEx 5 fan mod for C:U 7.0 Demo.