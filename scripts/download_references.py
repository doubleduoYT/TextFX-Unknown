#!/usr/bin/env python3
"""Fetch compile-only references from upstream distributors. Never bundle these in releases."""
from pathlib import Path
import io
import sys
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
REFS = ROOT / 'refs'
REFS.mkdir(exist_ok=True)
TARGETS = ('BepInEx.dll', '0Harmony.dll', 'UnityEngine.CoreModule.dll', 'UnityEngine.dll')


def get_zip(url):
    print('Fetching', url, flush=True)
    request = urllib.request.Request(url, headers={'User-Agent': 'TextFXUnknown-CI/0.3'})
    with urllib.request.urlopen(request, timeout=120) as response:
        data = response.read()
    archive = zipfile.ZipFile(io.BytesIO(data))
    bad = archive.testzip()
    if bad:
        raise RuntimeError('Corrupt download: '+bad)
    return archive


def extract_by_basename(archive, wanted):
    for name in archive.namelist():
        if Path(name).name == wanted:
            data = archive.read(name)
            if len(data) < 4000:
                raise ValueError('Unexpectedly small assembly '+wanted)
            dest=REFS/wanted
            dest.write_bytes(data)
            print('Reference ready:', wanted, len(data), 'bytes', flush=True)
            return True
    return False


def main():
    missing = [name for name in TARGETS if not (REFS/name).exists()]
    if any(x in missing for x in ('BepInEx.dll', '0Harmony.dll')):
        archive = get_zip('https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip')
        for target in ('BepInEx.dll', '0Harmony.dll'):
            if target in missing and not extract_by_basename(archive, target):
                raise FileNotFoundError(target+' not found in BepInEx archive')
    if 'UnityEngine.CoreModule.dll' in missing or 'UnityEngine.dll' in missing:
        versions = ['2022.3.62', '2022.3.60']
        errors=[]
        for version in versions:
            try:
                archive=get_zip('https://unity.bepinex.dev/libraries/'+version+'.zip')
                for target in ('UnityEngine.CoreModule.dll', 'UnityEngine.dll'):
                    if target in missing:
                        extract_by_basename(archive, target)
                if all((REFS/t).exists() for t in ('UnityEngine.CoreModule.dll', 'UnityEngine.dll')):
                    break
                errors.append(version+': missing CoreModule in archive')
            except Exception as e:
                errors.append(version+': '+str(e))
        if not all((REFS/t).exists() for t in ('UnityEngine.CoreModule.dll', 'UnityEngine.dll')):
            raise RuntimeError('Could not obtain Unity reference: '+' | '.join(errors))
    for target in TARGETS:
        if not (REFS/target).exists():
            raise FileNotFoundError(target)
    print('All build references present. They will not be included in the release.')

if __name__=='__main__':
    main()