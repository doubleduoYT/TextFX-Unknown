# TextFX Unknown v0.4.1 — Preview 1

This release fixes over-sized, scattered text effects reported with screenshots from v0.4.0.

## Fixes
- **Glyph-relative movement:** all shake, wave, impact, and glitch displacements are now computed from each character's actual mesh height instead of fixed Unity coordinates such as 2.4 or 5.
- **Hard travel limit:** no letter moves farther than **12% of its own height** by default. This limit is capped at 25% even if users increase the config.
- **Lower default intensity:** `EffectStrength = 0.40` in `[Visuals]`; `MaxGlyphTravel = 0.12`. Both are configurable in `BepInEx/config/com.hgsoft.textfxunknown.cfg`.
- **Short animated entrance:** automatic movement fades out within about 0.85 seconds per letter, preventing continuous large wobbling.
- **Gentler bounce and colors:** entrance scale is limited to ±6% before strength; color tints and rainbow blend more subtly.
- **Reduced accidental UI effects:** narrower character dialogue object detection and safer `Talker.Talk` prefix matching.
- **Immediate animation retained:** each visible character still gets a separate start time without changing the game's original typewriter behavior.
- Existing emotion-based presets and F8 diagnostic remain available. Manual inline FX are also bounded by the same safety limit.

## Installation
Replace `BepInEx/plugins/TextFXUnknown.dll` with the DLL from the release ZIP and start the game. Verify `Loading [TextFX Unknown 0.4.1]` in the BepInEx log. Use F8 to test if necessary.

## Compatibility and validation
- For Windows x64, Unity Mono, BepInEx 5, C:U 7.0 demo.
- The compiled DLL and editable source are packaged in the ZIP; **README stays in the GitHub repository**.
- Automated compilation, packaging, and archive validation are performed by GitHub Actions.
- **A successful build does not guarantee that every game scene is visually correct.** Live visual testing of this fix is pending.
- This is an unofficial fan mod. No game assets or third-party DLLs are redistributed.
