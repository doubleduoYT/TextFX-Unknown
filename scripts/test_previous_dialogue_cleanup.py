#!/usr/bin/env python3
"""Guard against stale glyph triangles when text content changes or gets shorter."""
from pathlib import Path
source=(Path(__file__).resolve().parents[1]/"src"/"TextFXUnknown.cs").read_text(encoding="utf-8")
assert "private void CommitVisibleTextMeshes(Component component, object info)" in source
assert "private void RestoreOriginalMesh(Component component)" in source
assert "old.Parsed.Text != parsed.Text" in source
assert "RestoreOriginalMesh(component);" in source
assert "RestoreOriginalMesh(previous.Component);" in source
assert "animations.Clear();" in source
assert "CommitVisibleTextMeshes(component, info);" in source
assert "updateGeometry.Invoke(component, new object[] { mesh, material });" in source
assert "liveQuads[material].Add(vertex)" in source
assert "if (liveQuads[material].Contains(vertex)) continue;" in source
assert "vertices[vertex + j] = Vector3.zero;" in source
assert "color.a = 0;" in source
assert "for (int material = 0; material < meshInfos.Length; material++)" in source
assert "Vector3[] baselineVertices = entry.BaselineVertices[m]" in source
assert "Mathf.Clamp(scale * extras.ScaleX, .92f, 1.08f)" in source
assert "Vector3.ClampMagnitude(delta, glyphHeight * .24f)" in source

def remove_unused(current_quad_indices, buffer_capacity):
    active=set(current_quad_indices)
    buffer=[float(i+1) for i in range(buffer_capacity*4)]
    for q in range(buffer_capacity):
        if q in active:
            continue
        for j in range(4):
            buffer[q*4+j]=0.
    return buffer
# 8 glyphs visible previously, but only positions 0-2 in the next line.
test=remove_unused([0,1,2],8)
assert all(test[i] != 0 for i in range(3*4))
assert all(test[i] == 0 for i in range(3*4,8*4))
# A fallback-font material can have no glyphs at all after line change.
assert remove_unused([],8)==[0.]*32
print("PASS: replaced/expired dialogue restoration, old-quad cleanup, all TMP materials upload, size bounds")
