#!/usr/bin/env python3
"""Create a ZIP with only the compiled DLL and source/build scripts; NO README."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import hashlib

ROOT=Path(__file__).resolve().parents[1]
DIST=ROOT/'dist'
DIST.mkdir(exist_ok=True)
DLL=ROOT/'src/bin/Release/netstandard2.1/TextFXUnknown.dll'
if not DLL.exists() or DLL.stat().st_size < 4096:
    raise SystemExit('Build DLL not found or too small: '+str(DLL))
OUTPUT=DIST/'TextFX-Unknown-v0.7.4-pre.1.zip'
content={
    'BepInEx/plugins/TextFXUnknown.dll': DLL,
    'source/TextFXUnknown.cs': ROOT/'src/TextFXUnknown.cs',
    'source/TextFXUnknown.Automatic.cs': ROOT/'src/TextFXUnknown.Automatic.cs',
    'source/TextFXUnknown.Curated.cs': ROOT/'src/TextFXUnknown.Curated.cs',
    'source/TextFXUnknown.Extended.cs': ROOT/'src/TextFXUnknown.Extended.cs',
    'source/test_effect_catalog.py': ROOT/'scripts/test_effect_catalog.py',
    'source/test_sustain_effects.py': ROOT/'scripts/test_sustain_effects.py',
    'source/test_geometry_safety.py': ROOT/'scripts/test_geometry_safety.py',
    'source/apply_effects_to_locale.py': ROOT/'scripts/apply_effects_to_locale.py',
    'source/test_locale_converter.py': ROOT/'scripts/test_locale_converter.py',
    'source/test_curated_mapping.py': ROOT/'scripts/test_curated_mapping.py',
    'source/TextFXUnknown.csproj': ROOT/'src/TextFXUnknown.csproj',
    'source/download_references.py': ROOT/'scripts/download_references.py',
    'source/package.py': ROOT/'scripts/package.py',
}
with ZipFile(OUTPUT,'w',ZIP_DEFLATED,compresslevel=9) as archive:
    for arcname, src in content.items():
        assert src.is_file(),src
        if 'readme' in arcname.lower():
            raise RuntimeError('README must not appear inside the release zip')
        archive.write(src,arcname)
with ZipFile(OUTPUT) as archive:
    names=archive.namelist()
    assert not any('readme' in x.lower() for x in names)
    assert archive.testzip() is None
    print('Packaged files:')
    print('\n'.join(names))
print('Created',OUTPUT,OUTPUT.stat().st_size,'bytes')
print('SHA256:',hashlib.sha256(OUTPUT.read_bytes()).hexdigest())