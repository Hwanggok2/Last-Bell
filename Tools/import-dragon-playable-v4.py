"""Validate reviewed player/portrait sources; --apply copies assets and adds only the two new actors."""
from pathlib import Path
import argparse,copy,hashlib,json,shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
PRODUCTION=ROOT/'Planning/Story/Dragon-v1/Production'
PACKAGE=PRODUCTION/'Player-v4'
PORTRAITS=PRODUCTION/'Portraits-v1'
MANIFEST=ROOT/'Client/Assets/Game/Art/Presentation/manifest.json'
DEST='Assets/Game/Art/Presentation/Clips/Dragon-v4'
UNIT=6.2/720
IDS={'player_dragon_daren','player_dragon_miel'}
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def inspect(path,size,max_colors):
    with Image.open(path) as im:
        rgba=im.convert('RGBA');px=list(rgba.getdata())
        assert rgba.size==size,path
        assert {p[3] for p in px}=={0,255},path
        colors={p[:3] for p in px if p[3]};assert len(colors)<=max_colors,(path,len(colors))
        return hashlib.sha256(rgba.tobytes()).hexdigest()
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');args=parser.parse_args()
    source=read(PACKAGE/'manifest.json');portraits=read(PORTRAITS/'manifest.json');old=read(MANIFEST)
    assert source['status']=='reviewed-ready-for-integration' and portraits['status']=='reviewed-ready-for-integration'
    assert {a['id'] for a in source['actors']}==IDS
    expected=read(PACKAGE/'validation.json');audits={(a['actor'],a['motion']):a for a in expected['actors']}
    required={'Idle','Run','Attack','DodgeUp','DodgeBack','DodgeDown','Hit'};actors=[];textures=[];copies=[]
    for a in source['actors']:
        assert set(a['clips'])==required
        actor=dict(id=a['id'],facesRight=True,height=a['referenceHeight']*a['recommendedIntegerScale']*UNIT,referenceHeightPixels=a['referenceHeight'],clips=[])
        for motion,c in a['clips'].items():
            paths=[];unique=set();audit=audits[(a['id'],'Attack' if motion=='DodgeDown' else motion)]
            for i,filename in enumerate(c['frames']):
                src=ROOT/filename;unique.add(inspect(src,(32,32),8));assert sha(src)==audit['frames'][i]['sha256'],src
                target=f'{DEST}/{a["characterId"]}/{motion}/{i:02}.png';paths.append(target);copies.append((src,ROOT/'Client'/target))
                pivot=c['pivotTopLeft'];textures.append(dict(path=target,source=filename,sha256=sha(src),width=32,height=32,pivot=dict(x=pivot[0],y=pivot[1])))
            assert len(paths)>=6 and len(unique)>=3
            assert len(c['frameDurationsMs'])==len(paths) and min(c['frameDurationsMs'])>0
            if motion=='Attack':
                assert sum(c['frameDurationsMs'])==500 and sum(c['frameDurationsMs'][:c['contactFrame']])==200
            origin=c.get('effectOriginTopLeft') or c['pivotTopLeft']
            actor['clips'].append(dict(motion=motion,sourceId=f'dragon-v4-{a["characterId"]}-{motion}',sourceJob=c['sourceJobId'],frames=paths,sourceFrames=c['sourceIndices'],fps=c['fps'],loop=c['loop'],contactFrame=c['contactFrame'],frameDurationsMs=c['frameDurationsMs'],effectOrigin=dict(x=origin[0],y=origin[1]),note='PixelLab class-outfit transfer of reviewed Luka pose/timing. Fixed clip pivot; no per-frame recenter.'))
        actors.append(actor)
    assert {p['id'] for p in portraits['portraits']}=={'on','dam','rin','tori','rina','barun','doto','gib','unknown'}
    for p in portraits['portraits']:
        src=ROOT/p['path'];inspect(src,(48,48),12);assert sha(src)==p['sha256'],src
        copies.append((src,ROOT/'Client'/p['unityPath']))
    for folder in [PACKAGE,PORTRAITS]:assert (folder/'license.json').is_file()
    result=copy.deepcopy(old);result['actors']=[a for a in old['actors'] if a['id'] not in IDS]+actors
    result['textures']=[t for t in old['textures'] if not t['path'].startswith(DEST+'/')]+textures
    preserved=[a for a in old['actors'] if a['id'] not in IDS]
    assert preserved==[a for a in result['actors'] if a['id'] not in IDS]
    report=dict(status='validated',actorIds=sorted(IDS),textureReferences=len(textures),portraits=9,preservedOtherActors=len(preserved),sourceSha256=sha(PACKAGE/'manifest.json'),portraitSourceSha256=sha(PORTRAITS/'manifest.json'),applied=args.apply)
    if args.apply:
        for src,dst in copies:
            dst.parent.mkdir(parents=True,exist_ok=True)
            if not dst.exists() or sha(dst)!=sha(src):shutil.copy2(src,dst)
        for src,dst in [(PACKAGE/'license.json',ROOT/'Client'/DEST/'LICENSE-PixelLab.json'),(PORTRAITS/'license.json',ROOT/'Client/Assets/Game/Art/Dialogue/Dragon-v4/LICENSE-PixelLab.json')]:shutil.copy2(src,dst)
        MANIFEST.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        report['manifestSha256']=sha(MANIFEST)
        (PACKAGE/'import-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False))
if __name__=='__main__':main()
