# TextFX Unknown v0.7.1 — Emergency Glyph Scaling Hotfix

This hotfix responds to screenshots of individual Korean glyphs growing until they cover the entire C:U gameplay screen in v0.7.0.

## Root cause and correction

The previous renderer used `textInfo.meshInfo.vertices` as the input geometry for the next frame. Those positions could still be **transformed from the previous frame**, so repeated scaling and rotations compounded indefinitely, despite being clamped within each individual frame.

**Fix:** Read the canonical original quad corners (`TMP_CharacterInfo.bottomLeft/topLeft/topRight/bottomRight`) instead. Every new frame transforms each glyph **from its source corners**; last frame's already modified vertices are never used as the starting size or center. Request fresh TextMeshPro geometry before rendering and preserve original per-line mesh colors to prevent cumulative alpha/tint changes.

Additional safeguards:
- Absolute per-frame output scale limited to **85–115% relative to original glyph geometry**.
- Vertex displacement (including combined scaling, rotation, translation) limited to **45% of the original glyph height**.
- **F7 emergency toggle**: disables/restores TextFX visuals immediately, with a TMP mesh reset, without modifying game files or configs.
- All 50 effects, 61 curated dialogue presets, 177 event groups and 194 per-line exceptions are retained.
- Original C:U typewriter timing remains under the game's control.

## Installation

1. **Completely exit** C:U.
2. Replace `BepInEx/plugins/TextFXUnknown.dll` with the new file inside this ZIP.
3. Start C:U; look for `TextFX Unknown 0.7.1 loaded` in `BepInEx/LogOutput.log`.
4. If any issue reoccurs, press F7 and attach a fresh log/screenshot when reporting the problem.

The release ZIP contains the compiled DLL, editable source and tests; README remains on GitHub and no original game assets are bundled.

GitHub Actions validates source safeguards and compilation. **Visual correctness has not yet been confirmed in a live C:U gameplay session.** Unofficial Windows Mono/BepInEx 5 fan mod by DoubDu / HGSoft.
