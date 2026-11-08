#!/usr/bin/env python3
"""Fail-safe rendering contract: no TMP mesh mutation in default mode."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
core=(root/"src"/"TextFXUnknown.cs").read_text(encoding="utf-8")
safe=(root/"src"/"TextFXUnknown.Safe.cs").read_text(encoding="utf-8")
assert '[BepInPlugin("com.hgsoft.textfxunknown", "TextFX Unknown", "0.8.0")]' in core
assert '"EnableExperimentalGlyphMesh", false' in core
assert "private bool UseSafeRenderer" in core
assert "if (UseSafeRenderer) DrawSafe(entry);" in core
assert "if (UseSafeRenderer)\n                {\n                    RestoreSafeText(old);" in core
assert "if (UseSafeRenderer) RestoreSafeText(animation);" in core
assert "if (UseSafeRenderer) RestoreSafeText(previous);" in core
assert "private void DrawSafe(TextAnimation entry)" in safe
assert "private void RestoreSafeText(TextAnimation entry)" in safe
for forbidden in ("ForceMeshUpdate(", "UpdateGeometry(", "mesh.vertices", "colors32", "updateGeometry.Invoke(", "forceMeshUpdate.Invoke(", "meshInfo"):
    assert forbidden not in safe, "unsafe mesh access in SAFE renderer: "+forbidden
assert "OriginalLocalScale * scale" in safe
assert "Mathf.Clamp(scale, .98f, 1.02f)" in safe
assert "OriginalLocalRotation * Quaternion.Euler" in safe
assert "textColorProperty.SetValue(entry.Component, color, null)" in safe
print("PASS: safe mode default, mesh-free draw/cleanup, preserved native scale/rotation/color, 2% bounds")
