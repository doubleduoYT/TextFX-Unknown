# TextFX Unknown

**Animated dialogue text for Casualties: Unknown.**

TextFX Unknown is an **unofficial, experimental BepInEx 5 mod** that gives dialogue a bit more personality. Use simple inline tags to make words shake, sway, appear gradually, or land with an impact. It also works with TextMeshPro's existing formatting tags, so you can combine animation with color, bold text, and font sizes.

> **Preview build:** This project targets the Windows Mono version of **Casualties: Unknown 7.0 Demo (Unity 2022.3.62f3)**. The plugin has been tested by a community user in the 7.0 demo for a basic wave effect, but v0.4.0 was reported as too large, v0.4.1 as too subtle, and v0.5.0 as too wave-heavy. v0.6.0 adds hand-curated dialogue effects. This build has not yet been visually tested in-game. It is not confirmed to work in every scene or in the full release. Please report any issues with the BepInEx log attached.

## What does it do?

- **Shake and wave:** animate individual letters instead of the entire text object.
- **Typewriter timing:** set a per-character reveal speed or insert a deliberate pause.
- **Fade and impact:** fade text in or out, or give individual letters a short burst of motion.
- **Easing and rainbow:** add a small entrance bounce or continuously shifting letter colors.
- **TextMeshPro formatting:** preserve built-in tags like `<color>`, `<b>`, `<i>`, `<size>`, and `<sprite>`.
- **Korean and English:** the effects work with both, as long as the game actually passes the tagged text to a TMP text component.

## Hand-curated per-dialogue effects — v0.6.0

**Not just keyword detection or a wave effect on every line.** The C:U 7.0 demo's actual English dialogue and the supplied matching Korean translation were reviewed by character and situation: **1,895 character lines, 177 event groups** (Experiment: 1,604, Milky: 146, Dune: 145). Each event was assigned a specifically selected animation, plus **194 individual line overrides** where one phrase has a different tone than the surrounding category. There are **61 authored effect presets**, including calm, fear, delight, nausea, curiosity, grief, fatigue, regret, warning, trading, comfort, anger and character-specific Dune delivery.

The plugin first tries to match the spoken **full sentence** against the manually curated table. Only dialogue absent from that table falls back to the old automatic classifier. Many ordinary lines now use a fade/bounce rather than constant wave.

### How phrase matching works

The DLL contains **only event IDs, indices and animation presets**; it does **not** bundle original copyrighted game dialogue. When the game launches, TextFX reads its installed `CasualtiesUnknown_Data/Lang/EN.json` and any compatible localized JSON in BepInEx plugins/config or the game's persistent-data directory. It builds a per-character phrase index using each `character[event][index]` and applies the chosen style as the line appears.

For your Korean language pack, if the plugin does not detect the original `ko-KR.json` in an existing mod folder, put **your own copy** at `BepInEx/config/TextFXUnknown/ko-KR.json`. Restart C:U. No text is modified or transmitted; only the local TextMeshPro letter meshes are animated.

Check the BepInEx log for `[TextFX] Curated dialogue:` and subsequent `curated=True` entries. **1,895 indexed entries per full matching locale** is expected; unrelated UI and extra custom language lines remain unaffected, or receive a fallback if identified as character speech.

The character effect still begins on the first visible glyph, preserving the game's own typewriter timing. F8 is a separate six-second visible rainbow/shake/glitch test.

### Curated examples by context

| Source dialogue situation | Hand-selected effect |
| --- | --- |
| First arrival | Cautious fade, hopeful bounce or surprise, with line-specific overrides |
| Tasty food | Light, happy entrance |
| Bad food / nausea | Disgusted shake and subdued green |
| Injured / bleeding | Pain impacts or frightened tremors |
| Exhaustion / grief | Tired fades and muted color, not constant wave |
| Trader greetings | Polite entrance and warm bounce |
| Trader warnings / fighting | Short, restrained impact and tense shake |
| Milky's normal dialogue | Calm, precise motion |
| Dune's trader dialogue | Distinct friendly, awkward and threatening variants |

The precise English and Korean localization files are **read at runtime**, never shipped in the release ZIP. If a newer game build changes a line or uses a non-TextMeshPro renderer, curated matching may not apply; this preview needs in-game testing.

## Automatic dialogue animation — v0.6.0

**No localization JSON edits required.** The game still controls the speed and visibility of each letter; TextFX decorates the characters as soon as they become visible. Automatic dialogue detection works with both incremental text changes (`H → He → Hel...`) and TextMeshPro's built-in `maxVisibleCharacters` reveal. An animation state remembers the appearance time of **each glyph**, so newly revealed characters animate immediately instead of waiting for the end of a line. **Letters stay near their original positions**: all motion is measured as a percentage of each glyph's height, then strictly capped.

### Visible movement, without scattered letters

