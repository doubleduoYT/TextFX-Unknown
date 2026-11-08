# TextFX Unknown v0.7.5 — HUD/Text Corruption Fix

Fixes the reported screen-top garbled text, floating isolated Hangul and corrupted HUD appearance seen while TextFX v0.7.4 and the tagged Korean dialogue JSON were installed.

## Key safety corrections
1. **Dialogue-only targets.** TextFX no longer applies mesh animations to miscellaneous TextMeshPro labels, even if their text happens to match a recent line. Recognized targets include the observed `TalkText` names; HUD, version/debug, menu, tooltip and status objects are excluded.
2. **Safe F8 diagnostics.** F8 affects eligible character dialogue only, not arbitrary text/UI.
3. **Sanitize source text before the game's typewriter.** A Harmony prefix on `Talker.Talk(string, ...)` strips TextFX tags before C:U processes/subdivides localized dialogue strings. Otherwise a partly revealed tag may become broken visible letters or corrupt TMP parsing. The intended appearance can be recovered from the curated event/phrase rules.
4. **Safe generic TMP processing.** Unknown or non-dialogue objects have TextFX tags removed, preserving normal TMP markup, but receive no vertex modifications.
5. **Defense in depth.** `LateUpdate` and `Draw` reject all non-dialogue targets, even when an animation was previously scheduled.

## Retained
- 50 effect tags, 61 curated styles across 177 event groups and 194 phrase exceptions.
- All 1,895 Korean dialogue lines can stay in the tagged `ko-KR.TextFX.json`.
- The game's own typewriter timing; F7 emergency off/on; bounded source-mesh geometry.
- The ZIP includes the built DLL, source and tests. No README or original game files included.

## Installation / test
1. Close the game completely. Replace `BepInEx/plugins/TextFXUnknown.dll` with this build.
2. Relaunch and confirm `TextFX Unknown 0.7.5 loaded` in `BepInEx/LogOutput.log`.
3. Verify the **top HUD and beta/version text first**, before pressing F8.
4. Test actual speech text; F8 now intentionally affects only dialogue text.
5. If HUD corruption remains, hit F7 or remove the DLL and try your original untagged `ko-KR.json` to isolate conflicts with other mods or the game's UI.

GitHub Actions verifies the code guards, compiles and validates the archive, but a live C:U graphical test is still pending.