# TextFX Unknown

**Animated dialogue text for Casualties: Unknown.**

TextFX Unknown is an **unofficial, experimental BepInEx 5 mod** that gives dialogue a bit more personality. Use simple inline tags to make words shake, sway, appear gradually, or land with an impact. It also works with TextMeshPro's existing formatting tags, so you can combine animation with color, bold text, and font sizes.

> **Preview build:** This project targets the Windows Mono version of **Casualties: Unknown 7.0 Demo (Unity 2022.3.62f3)**. The plugin is not yet confirmed to work in every scene or in the full release. Please report any issues with the BepInEx log attached.

## What does it do?

- **Shake and wave:** animate individual letters instead of the entire text object.
- **Typewriter timing:** set a per-character reveal speed or insert a deliberate pause.
- **Fade and impact:** fade text in or out, or give individual letters a short burst of motion.
- **Easing and rainbow:** add a small entrance bounce or continuously shifting letter colors.
- **TextMeshPro formatting:** preserve built-in tags like `<color>`, `<b>`, `<i>`, `<size>`, and `<sprite>`.
- **Korean and English:** the effects work with both, as long as the game actually passes the tagged text to a TMP text component.

## Automatic dialogue animation (new in 0.3.1)

You **do not need to edit your language JSON** to see the basic effect. By default, the plugin attempts to identify character dialogue through `Talker.Talk`, TextMeshPro object names, and matching on-screen text. Recognized lines receive subtle wave/fade motion; exclamations can shake. Your existing English or Korean game text remains unchanged.

If a scene uses Unity's legacy UI Text component rather than TextMeshPro, glyph animation is not yet supported for that component. This is still a preview and detection is best effort.

### Diagnose rendering with F8

While in a game scene, press **F8**. For six seconds the mod tries to animate up to 10 visible TextMeshPro labels with an exaggerated rainbow/wave/shake effect. Inspect `BepInEx/LogOutput.log` for `[TextFX]` messages:

- `Talker.Talk patched overloads:` — whether the game dialogue hook was found.
- `Talker.Talk observed` — whether dialogue events actually reached it.
- `Auto effect applied to TMP` — which text objects were animated.
- `Unable to render text effects` — whether TMP mesh manipulation failed.

If F8 animates text but character speech stays plain, please send the log: dialogue detection may need a game-specific fix. If F8 doesn't animate even ordinary UI text, the TMP rendering path needs investigating.

To turn off automatic animation, change `AutoAnimateDialogue = false` under `[Dialogue]` in `BepInEx/config/com.hgsoft.textfxunknown.cfg`. Explicit effect tags still work when this option is disabled.

No external character or item assets are bundled. **This version does not replace textures, images, sprites, or character portraits, and it does not shake the entire dialogue window.**

## Installing

1. Install **BepInEx 5 (Mono, Windows x64)** in your copy of Casualties: Unknown. [BepInEx releases](https://github.com/BepInEx/BepInEx/releases).
2. Open the [latest TextFX Unknown release](../../releases) and download `TextFX-Unknown-v0.3.1-pre.1.zip`.
3. From the ZIP, extract **`BepInEx/plugins/TextFXUnknown.dll`** into the corresponding folder inside your game directory.
4. Launch the game and look for `TextFX Unknown 0.3.1 loaded` in `BepInEx/LogOutput.log`.

The ZIP also contains editable **source code**, but you don't need it just to play. It intentionally **does not contain a README**, BepInEx itself, game files, or Unity reference DLLs.

## Using text effects

Add tags to text that the game displays using TextMeshPro, including supported dialogue/localization strings:

```json
{
  "warning": "<speed=0.08><shake=0.8><color=#FF6666>Watch out!</color></shake></speed>",
  "wonder": "<fadein=0.4><wave=0.7>What is this place...?</wave></fadein>",
  "friendly": "<rainbow><ease>Hello, friend!</ease></rainbow>",
  "pause": "<speed=0.065>Wait...<pause=0.5/>what was that?</speed>"
}
```

| Tag | Result |
| --- | --- |
| `<shake=0.7>…</shake>` | Small, repeated letter movement |
| `<wave=0.5>…</wave>` | Smooth vertical wave |
| `<speed=0.08>…</speed>` | Reveal one character every 0.08 seconds |
| `<pause=0.5/>` | Wait 0.5 seconds before the next character |
| `<fadein=0.5>…</fadein>` | Fade in over 0.5 seconds |
| `<fadeout=0.8>…</fadeout>` | Fade out after appearing |
| `<impact=1>…</impact>` | Brief stronger letter shake |
| `<ease>…</ease>` | Short entrance bounce |
| `<rainbow>…</rainbow>` | Cycling letter colors |
| `<alpha=0.6>…</alpha>` | Reduce letter opacity |

All custom tags are removed before TMP renders the text. Regular TMP rich-text tags are retained. **Do not paste the entire JSON snippet into the game:** edit the appropriate existing locale string instead, preserving its original key and structure. This mod is a renderer, not a language-file installer.

## Compatibility and limitations

- **BepInEx 5 / Unity Mono only.** IL2CPP, Android, and BepInEx 6 are not supported by this build.
- The game must use TextMeshPro for the affected text. If a scene uses a different text renderer, these effects will not appear.
- Other mods that replace or override TMP text methods or dialogue may interfere.
- This is a **preview** and has not been validated in a live game session. A successful CI build is not the same as a gameplay test.
- Tagged text is always supported where TMP is detected. Untagged dialogue is automatically animated when recognized. Some scenes, other renderers, and unusual markup combinations may not be supported.

## Build from source

The repository does not commit game DLLs or third-party runtime DLLs. Use the provided build script to download compile-only references from the official BepInEx release and the Unity library mirror maintained by BepInEx, then compile with the .NET SDK:

```bash
python3 scripts/download_references.py
dotnet build src/TextFXUnknown.csproj -c Release
python3 scripts/package.py
```

For offline builds, you can instead put **`BepInEx.dll`**, **`0Harmony.dll`**, and **`UnityEngine.CoreModule.dll`** from your legally obtained game installation and BepInEx distribution into `refs/`.

The GitHub Actions workflow builds the DLL and packages the release ZIP from the source on each push to `main`.

## Feedback

Found a broken line, missing animation, or a conflict with another mod? Please [open an issue](../../issues) and include your game version, BepInEx version, the exact text/tag combination, and a relevant log excerpt. Avoid posting private data or redistributing game assets.

## Credits and disclaimer

Developed by **DoubDu / HGSoft**. Uses [BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony), and Unity TextMeshPro APIs. Inspired by the C:U modding community; other mod projects' code is **not included** in this repository or release package.

**TextFX Unknown is a fan-made project and is not affiliated with, endorsed by, or published by Orsoniks, Oro Interactive, Unity, or the BepInEx developers.** Casualties: Unknown and related assets belong to their respective rights holders. A legitimate copy of the game is required.