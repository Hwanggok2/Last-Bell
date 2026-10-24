"""Copy reviewed v5 portraits/companions; preserve all existing unrelated art entries."""
from pathlib import Path
import argparse, copy, hashlib, json, shutil
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
PACK=ROOT/'Planning/Story/Dragon-v1/Production/Companions-v5'
PORTRAITS=ROOT/'Planning/Story/Dragon-v1/Adventure-v5/portraits'
MANIFEST=ROOT/'Client/Assets/Game/Art/Presentation/manifest.json'
DEST='Assets/Game/Art/Presentation/Clips/Dragon-v5'
UNIT=6.2/720
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def inspect(path,size):
    with Image.open(path) as image:
        pixels=list(image.convert('RGBA').getdata())
        assert image.size==tuple(size),path
        assert {p[3] for p in pixels}=={0,255},path
        assert len({p[:3] for p in pixels if p[3]})<=8,path
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');args=parser.parse_args()
    package=read(PACK/'manifest.json');old=read(MANIFEST)
    assert package['status']=='source-art-reviewed-ready-for-root-import'
    actors=[];textures=[];copies=[];ids={a['id'] for a in package['actors']}
    assert ids=={'companion_'+id for id in ['tori','barun','doto','rina','momo']}
    for source in package['actors']:
        actor=dict(id=source['id'],facesRight=True,height=source['referenceBodyHeightPx']*source['recommendedIntegerScale']*UNIT,referenceHeightPixels=source['referenceBodyHeightPx'],clips=[])
        for clip in source['clips']:
            motion=clip['name'];paths=[]
            assert motion in ['Idle','Attack','Run']
            assert len(clip['frames'])>=(4 if motion=='Idle' else 6)
            assert len(clip['frames'])==len(clip['durationMs']) and min(clip['durationMs'])>0
            if motion=='Attack':assert sum(clip['durationMs'])==600 and sum(clip['durationMs'][:clip['contactFrame']])==180
            for index,name in enumerate(clip['frames']):
                src=PACK/name;inspect(src,clip['canvas'])
                target=f'{DEST}/{source["npcId"]}/{motion}/{index:02}.png';paths.append(target)
                copies.append((src,ROOT/'Client'/target))
                pivot=clip['pivotTopLeft'];w,h=clip['canvas']
                textures.append(dict(path=target,source=src.relative_to(ROOT).as_posix(),sha256=sha(src),width=w,height=h,pivot=dict(x=pivot[0],y=pivot[1])))
            origin=source['contactOriginTopLeft']
            actor['clips'].append(dict(motion=motion,sourceId=f'dragon-v5-{source["npcId"]}-{motion}',sourceJob=clip['sourceGenerationJob'],frames=paths,sourceFrames=clip['sourceFrameIndices'],fps=clip['nominalFps'],loop=clip['loop'],contactFrame=clip['contactFrame'] if clip['contactFrame'] is not None else -1,frameDurationsMs=clip['durationMs'],effectOrigin=dict(x=origin[0],y=origin[1]),note='Reviewed native pixel companion; fixed feet pivot; attack contact at180ms.'))
        actors.append(actor)
    for id in ['on','dam','rin','tori','rina','barun','doto','gib','unknown','momo']:
        src=PACK/'portraits/momo.png' if id=='momo' else PORTRAITS/(id+'.png')
        inspect(src,(24,24));copies.append((src,ROOT/'Client/Assets/Game/Art/Dialogue/Dragon-v5'/(id+'.png')))
    result=copy.deepcopy(old)
    result['actors']=[a for a in old['actors'] if a['id'] not in ids]+actors
    result['textures']=[t for t in old['textures'] if not t['path'].startswith(DEST+'/')]+textures
    assert [a for a in old['actors'] if a['id'] not in ids]==[a for a in result['actors'] if a['id'] not in ids]
    report=dict(status='validated',applied=args.apply,actorIds=sorted(ids),frames=len(textures),portraits=10,sourceSha256=sha(PACK/'manifest.json'),portraitHashes={src.stem:sha(src) for src,dst in copies if '/Dialogue/' in dst.as_posix()})
    if args.apply:
        for src,dst in copies:
            dst.parent.mkdir(parents=True,exist_ok=True)
            if not dst.exists() or sha(src)!=sha(dst):shutil.copy2(src,dst)
        for source,destination in [(PACK/'license.json',ROOT/'Client'/DEST/'LICENSE-PixelLab.json'),(PORTRAITS/'license.json',ROOT/'Client/Assets/Game/Art/Dialogue/Dragon-v5/LICENSE-PixelLab.json')]:
            if source.exists():shutil.copy2(source,destination)
        MANIFEST.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        report['manifestSha256']=sha(MANIFEST)
        (ROOT/'Planning/Production/dragon-v5-art-import.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False))
if __name__=='__main__':main()
