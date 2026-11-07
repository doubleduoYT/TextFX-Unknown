#!/usr/bin/env python3
"""Check curated event/profile metadata without loading original game text."""
from pathlib import Path
import re

code = (Path(__file__).resolve().parents[1] / "src" / "TextFXUnknown.Curated.cs").read_text(encoding="utf-8")

def extract(name):
    tag = "private static readonly string[] " + name + " = {"
    start = code.index(tag) + len(tag)
    end = code.index("\n        };", start)
    return re.findall(r'"([^"]*)"', code[start:end])

groups = [row.split("|") for row in extract("EventAssignments")]
exceptions = [row.split("|") for row in extract("LineOverrides")]
style_names = set(re.findall(r'^\s+\{ "([a-z_]+)", "<[^"]+" \},$', code, flags=re.M))
assert len(groups) == 177, ("Event group count",len(groups))
assert len(exceptions) == 194, ("Line exceptions",len(exceptions))
assert len(style_names) == 61, ("Effect presets",len(style_names))
assert all(len(r)==3 and r[0] in "012" for r in groups)
assert all(len(r)==4 and r[0] in "012" for r in exceptions)
keys = {(r[0],r[1]) for r in groups}
assert len(keys)==len(groups), "Duplicate groups"
assert len({tuple(r[:3]) for r in exceptions})==len(exceptions), "Duplicate exceptions"
assert all((r[0],r[1]) in keys for r in exceptions), "Unknown exception event"
assert all(row[-1] in style_names for row in groups+exceptions), "Unknown style name"
assert "We're here..." not in code, "Source line accidentally included"
print("PASS: 177 event groups, 194 individual exceptions, 61 unique presets; no game text bundled.")
