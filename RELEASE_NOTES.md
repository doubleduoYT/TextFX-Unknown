# TextFX Unknown v0.5.0 Preview 1

**More visible motion for every detected character dialogue line.** This preview responds to reports that v0.4.1 made text movement nearly invisible and that F8 was too subtle.

## What's changed
- **Visible automatic effects on ordinary speech:** all recognized character dialogue gets a wave or similar movement, even if the line is not angry, afraid, or excited. Coverage includes named TalkText, trader text, speech/subtitle-like TMP objects, and active text that matches Talker.Talk.
- **Effects start as text types:** per-character timing and C:U's original typewriter reveal are preserved; early matching uses the full Talker sentence to classify emotion from the first displayed glyph on recognized dialogue targets.
- **Stronger, safer motion:** default `EffectStrength = 0.85`, `MaxGlyphTravel = 0.21`. Movement stays within 21% of each glyph's height by default, capped at 30% if raised in config. Sway remains visible during the line, rather than expiring after the first 0.85 seconds.
- **F8 visual test fixed:** six seconds of strong saturated rainbow, wave, shake and glitch, independent of existing low intensity config, with a safe 27% glyph-height travel cap. Re-enters the diagnostic by resetting glyph animation ages for already completed text.
- **Automatic config migration:** old untouched v0.4.1 default settings (0.40/0.12) upgrade once to the new defaults. Deliberately customized settings are left unchanged.
- **More reliable glyph timing:** assign birth times to characters discovered during the TMP mesh update as well.
- **Emotion presets preserved:** fear, anger, surprise, joy, sadness, confusion, neutral and character-based variants.
- **ZIP excludes README** (available in repository). Includes built `TextFXUnknown.dll` and editable source/build scripts, no original game assets.

## Install
Replace `BepInEx/plugins/TextFXUnknown.dll` with the DLL from `TextFX-Unknown-v0.5.0-pre.1.zip`. Confirm `Loading [TextFX Unknown 0.5.0]` in `BepInEx/LogOutput.log`. Press F8 to verify a noticeable animation; observe normal character speech to verify default movement.

## Limitations
This is a Windows BepInEx 5 / Mono preview for the C:U 7.0 demo, not a guaranteed full-release/IL2CPP/Android build. Automatic coverage applies to **detected TextMeshPro character dialogue**, not unrelated menus or every unknown renderer. v0.5.0's effects have not yet been verified by an actual gameplay test; GitHub Actions compilation and archive validation are separate checks. No permission to redistribute the game's original files is implied.

Unofficial mod by DoubDu / HGSoft; not affiliated with the game's rights holders.
