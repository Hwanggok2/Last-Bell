"""Record the saved v5 integration and immutable observed-build handoff."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
def write(path, data):
    (ROOT / path).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
def entry(path):
    return dict(path=path.relative_to(ROOT).as_posix(), sha256=hashlib.sha256(path.read_bytes()).hexdigest())

op = read(ROOT / 'Client/Logs/dragon-v5-operation.json')
assert op['operation'] == 'observed-build' and op['status'] == 'complete', op
build = ROOT / 'Builds/Dragon-v5-QA'
runtime = build / 'LastBellMvp_Data/Managed/LastBell.Runtime.dll'
assert b'DragonThemeSmokeProbe' in runtime.read_bytes(), 'Observed build required'
paths = set()
for folder in ['Client/Assets/Game/Runtime', 'Client/Assets/Game/Editor', 'Client/Assets/Game/Data/Generated',
               'Client/Assets/Game/Prefabs', 'Client/Assets/Game/Scenes', 'Client/Assets/Game/Art',
               'Client/Assets/StreamingAssets', 'Tools/QA/Dragon-v5']:
    paths.update(p for p in (ROOT / folder).rglob('*') if p.is_file())
paths.update(ROOT / p for p in ['GameData/mvp-catalog.json', 'Client/ProjectSettings/ProjectSettings.asset'])
freeze = dict(sourceFrozen=True, version='0.9.4', dataVersion='mvp1.17.0',
              builtAtUtc=op['utc'], runtimeSha256=entry(runtime)['sha256'],
              executable=entry(build / 'LastBellMvp.exe'), files=[entry(p) for p in sorted(paths)])
write('Planning/Production/dragon-v5-observed-build.json', freeze)
record = dict(version='0.9.4', dataVersion='mvp1.17.0', status='saved-assets-applied-observed-build-ready',
              qaStatus='PENDING', playableStages=[1,20], saveSchema=9,
              profile='persistentDataPath/Playtests/dragon-0.9.2',
              migration='LastBell.Editor.DragonAdventureMigration',
              observedBuild='Planning/Production/dragon-v5-observed-build.json',
              coreEvidence='Planning/Production/dragon-v094-client-handoff.md',
              coreAssertions=217, dataTests=28, unityEditModePassed=198,
              changes=['Detached speaker badge; player portrait left, NPC right; revised Miel 24px portrait',
                       'Five-stat inn including per-clear recovery; no boss full heal',
                       'Companion encounters, formation, upgrade, services and actual support attacks',
                       'Five companions with 98 authored Idle/Run/Attack frames',
                       'Enemy retreat/hold/advance tied to actual impact; sequential Stage5 input tutorial',
                       'Four saved ParticleSystems for skill contact, shield, actual stun, support/recovery',
                       'Skill and companion effects follow confirmed events and authoritative clocks'],
              limits=['Stages21-50 remain future content', 'Physical OS input deferred',
                      'Mobile device performance/touch not run', 'Direct audio listening not run'],
              artImport=read(ROOT/'Planning/Production/dragon-v5-art-import.json'))
write('Planning/Production/dragon-v5-integration.json', record)
print(json.dumps(dict(runtimeSha256=freeze['runtimeSha256'], frozenFiles=len(paths), builtAtUtc=op['utc'])))
