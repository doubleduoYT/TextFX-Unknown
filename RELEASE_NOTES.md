# TextFX Unknown v0.4.0 — Preview 1

TextFX Unknown now animates individual dialogue glyphs from their first visible frame, while preserving the game's original dialogue reveal.

## Changes
- **Immediate effects:** poll cached active TMP components every frame after game Update instead of detecting dialogue every 0.5 seconds.
- **Progressive text:** recognize prefixes of the latest `Talker.Talk` line (e.g. `H`, `He`, `Hel`) and update the existing animation without restarting already shown letters.
- **Game-controlled reveal:** read `maxVisibleCharacters` without overriding it in automatic mode.
- **Per-glyph timers:** fade, impact, and entrance easing are measured from each character's first visible frame, not the end of the complete line.
- **Emotion-driven FX:** distinguish neutral, fear, anger, surprise, joy, sadness and confusion using text and punctuation. Optional speaker-based neutral presets for Experiment, Milky and Dune when detectable.
- **New custom tags:** `<glitch=0.5>` and `<tint=#FF7777>`.
- **F8 diagnostic:** retain six-second visual FX test; adjust it to work on already-animated text.
- **Performance:** refresh TMP component cache periodically, but check cached text values each frame.
- **Packaging:** release ZIP contains the compiled BepInEx DLL and editable source/build scripts, **not README** or game/Unity DLLs.

## Compatibility and verification
- Targets Windows x64, Mono, BepInEx 5 and C:U 7.0 Demo.
- GitHub Actions compiles the actual DLL and verifies the distribution ZIP.
- The previous v0.3.1 was reported to work by a user for a wave effect. **v0.4.0 is not yet tested in a live C:U session.**
- Other mods that rewrite dialogue or mesh geometry may still interfere.
- TextMeshPro only; legacy Unity UI Text is not animated.

## Install
Replace `BepInEx/plugins/TextFXUnknown.dll` with the DLL from the ZIP. Verify `Loading [TextFX Unknown 0.4.0]` in `BepInEx/LogOutput.log`. Test characters while the line is still typing, not just after it finishes.

Developed by DoubDu / HGSoft. Unofficial fan mod, not affiliated with the rights holders.