v0.5.0 makes normal dialogue visibly animated again. **Every recognized C:U character dialogue line** gets an effect automatically — including ordinary speech that contains no emotional keywords — while the game's original letter-by-letter reveal remains in control. TalkText, trader dialogue, speech bubbles, and subtitle-like TMP objects are prioritized; messages observed through `Talker.Talk` can also be matched. This avoids altering game localization JSON files or broadcasting modified text in multiplayer.

Unlike the oversized v0.4.0 visuals, all movement is based on **each letter's height** and has a strict distance cap. Basic characters gently sway throughout a line rather than losing all movement after less than one second.

The following settings are in `BepInEx/config/com.hgsoft.textfxunknown.cfg`:

```ini
[Visuals]
EffectStrength = 0.85
MaxGlyphTravel = 0.21
```

- `EffectStrength` controls visible letter movement and color blending (0–1).
- `MaxGlyphTravel` is the maximum travel measured as a fraction of each glyph's height, with a hard maximum of 0.30. It never uses absolute world-coordinate jumps.
- Existing v0.4.1 configuration files that still contain the **original defaults** 0.40 / 0.12 are automatically upgraded once to 0.85 / 0.21. User-customized values are preserved.
- For calmer text, choose `EffectStrength = 0.60` and `MaxGlyphTravel = 0.15`. For more movement, use 1.0 / 0.25.

**F8 diagnostic:** Press F8 at any time to show strong, saturated rainbow coloring, continuous wave/shake, and a short glitch effect for **six seconds** on up to ten active TMP text labels. The test is independent of ordinary visual-intensity settings, and it resets a line's old glyph timers so it works even *after* the text finishes displaying. F8 deliberately uses stronger but still bounded movement (maximum 27% of glyph height).

Some unusual scene objects or non-TextMeshPro dialogue components may not be recognized. The current preview does not guarantee 100% coverage of every scene; please report missing lines with a log or screenshot.

### Emotion-aware presets

| Detected mood | Automatic effect |
| --- | --- |
| Neutral | Short fade-in + subtle entrance bounce |
| Fear | Red tint, trembling and small glitch jitter |
| Anger | Red tint, shake and per-letter impact |
| Surprise | Gold tint and impact/bounce |
| Joy | Rainbow color and entrance bounce |
| Sadness | Blue tint, slow fade and soft wave |
| Confusion / questions | Purple tint, wave and subtle glitch |

Mood is inferred using the complete line from `Talker.Talk` when accessible, otherwise from displayed text. The mod attempts to distinguish Experiment, Milky and Dune using object names where possible, and applies an individual neutral style; character identification is **best effort**, not guaranteed.

**Existing game typewriter timing is preserved.** TextFX does not replace the original dialogue text or forcibly reveal the full string. The automatic effects are visual only; they do not change the Steam multiplayer protocol or broadcast modified text to other players.

### Diagnose rendering with F8

Press **F8** in a game scene to run the visible six-second rainbow/glitch/shake test on up to ten active TMP labels. The BepInEx log includes `[TextFX]` lines for `Talker.Talk observed`, `Auto FX started/updated`, and errors. Try F8 if no effects appear. A lag of approximately one rendered frame can occur depending on how the game updates text.

Use `AutoAnimateDialogue = false` in `BepInEx/config/com.hgsoft.textfxunknown.cfg` to disable automatic effects while leaving manually tagged text enabled.

No external character or item assets are bundled. **Textures, images, portraits and whole-window shaking are not implemented.**

## Installing

1. Install **BepInEx 5 (Mono, Windows x64)** in your copy of Casualties: Unknown. [BepInEx releases](https://github.com/BepInEx/BepInEx/releases).
2. Open the [latest TextFX Unknown release](../../releases) and download `TextFX-Unknown-v0.6.0-pre.1.zip`.
3. From the ZIP, extract **`BepInEx/plugins/TextFXUnknown.dll`** into the corresponding folder inside your game directory.
4. Launch the game and look for `TextFX Unknown 0.6.0 loaded` in `BepInEx/LogOutput.log`.

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
| `<glitch=0.5>…</glitch>` | Brief per-glyph color/jitter glitches |
| `<tint=#FF7777>…</tint>` | Blend glyph color with the specified RGB tint |

All custom tags are removed before TMP renders the text. Regular TMP rich-text tags are retained. **Do not paste the entire JSON snippet into the game:** edit the appropriate existing locale string instead, preserving its original key and structure. This mod is a renderer, not a language-file installer.

## Compatibility and limitations

- **BepInEx 5 / Unity Mono only.** IL2CPP, Android, and BepInEx 6 are not supported by this build.
- The game must use TextMeshPro for the affected text. If a scene uses a different text renderer, these effects will not appear.
- Other mods that replace or override TMP text methods or dialogue may interfere.
- This is a **preview** and has not been validated in a live game session. A successful CI build is not the same as a gameplay test.
- Tagged TMP text is supported. Untagged dialogue is automatically animated when recognized. This release is intended to begin effects while characters are being revealed, but full gameplay verification is still pending. Some scenes, other renderers, and unusual markup combinations may not be supported.

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