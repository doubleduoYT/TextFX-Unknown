#!/usr/bin/env python3
"""Test direct localization rewriting without shipping original game dialogue."""
import importlib.util
import json
import pathlib
import subprocess
import sys
import tempfile

root=pathlib.Path(__file__).resolve().parents[1]
script=root/"scripts"/"apply_effects_to_locale.py"
spec=importlib.util.spec_from_file_location("dialogue_converter",str(script))
module=importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
groups,exceptions,presets=module.curated_rules()
assert len(groups)==177 and len(exceptions)==194 and len(presets)==61
N=max([0]+[i for (_, _, i) in exceptions])+1
fake=[{} for _ in range(3)]
for (ch,event) in groups:
    max_index=max([i for (c,e,i) in exceptions if c==ch and e==event] or [0])
    fake[ch][event]=[f"example {ch}:{event}:{i}" for i in range(max_index+1)]
data={"name":"Synthetic","description":"non-dialogue field left untouched","character":fake,"unrelated":{"ok":True}}
original=json.loads(json.dumps(data))
changed,skipped,unknown=module.apply_locale(data,groups,exceptions,presets)
assert changed==sum(len(x) for ch in fake for x in ch.values())
assert skipped==unknown==0
assert data["description"]==original["description"] and data["unrelated"]==original["unrelated"]
assert all(line.startswith("<") for ch in data["character"] for values in ch.values() for line in values)
n2,s2,u2=module.apply_locale(data,groups,exceptions,presets)
assert n2==0 and s2==changed and u2==0, "second invocation must not duplicate tags"
# Existing TMP tags must not be destroyed.
assert module.wrap_phrase("<color=red>Hello</color>","<bounce=0.5>").startswith("<bounce=0.5><color=red>")
with tempfile.TemporaryDirectory() as tmp:
    src=pathlib.Path(tmp)/"ko-KR.json"
    dst=pathlib.Path(tmp)/"ko-KR.TextFX.json"
    src.write_text(json.dumps(original,ensure_ascii=False),encoding="utf-8")
    p=subprocess.run([sys.executable,str(script),str(src),"-o",str(dst)],capture_output=True,text=True)
    assert p.returncode==0,p.stderr
    assert dst.is_file()
    produced=json.loads(dst.read_text(encoding="utf-8"))
    assert produced["character"]==data["character"]
    assert json.loads(src.read_text(encoding="utf-8"))==original, "original must remain unchanged"
print("PASS: curated direct JSON rewrite, 177 events, 194 override rules, rich tags, idempotence, original preserved")
