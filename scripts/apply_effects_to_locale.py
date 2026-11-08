#!/usr/bin/env python3
"""Apply TextFX Unknown's curated effects DIRECTLY to a local dialogue JSON.

Reads the actual user's original C:U locale. Does not bundle/distribute game text.
Compatible with original 7.0 EN.json or matching Korean ko-KR.json.
Usage:
    python apply_effects_to_locale.py ko-KR.json -o ko-KR.TextFX.json
    python apply_effects_to_locale.py EN.json -o EN.TextFX.json
    python apply_effects_to_locale.py ko-KR.json --dry-run
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import sys

SCRIPT_DIR = Path(__file__).resolve().parent
SOURCE = SCRIPT_DIR.parent / "src" / "TextFXUnknown.Curated.cs"
if not SOURCE.is_file():
    SOURCE = SCRIPT_DIR / "TextFXUnknown.Curated.cs"  # release ZIP's source/ folder
START_TAG = re.compile(r"<([a-z]+)(?:=[^>]*)?>", re.I)
FX_TAGS = re.compile(
    r"<(?:tint|shake|wave|fadein|fadeout|speed|impact|ease|pause|alpha|"
    r"rainbow|glitch|bounce|sway|rotate|pendulum|tremble|heartbeat|"
    r"breathe|drop|float|slide|twist|elastic|zoomin|zoomout|spring|"
    r"typewriter|stagger|blurreveal|spinin|assemble|colorpulse|gradient|"
    r"sparkle|neon|flicker|hologram|afterimage|chromatic|fear|rage|"
    r"surprise|happy|sadness|confusion|whisper|scream|exhausted|"
    r"insanity|accelerate|decelerate|wordemphasis|punctuationpause|"
    r"screenshake|shockwave|ripple|corruption)(?:=|>)",
    re.I,
)


def extract_rows(text: str, name: str) -> list[str]:
    marker = "private static readonly string[] " + name + " = {"
    start = text.index(marker) + len(marker)
    end = text.index("\n        };", start)
    return re.findall(r'"([^"]*)"', text[start:end])


def curated_rules() -> tuple[dict, dict, dict]:
    source = SOURCE.read_text(encoding="utf-8")
    categories = {}
    for row in extract_rows(source, "EventAssignments"):
        who, event, preset = row.split("|")
        categories[(int(who), event)] = preset
    overrides = {}
    for row in extract_rows(source, "LineOverrides"):
        who, event, index, preset = row.split("|")
        overrides[(int(who), event, int(index))] = preset
    presets = {
        name: markup
        for name, markup in re.findall(
            r'^\s*\{ "([a-z_]+)", "([^"]+)" \},$', source, re.M
        )
    }
    assert len(categories) == 177, "Incomplete curated categories"
    assert len(overrides) == 194, "Incomplete per-line curated overrides"
    assert len(presets) == 61, "Incomplete effect presets"
    return categories, overrides, presets


def wrap_phrase(phrase: str, prefix: str) -> str:
    # Preserve literal text and existing Unity rich tags. TextFX's BepInEx
    # Harmony hook removes only the custom FX markup before TMP draws the text.
    # Running twice should not duplicate tags.
    if FX_TAGS.search(phrase):
        return phrase
    names = START_TAG.findall(prefix)
    suffix = "".join("</" + name + ">" for name in reversed(names))
    return prefix + phrase + suffix


def apply_locale(data: dict, categories: dict, overrides: dict,
                 presets: dict) -> tuple[int, int, int]:
    characters = data.get("character")
    if not isinstance(characters, list):
        raise ValueError("JSON does not contain a character list; no edits made")
    changed = 0
    skipped = 0
    unknown = 0
    for speaker, events in enumerate(characters):
        if not isinstance(events, dict):
            continue
        for event, lines in events.items():
            if not isinstance(lines, list):
                continue
            preset = categories.get((speaker, event))
            if preset is None:
                unknown += sum(isinstance(item, str) and bool(item) for item in lines)
                continue
            for i, original in enumerate(lines):
                if not isinstance(original, str) or not original:
                    continue
                chosen = overrides.get((speaker, event, i), preset)
                prefix = presets[chosen]
                decorated = wrap_phrase(original, prefix)
                if decorated != original:
                    lines[i] = decorated
                    changed += 1
                else:
                    skipped += 1
    return changed, skipped, unknown


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path, help="Original 7.0 EN.json / ko-KR.json")
    parser.add_argument("-o", "--output", type=Path, help="Output (defaults to .TextFX.json)")
    parser.add_argument("--dry-run", action="store_true", help="Validate/count only; do not write")
    args = parser.parse_args()
    if not args.input.is_file():
        parser.error(f"Input not found: {args.input}")
    categories, overrides, presets = curated_rules()
    with args.input.open("r", encoding="utf-8-sig") as stream:
        data = json.load(stream)
    if not isinstance(data, dict):
        parser.error("Expected a JSON object")
    before = json.dumps(data, ensure_ascii=False, sort_keys=True)
    changed, skipped, unknown = apply_locale(data, categories, overrides, presets)
    if changed == 0 and skipped == 0:
        parser.error("No recognized dialogue entries; is this really a C:U 7.0 locale?")
    print(f"Updated {changed} dialogue lines. Already tagged: {skipped}. Unmapped: {unknown}.")
    if args.dry_run:
        print("Dry run: source file has not been changed.")
        return 0
    output = args.output or args.input.with_name(args.input.stem + ".TextFX.json")
    if output.resolve() == args.input.resolve():
        parser.error("Refusing to overwrite the original locale JSON")
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="\n") as stream:
        json.dump(data, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
    # Reparse and verify that we saved valid JSON and preserved non-dialogue data.
    saved = json.loads(output.read_text(encoding="utf-8"))
    assert saved.get("name") == data.get("name")
    assert saved.get("character") == data.get("character")
    assert before != json.dumps(data, ensure_ascii=False, sort_keys=True) or skipped > 0
    print(f"Saved {output} ({output.stat().st_size} bytes). Original untouched.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
