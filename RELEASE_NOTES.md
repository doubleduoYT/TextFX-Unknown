# TextFX Unknown v0.8.0 — Safe Korean Dialogue Renderer

**Priority: stop corrupted Korean dialogue and giant tan glyph rectangles.** Screenshots and a full BepInEx log from v0.7.6 show repeated attempts to replace character mesh buffers, but broken text persisted. No clear rendering exception was recorded.

## New safe default renderer
- `[Safety] EnableExperimentalGlyphMesh = false` by default (including on existing installs).
- In SAFE mode **never modify TMP glyph vertices, UVs, mesh materials or GPU submesh buffers**. No `ForceMeshUpdate`/`UpdateGeometry` calls are made from its drawing/cleanup paths.
- Instead, apply small full-dialogue transform-scale/rotation motion (up to ~2% / ±1.7°) and color tint/pulse using public TMP `color` and Unity Transform APIs.
- **F7** restores the native text object's scale/rotation/color and suspends the effects.
- **F8** tests full-dialogue rainbow/color and bounded transform effects without modifying glyph buffers.
- Keep 50 defined effect tags, 61 curated profiles, 177 situation groups, 194 per-line exceptions, and language detection. SAFE mode intentionally **does not perform per-character visual animation**.
- Unsafe previous per-glyph renderer exists strictly for opt-in debugging; **do not enable it** during normal Korean play.

## Important installation steps
1. Fully exit Casualties: Unknown.
2. Replace `BepInEx/plugins/TextFXUnknown.dll` with the DLL from this release ZIP.
3. Use the original **untagged** `ko-KR.json` (not `ko-KR.TextFX.json`) for this test, and avoid duplicate locale packs.
4. Verify `[TextFX] Rendering mode: SAFE` and `TextFX Unknown 0.8.0 loaded` in `BepInEx/LogOutput.log`.
5. Confirm the top-of-screen Hangul appears correctly across several successive dialogue lines. Press F8 to test the safe visual effects.
6. If text remains corrupted, remove TextFXUnknown.dll entirely and retest to isolate conflicts with XTMP/Together/other mods.

## Validation
GitHub Actions runs the safe renderer source guard, existing 50-tag and locale tests, and a real DLL compilation / ZIP check. The ZIP contains compiled DLL and editable source/tests, not game files or README. **Actual in-game correction still awaits user testing.**
