"""Package the completed normal build once; retain all older releases."""
from pathlib import Path
import hashlib
import json
import zipfile

ROOT=Path(__file__).resolve().parents[1]
BUILD=ROOT/'Builds/Dragon-v3'
ZIP=ROOT/'Builds/Dragon-v3-Windows-0.9.2.zip'
sha=lambda data:hashlib.sha256(data).hexdigest()

def main():
    if ZIP.exists():
        raise FileExistsError('Preserve the existing release; do not overwrite it automatically.')
    op=json.loads((ROOT/'Client/Logs/dragon-v3-operation.json').read_text(encoding='utf-8-sig'))
    assert op['operation']=='release-build' and op['status']=='complete',op
    dll=BUILD/'LastBellMvp_Data/Managed/LastBell.Runtime.dll'
    assert b'DragonThemeSmokeProbe' not in dll.read_bytes(), 'Observed code in normal release'
    assert (BUILD/'LastBellMvp.exe').is_file()
    for name in ['PixelLab-Dragon-v3-Player.txt','PixelLab-Dragon-v3-Forest.txt']:
        assert (BUILD/'LastBellMvp_Data/StreamingAssets/ArtLicenses'/name).is_file(), name
    files=[dict(path=p.relative_to(ROOT).as_posix(),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(BUILD.rglob('*')) if p.is_file()]
    temp=ZIP.with_suffix('.zip.tmp')
    with zipfile.ZipFile(temp,'x',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for f in files:
            p=ROOT/f['path'];z.write(p,p.relative_to(BUILD).as_posix())
    with zipfile.ZipFile(temp) as z:
        assert z.testzip() is None and len(z.infolist())==len(files)
        for f in files:
            assert sha(z.read((ROOT/f['path']).relative_to(BUILD).as_posix()))==f['sha256']
    temp.rename(ZIP)
    result=dict(version='0.9.2',dataVersion='mvp1.15.0',buildOptions='None',
                extraScriptingDefines=['LASTBELL_PLAYTEST_092'],builtAtUtc=op['utc'],
                profile='persistentDataPath/Playtests/dragon-0.9.2; explicit save-dir takes precedence',
                firstStart='Existing prologue/selection flow -> Stage1 tutorial; subsequent launches resume this new journey',
                oldUserSavePreserved=True,qaProbeExcluded=True,zipIntegrity='pass',
                executionEvidence='Observed build targeted QA; ordinary release verified as package, not separately played',
                qaReport='Planning/QA/dragon-v3-result-20261024.md',files=files,
                archive=dict(path=ZIP.relative_to(ROOT).as_posix(),bytes=ZIP.stat().st_size,sha256=sha(ZIP.read_bytes())))
    (ROOT/'Planning/Production/dragon-v3-release.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(files=len(files),archive=result['archive'],probeExcluded=True)))

if __name__=='__main__':main()
