# TextFX Unknown v0.7.0 — 50 Effects Preview 1

The user-supplied catalog of **50 text effects** is now represented by parser tags and an extended per-glyph TextMeshPro animation layer across five families.

### New effects
- **Movement (12):** Bounce, Sway, Rotate, Pendulum, Tremble, Heartbeat, Breathe, Drop, Float, Slide, Twist, Elastic.
- **Reveal/exit (10):** Fade In, Fade Out, Zoom In, Zoom Out, Spring, Typewriter, Stagger, Blur Reveal*, Spin In, Assemble.
- **Color/visual (10):** Rainbow, Color Pulse, Gradient, Sparkle, Neon Glow*, Flicker, Hologram, Afterimage*, Chromatic Aberration*, Glitch.
- **Emotion (10):** Fear, Rage, Surprise, Happy, Sadness, Confusion, Whisper, Scream, Exhausted, Insanity.
- **Advanced (8):** Accelerate, Decelerate, Word Emphasis, Punctuation Pause, Screen Shake*, Shockwave, Ripple, Corruption*.

Items marked * are **visual approximations, not their full cinematic equivalent**. Blur uses alpha/scale instead of a blur pass; Neon is color emphasis without a glow outline; Afterimage uses tint oscillation without cloning glyph meshes; Chromatic uses RGB tint oscillation without actual RGB channel offsets; Screen Shake moves text glyphs but not the whole dialogue window; Corruption does not replace actual characters.

### Changes to real C:U dialogue
- Reassigned **all 61** human-curated context presets used across **177 event groups and 194 phrase exceptions**.
- Previously wave-heavy dialogue now uses distinct animations: exhaustion/breathing, hope/float, pain/tremble, trading/slide, surprise/spring, warning/shockwave, Dune's calm/cautious rotation and more.
- Existing per-phrase locale matching takes precedence, then normal fallback for unidentified character speech.
- Supports composition such as `<colorpulse=#FF0000,#00FFFF>`, `<bounce=0.6>`, `<spring=0.8>` in custom text.
- Auto dialogue preserves C:U's own typewriter. Advanced timing controls activate when tagged explicitly.

### Safety, installation, verification
- Every glyph's translation is clamped to the configured local travel limit (maximum 30% of its own height); rotation ±18° and scale 70–130%.
- Windows Mono, BepInEx 5, C:U 7.0 demo; full release, Android, and IL2CPP are not validated.
- Replace `BepInEx/plugins/TextFXUnknown.dll`. Confirm `TextFX Unknown 0.7.0 loaded` in BepInEx log. The ZIP contains the built DLL and editable source/tests **without README or game assets**.
- CI verifies catalog count, curated mapping metadata, and successful compilation; **visual behavior in the running game still requires testing**.
