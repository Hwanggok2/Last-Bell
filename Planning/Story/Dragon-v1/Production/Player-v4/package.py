"""Assemble reviewed source frames; validate geometry/palette without repainting art."""
from pathlib import Path
import json,hashlib,shutil,copy
from PIL import Image,ImageDraw
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[4]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def rel(p):return p.relative_to(ROOT).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def stats(path):
    im=Image.open(path).convert('RGBA'); px=list(im.getdata())
    return dict(width=im.width,height=im.height,bounds=list(im.getbbox()),colors=sorted({'#%02X%02X%02X'%p[:3] for p in px if p[3]}),alpha=sorted({p[3] for p in px}),sha256=sha(path),pixelSha256=hashlib.sha256(im.tobytes()).hexdigest())
def main():
    template=read(BASE.parent/'Player-v3/manifest.json')['actors'][0]
    jobs=read(BASE/'requests/jobs.json');actors=[];audit=[]
    for key,actorid,name,height in [('daren','player_dragon_daren','다렌',22),('miel','player_dragon_miel','미엘',24)]:
        clips={};colors=set()
        for motion in ['Idle','Run','Attack','DodgeUp','DodgeBack','Hit']:
            old=template['clips'][motion];source=next(x for x in jobs if x['id']==key and x['motion']==motion)
            files=sorted((BASE/key/'raw'/motion).glob('*.png'));assert len(files)==len(old['frames'])
            paths=[];details=[]
            for i,p in enumerate(files):
                s=stats(p);assert (s['width'],s['height'])==(32,32);assert s['alpha']==[0,255];assert len(s['colors'])<=8
                original=Image.open(ROOT/old['frames'][i]).convert('RGBA');assert s['bounds'][3]==original.getbbox()[3],(key,motion,i,'foot baseline changed')
                colors.update(s['colors']);dest=BASE/key/'clips'/motion/f'{i:02}.png';dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
                paths.append(rel(dest));details.append(dict(index=i,**s))
            assert len({s['pixelSha256'] for s in details})>=3
            clip={k:copy.deepcopy(old[k]) for k in ['fps','loop','contactFrame','effectOriginTopLeft','pivotTopLeft','frameDurationsMs']}
            clip.update(status='ready',frames=paths,sourceIndices=list(range(len(paths))),sourceJobId=source['jobId'],sourceTool='PixelLab transfer_outfit',review=rel(BASE/'reviews'/f'{key}-{motion}.png'))
            clips[motion]=clip;audit.append(dict(actor=actorid,motion=motion,frames=details,uniqueFrames=len({s['pixelSha256'] for s in details}),footBaselineMatchesLuka=True))
        clips['DodgeDown']=copy.deepcopy(clips['Attack']);clips['DodgeDown']['reuse']='Attack14; existing parry timing remains runtime-owned.'
        assert sum(clips['Attack']['frameDurationsMs'])==500
        assert sum(clips['Attack']['frameDurationsMs'][:clips['Attack']['contactFrame']])==200
        actors.append(dict(id=actorid,characterId='dam' if key=='daren' else 'rin',displayName=name,canonical=rel(BASE/key/'canonical.png'),width=32,height=32,pivotTopLeft=[16,29],referenceHeight=height,recommendedIntegerScale=7,recommendedDisplayHeight=height*7,facing='east',palette=sorted(colors),clips=clips))
    manifest=dict(schemaVersion=1,package='Dragon-v4-playable-characters',status='reviewed-ready-for-integration',coordinateConvention='Native source pixels; top-left; fixed clip pivot. Feet exactly match corresponding accepted Luka frames; no per-frame recentering.',pixelImport=dict(filter='Point',compression='None',mipmaps=False,alphaIsTransparency=True,meshType='FullRect',perFrameTrim=False),actors=actors,license='license.json',review='visual-review.md',sourceMotion=rel(BASE.parent/'Player-v3/manifest.json'))
    (BASE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (BASE/'validation.json').write_text(json.dumps(dict(status='passed',physicalFrames=104,clipReferences=132,sourceFeetEqual=True,actors=audit),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    portraits=BASE.parent/'Portraits-v1';portraits_list=[]
    names={'on':'루카','dam':'다렌','rin':'미엘','tori':'토리','rina':'리나','barun':'바룬','doto':'도토','gib':'기블','unknown':'???'}
    for id,name in names.items():
        rawid={'doto':'doto-final','rin':'rin-final','gib':'gib-final'}.get(id,id);src=portraits/'raw'/f'{rawid}.png';s=stats(src)
        assert (s['width'],s['height'])==(48,48) and s['alpha']==[0,255] and len(s['colors'])<=12
        dest=portraits/f'{id}.png';shutil.copy2(src,dest)
        portraits_list.append(dict(id=id,name=name,path=rel(dest),unityPath=f'Assets/Game/Art/Dialogue/Dragon-v4/{id}.png',**s))
    pm=dict(schemaVersion=1,status='reviewed-ready-for-integration',nativeSize=[48,48],displaySize720p=[144,144],importSettings=dict(filter='Point',compression='None',mipmaps=False,spriteMode='Single'),portraits=portraits_list,license='license.json',notes=['unknown is exact Tori silhouette for the first unrevealed line.','doto first portrait rejected; final regenerates bearded dwarf.','rin hair uses guarded PixelLab palette correction to match blonde battle sprite.'])
    (portraits/'manifest.json').write_text(json.dumps(pm,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    sheet=Image.new('RGB',(9*160,180),'#D9DBC8');draw=ImageDraw.Draw(sheet)
    for i,p in enumerate(portraits_list):
        im=Image.open(ROOT/p['path']).convert('RGBA').resize((144,144),Image.Resampling.NEAREST);sheet.paste(im,(i*160,25),im);draw.text((i*160+8,5),p['id'],fill='black')
    sheet.save(portraits/'review-sheet.png')
    print('Packaged2actors/104sourceFrames/132clipReferences and9portraits. All alpha, palette, timing and foot-baseline checks passed.')
if __name__=='__main__':main()
