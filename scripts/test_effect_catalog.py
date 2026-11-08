#!/usr/bin/env python3
"""Verify 50 user-requested tags and renderer entry points, without game files."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
core=(root/"src"/"TextFXUnknown.cs").read_text(encoding="utf-8")
extended=(root/"src"/"TextFXUnknown.Extended.cs").read_text(encoding="utf-8")
curated=(root/"src"/"TextFXUnknown.Curated.cs").read_text(encoding="utf-8")
groups=[
"bounce sway rotate pendulum tremble heartbeat breathe drop float slide twist elastic",
"fadein fadeout zoomin zoomout spring typewriter stagger blurreveal spinin assemble",
"rainbow colorpulse gradient sparkle neon flicker hologram afterimage chromatic glitch",
"fear rage surprise happy sadness confusion whisper scream exhausted insanity",
"accelerate decelerate wordemphasis punctuationpause screenshake shockwave ripple corruption"
]
names=[v for group in groups for v in group.split()]
assert len(names)==50 and len(set(names))==50
tag_decl=re.search(r'private const string FxNames = "([^"]+)";',core)
assert tag_decl is not None, "missing tag registry"
registry=set(tag_decl.group(1).split("|"))
assert set(names)<=registry, "missing tags: "+str(set(names)-registry)
for name in names:
    assert name in core or name in extended, "missing implementation mention: "+name
assert "EvaluateExtendedEffects(fx, i, count, now, age," in core
assert "FxRevealStep(fx, i, count)" in core
assert "IsPunctuation(" in core
assert "Vector2.ClampMagnitude(displacement" in core, "travel cap removed"
assert "new Dictionary<string, float>(Extra" in core, "nested tag state not cloned"
styles=re.findall(r'^\s+\{ "([a-z_]+)", "([^"]+)" \},$',curated,re.M)
assert len(styles)==61, "61 authored dialogue profiles must remain"
assert len(set(name for name,_ in styles))==61
new_used=sum(any("<"+name+"=" in body or "<"+name+">" in body for name in names if name not in ("fadein","fadeout","rainbow","glitch")) for _,body in styles)
assert new_used>=45, "curated profiles did not adopt extended effects"
print("PASS: 50 tag names, safe vertex transform, 61 updated curated profiles, " + str(new_used) + " upgraded presets")
