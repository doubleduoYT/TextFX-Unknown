#!/usr/bin/env python3
"""Regression checks for TextFX leaking into HUD and partial typewriter tags."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
core=(root/"src/TextFXUnknown.cs").read_text(encoding="utf-8")
auto=(root/"src/TextFXUnknown.Automatic.cs").read_text(encoding="utf-8")
assert '[BepInPlugin("com.hgsoft.textfxunknown", "TextFX Unknown", "0.7.5")]' in core
assert "if (!NameLooksLikeDialogue(component)) return;" in core, "Draw must reject HUD"
assert "component != null && NameLooksLikeDialogue(component)" in core, "Tagged setters must not animate HUD"
assert "if (NameLooksLikeDialogue(component))\n                        Animate(component, parsed);" in core, "Slow scanner must not animate HUD"
assert "!NameLooksLikeDialogue(component)" in core, "LateUpdate must reject non-dialogue"
assert "harmony.Patch(method, prefix:" in auto, "Talker prefix sanitation absent"
assert "harmony.Patch(method, postfix:" not in auto, "Unsafe Talker postfix still active"
assert "OnTalk(object __instance, ref string __0)" in auto
assert "if (parsed.HasEffects) __0 = parsed.Text;" in auto, "Typewriter must not receive raw custom tags"
assert "if (!named) continue;" in auto, "Broad automatic matching must not animate HUD"
assert "bool diagnostic = named && Time.unscaledTime < testUntil" in auto, "F8 limited to dialogue"
for blocked in ("hud","debug","version","beta","hint","setting","menu","inventory","tooltip","button","status"):
    assert 'name.Contains("'+blocked+'")' in auto, "HUD exclusion missing: "+blocked
for dialogue in ("talktext","tradertalktext","chartalktext","dialoguetext","speechtext","bubbletext"):
    assert 'name.Contains("'+dialogue+'")' in auto, "Dialogue name missing: "+dialogue
# Keep regression constraints from previous anti-giant-glyph fixes.
assert "Vector3[] baselineVertices = entry.BaselineVertices[m]" in core
assert "Mathf.Clamp(scale * extras.ScaleX, .92f, 1.08f)" in core
assert "Vector3.ClampMagnitude(delta, glyphHeight * .24f)" in core
print("PASS: dialogue-only text animations, F8 HUD exclusion, pre-typewriter tag sanitation, geometry guard")
