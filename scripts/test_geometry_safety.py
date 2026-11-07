#!/usr/bin/env python3
"""Guard against cumulative text mesh scaling (source invariants + deterministic model)."""
from pathlib import Path
core=(Path(__file__).resolve().parents[1]/"src"/"TextFXUnknown.cs").read_text(encoding="utf-8")
for corner in ("bottomLeft","topLeft","topRight","bottomRight"):
    assert 'Field(charInfo, "'+corner+'")' in core, "missing TMP canonical corner: "+corner
assert "originalQuad[j] - center" in core, "glyph transform not based on source"
assert "originalQuad[j] +\n                        Vector3.ClampMagnitude(delta" in core, "missing absolute vertex clamp"
assert "Mathf.Clamp(scale * extras.ScaleX, .85f, 1.15f)" in core
assert "Mathf.Clamp(scale * extras.ScaleY, .85f, 1.15f)" in core
assert "Vector3.ClampMagnitude(delta, glyphHeight * .45f)" in core
assert "Color32 c = originalColors[v + j]" in core, "colors may accumulate over frames"
assert "KeyCode.F7" in core and "effectsSuspended" in core, "F7 safety toggle absent"
assert "var local = vertices[v + j] - center" not in core, "cumulative mesh input reintroduced"
# Model a repeated scale using original corners across 10,000 simulated frames.
# This alone is not a Unity runtime test: it checks the safety principle.
import math
quad=[(-.5,0),(-.5,1),(.5,1),(.5,0)]
for frame in range(10000):
    scale=1.15+0.02*math.sin(frame/11)
    scale=max(.85,min(1.15,scale))
    for x,y in quad:
        nx=x*scale
        ny=(y-.5)*scale+.5
        displacement=math.hypot(nx-x,ny-y)
        assert displacement <= .45+1e-7
print("PASS: canonical TMP geometry, bounded glyph scale and offset, original colors, F7 rollback")
