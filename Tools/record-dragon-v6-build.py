"""Freeze the completed observed v6 build for the dedicated, bounded QA campaign."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))

def entry(path):
    return {'path': path.relative_to(ROOT).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}

def write(name, value):
    (ROOT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

operation = read(ROOT / 'Client/Logs/dragon-v6-operation.json')
assert operation['operation'] == 'observed-build' and operation['status'] == 'complete', operation
catalog = read(ROOT / 'Client/Assets/Game/Data/Generated/mvp-catalog.json')
assert catalog['dataVersion'] == 'mvp1.18.0' and catalog['mvpLastStage'] == 20
assert (ROOT / 'Planning/Production/dragon-v095-client-handoff.md').is_file()
build = ROOT / 'Builds/Dragon-v6-QA'
runtime = build / 'LastBellMvp_Data/Managed/LastBell.Runtime.dll'
assert b'DragonThemeSmokeProbe' in runtime.read_bytes(), 'Observed build required'
paths = set()
for folder in ['Client/Assets/Game/Runtime', 'Client/Assets/Game/Editor', 'Client/Assets/Game/Data/Generated',
               'Client/Assets/Game/Prefabs', 'Client/Assets/Game/Scenes', 'Client/Assets/Game/Art',
               'Client/Assets/StreamingAssets', 'Tools/QA/Dragon-v6']:
    paths.update(p for p in (ROOT / folder).rglob('*') if p.is_file())
paths.update(ROOT / p for p in ['GameData/mvp-catalog.json', 'Client/ProjectSettings/ProjectSettings.asset', 'Tools/QA/Dragon-v3/common.ps1'])
freeze = dict(sourceFrozen=True, version='0.9.6', dataVersion='mvp1.18.0', builtAtUtc=operation['utc'],
              runtimeSha256=entry(runtime)['sha256'], executable=entry(build / 'LastBellMvp.exe'),
              files=[entry(p) for p in sorted(paths)])
write('Planning/Production/dragon-v6-observed-build.json', freeze)
record = dict(version='0.9.6', dataVersion='mvp1.18.0', status='saved-assets-applied-observed-build-ready',
              qaStatus='PENDING', playableStages=[1, 20], saveSchema=9,
              profile='persistentDataPath/Playtests/dragon-0.9.2',
              migration='LastBell.Editor.DragonAdventureMigration',
              observedBuild='Planning/Production/dragon-v6-observed-build.json',
              coreEvidence='Planning/Production/dragon-v095-client-handoff.md',
              changes=['Restored prior inn upgrade layout with companion entry at top right',
                       'Chunky pixel HP bars, large outlined bitmap damage numbers and companion hit flash',
                       'Three independent player skills usable during normal attack and dodge',
                       'Five companion proc skills with level-based chance and attack periods',
                       'Saved 4 ParticleSystems, sky lightning and distinctive companion contacts',
                       'Green actual HP and orange linear trail only after a single hit >= one third maximum HP'],
              limits=['Stages21-50 remain future content', 'Physical OS input and mobile-device checks not performed',
                      'Direct audio listening and device performance not measured'])
write('Planning/Production/dragon-v6-integration.json', record)
print(json.dumps(dict(runtimeSha256=freeze['runtimeSha256'], frozenFiles=len(paths), builtAtUtc=operation['utc'])))
