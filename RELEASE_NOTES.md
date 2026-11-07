# TextFX Unknown v0.7.2 — Isolated Giant Glyph Hotfix

Screenshots of v0.7.1 still showed **isolated Hangul syllables appearing several times larger than ordinary dialogue**. The previous emergency fix used the four corner metrics from `TMP_CharacterInfo`, which are not always interchangeable with the actual vertices for a TextMeshPro submesh/material.

## Rendering correction
- Use immutable copies of the **actual TextMeshPro meshInfo.vertices** as the per-glyph baseline, instead of `TMP_CharacterInfo.bottomLeft/topLeft/topRight/bottomRight`.
- On every new line or incremental typewriter text update, **force a full native TMP reparse** and refresh the baseline geometry and original colors.
- Every frame now calculates the current glyph transformation relative to the same immutable actual mesh quad; never from previously modified glyph vertices or mismatched character layout metrics.
- Final scaling is restricted to **92–108%** of actual source glyph size, rotation to **±9°**, and each output vertex to within **24% of source glyph height** from its native coordinate.
- This applies to all composed 50 effect tags and 61 manually curated dialogue styles; only extreme zoom/rotation range is deliberately reduced.
- Retains F7 emergency toggle and F8 diagnostic, as well as existing per-character start timing.

## Install and test
1. Fully exit Casualties: Unknown.
2. Replace `BepInEx/plugins/TextFXUnknown.dll` with the new DLL from this ZIP.
3. Relaunch, verify `TextFX Unknown 0.7.2 loaded`, and test a normal dialogue without pressing F8.
4. If giant letters recur, press F7 to suspend effects and save a screenshot and `BepInEx/LogOutput.log`.

Built for Windows Mono/BepInEx 5, C:U 7.0 demo. CI compilation and static safety tests pass, but **actual visual behavior in-game remains unverified**. ZIP contains the compiled DLL and source/tests, without README or original game assets.
