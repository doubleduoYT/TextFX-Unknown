# TextFX Unknown v0.3.1 Preview 1

**Fix candidate for plain/unanimated dialogue in v0.3.0.**

### Changes
- Added automatic animation for detected C:U character dialogue; editing localization JSON is no longer required for basic wave/fade effects.
- Added reflection-based `Talker.Talk` observation and object-name matching for on-screen TextMeshPro elements.
- Added **F8** six-second visual diagnostic and `[TextFX]` logs for talk events, target detection, and animation attempts.
- Skip abstract/unimplemented TMP patch targets to reduce HarmonyX warnings.
- Only override `maxVisibleCharacters` when explicit speed/pause tags request typewriter timing.
- Original localization strings remain untouched. Source and compiled mod DLL are in the ZIP; README stays on GitHub.

### Limitations
This release is compiled by GitHub Actions, but **not tested inside a running copy of the game**. Unity legacy UI Text is not supported for glyph animation. Automatic dialogue detection may miss unusual scenes, and other mods can alter text rendering.

### Installation
Replace `BepInEx/plugins/TextFXUnknown.dll` with this version. Start the game and check `BepInEx/LogOutput.log` for `TextFX Unknown 0.3.1 loaded`. If dialogue stays plain, press **F8** and attach the corresponding `[TextFX]` log lines to a GitHub issue.

This is an unofficial community mod, not affiliated with the original game's rights holders.
