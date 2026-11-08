# TextFX Unknown v0.7.6 — Old Dialogue Overlap / Stale Glyph Fix

This hotfix responds to gameplay screenshots showing earlier Korean dialogue fragments overlaid on later lines, alongside occasional solid pale glyph quads. The root cause is **suspected stale TextMeshPro geometry** when characters or font submeshes are reused.

## Changes
- **Reset the prior line before replacement:** when a TalkText component switches to a new sentence, restore its original geometry before installing the next animation state.
- **Reset expired dialogue:** when an active effect is removed, flush the source geometry back to the renderer, not just the plugin's internal dictionary.
- **Clear old glyph slots:** all unused per-material quad vertices are zeroed and their alpha cleared. This specifically addresses *long sentence → shorter sentence* transitions.
- **Flush every material:** upload the regenerated mesh geometry for all TMP materials, not only those whose current characters have active effects. Prevents stale glyphs persisting on GPU canvases or fallback font materials.
- **F7 emergency reset fixed:** F7 now restores GPU meshes and clears active animation entries, rather than only calling `ForceMeshUpdate`.
- Keep v0.7.5's dialogue-only target filtering and early `Talker.Talk` TextFX tag sanitation. Preserve all 50 effects, 61 curated presets, and the original game's typewriter timing.

## Recommended locale configuration

**For your first test, restore the original untagged `ko-KR.json`** instead of the earlier `ko-KR.TextFX.json`. TextFX retains automatic per-dialogue effects based on its curated runtime locale/phrase index, so the tags do not need to be embedded in the game's actual language strings. This also prevents two text-processing paths from conflicting. The plugin does not delete or modify the user's original JSON.

## Installation and validation

1. Completely quit C:U, then replace `BepInEx/plugins/TextFXUnknown.dll` with the DLL in this ZIP.
2. Keep the original untagged `ko-KR.json` as the installed locale; do not run the earlier tag-insertion converter for this test.
3. Restart; confirm `TextFX Unknown 0.7.6 loaded` and `[TextFX] Curated dialogue:` in the log.
4. Trigger a long character line followed by a short line, and watch for old text/solid squares remaining.
5. F7 disables effects and restores geometry immediately. If overlap persists, report the log and screenshot.

GitHub Actions validates static mesh cleanup invariants, all earlier effect/locale checks, and an actual DLL compilation and release ZIP. **Actual visual results require a live game test.** Release ZIP includes DLL, source and tests, without README or copyrighted game data.