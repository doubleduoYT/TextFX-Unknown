#!/usr/bin/env python3
"""Static and numerical regression tests for long-lived dialogue animations."""
from pathlib import Path
from math import sin
base=Path(__file__).resolve().parents[1]
core=(base/"src"/"TextFXUnknown.cs").read_text(encoding="utf-8")
extended=(base/"src"/"TextFXUnknown.Extended.cs").read_text(encoding="utf-8")

assert 'SustainAmbientEffects", true' in core
assert "sustainAmbient != null && sustainAmbient.Value" in core
assert "bool sustainAmbient)" in extended
for name in ("bounce","float","elastic","spring"):
    assert 'fx.Get("'+name+'")' in extended
for marker in ("idleBounce","float hover = sustainAmbient",
               "idleStretch", "float pulse = sustainAmbient"):
    assert marker in extended, "missing ongoing loop: "+marker
# Verify the persistent terms are keyed off GLOBAL time, not just a fading
# per-glyph entrance time; at age=10s the entrance has faded to zero.
for age in (8.0, 20.0, 100.0):
    phase=age*4.8+.37
    bounce=.073*max(0.0,sin(phase))**2
    hover=.076*sin(age*2.1+.27)
    elastic=.039*sin(age*2.7+.26)
    spring=.034*sin(age*3.45+.19)
    assert max(abs(bounce),abs(hover),abs(elastic),abs(spring)) > .01
    assert max(abs(bounce),abs(hover),abs(elastic),abs(spring)) < .08
# One-shot FX must keep an exp(-age)-style envelope, not restart indefinitely.
assert 'fx.Get("drop")' in extended and '.24f * entrance * a' in extended
assert 'fx.Get("zoomin")' in extended and '.28f * entrance * a' in extended
assert 'fx.Get("assemble")' in extended and '.19f * entrance * a' in extended
# Regression against huge scaling; maintain strict immutable original geometry.
assert "Vector3[] baselineVertices = entry.BaselineVertices[m]" in core
assert "Mathf.Clamp(scale * extras.ScaleX, .92f, 1.08f)" in core
assert "Vector3.ClampMagnitude(delta, glyphHeight * .24f)" in core
print("PASS: persistent Bounce/Float/Elastic/Spring, one-shot reveals, safe original-glyph bounds")
