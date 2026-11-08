# TextFX Unknown v0.7.4 — Persistent Dialogue FX Hotfix

This update responds to a report that some dialogue effects appeared only **while the text was typing** and disappeared immediately or soon after the last character was shown.

## What changed
- **Bounce:** retains its one-shot per-letter entrance, then transitions into a gentle ongoing rhythmic bounce until the dialogue disappears.
- **Float:** keeps its original rise-in animation, followed by a slow hovering movement.
- **Elastic:** retains a brief arrival stretch, then a restrained elastic oscillation.
- **Spring:** finishes the initial spring-back, then continues as a small rhythmic pulse.
- Existing **Breathe, Heartbeat, Sway, Rotate, Tremble, Rainbow, Color Pulse, Flicker, Hologram, etc.** already loop and remain supported.
- **One-shot reveals intentionally remain one-shot:** Fade In, Fade Out, Drop, Slide, Zoom In, Zoom Out, Spin In, Assemble, Blur Reveal and Stagger are not forced to loop. They settle naturally into the finished text.
- **Config toggle:** `[Visuals] SustainAmbientEffects = true` by default, or set to `false` to restore the old one-shot behavior for Bounce, Float, Elastic and Spring.
- All 50 effect tags, 61 manually curated situation profiles, 177 event groups, 194 dialogue exceptions, original C:U typewriter timing, and F7/F8 controls remain in place.
- The previously created `generated/ko-KR.TextFX.json` already has the needed tags. **It does not need to be regenerated or replaced for this DLL update.**

## Visual safety
All added motion uses the original glyph mesh coordinate baseline and the existing strict translation and scale limits. This update does not enable any full-screen text scaling.

## Installation
1. Completely exit Casualties: Unknown.
2. Replace `BepInEx/plugins/TextFXUnknown.dll` with the DLL from this ZIP.
3. Restart and confirm `TextFX Unknown 0.7.4 loaded` in `BepInEx/LogOutput.log`.
4. Let a dialogue finish, then observe a line with Bounce/Float/Spring/Elastic for persistent movement.
5. **F7** suspends all effects if a visual issue remains; **F8** runs the visual diagnostic.

The release archive includes the compiled DLL and sources/tests, **no README and no copyrighted original game text**.

GitHub Actions validates compilation and effect lifetime invariants. The actual in-game visual behavior still requires verification by the player.
