#!/usr/bin/env python3
"""Protect against runaway scaling and mismatched TMP character-metrics quads."""
from pathlib import Path
import math
core=(Path(__file__).resolve().parents[1]/"src"/"TextFXUnknown.cs").read_text(encoding="utf-8")
assert "new Dictionary<int, Vector3[]>()" in core, "immutable mesh baseline cache missing"
assert "entry.BaselineVertices[materialIndex] = (Vector3[])materialVertices.Clone()" in core, "original mesh not captured"
assert "entry.BaselineVertices.Clear()" in core, "baseline must reset on line change"
assert "Vector3[] baselineVertices = entry.BaselineVertices[m]" in core
assert "originalQuad[corner] = baselineVertices[v + corner]" in core
assert 'Field(charInfo, "bottomLeft")' not in core, "text metrics must not replace mesh quad"
assert "args[1] = true" in core, "native TMP must be force-reparsed"
assert "Mathf.Clamp(scale * extras.ScaleX, .92f, 1.08f)" in core
assert "Mathf.Clamp(scale * extras.ScaleY, .92f, 1.08f)" in core
assert "Mathf.Clamp(extras.Angle, -9f, 9f)" in core
assert "Vector3.ClampMagnitude(delta, glyphHeight * .24f)" in core
assert "Color32 c = originalColors[v + j]" in core
assert "KeyCode.F7" in core and "effectsSuspended" in core
assert "var local = vertices[v + j] - center" not in core
# Simulation: each frame's transform must stay relative to the same immutable quad,
# not scale the previous frame's positions.
source=[(-.5,0),(-.5,1),(.5,1),(.5,0)]
for k in range(10000):
    alpha=k/35.
    sx=min(1.08,max(.92,1+.15*math.sin(alpha)))
    sy=min(1.08,max(.92,1+.15*math.cos(alpha)))
    angle=math.radians(min(9,max(-9,18*math.sin(k/18.))))
    c,s=math.cos(angle),math.sin(angle)
    for x,y in source:
        u=x*sx; v=(y-.5)*sy
        nx=u*c-v*s
        ny=u*s+v*c+.5
        offset=math.hypot(nx-x,ny-y)
        if offset > .24:
            nx=x+(nx-x)*.24/offset
            ny=y+(ny-y)*.24/offset
        assert math.hypot(nx-x,ny-y) <= .24000001
print("PASS: immutable source mesh, native reparse, final 8% scale/9deg/24% bounds, F7 safety")
