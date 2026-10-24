"""Validate and assemble planning-only portrait proposal. Does not edit Unity or older assets."""
from pathlib import Path
import json,hashlib,shutil
from PIL import Image,ImageDraw
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[4]
OLD=BASE.parents[1]/'Production/Portraits-v1'
NAMES={'on':'루카','dam':'다렌','rin':'미엘','tori':'토리','rina':'리나','barun':'바룬','doto':'도토','gib':'기블','unknown':'???'}
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    old_hash=json.loads((BASE/'requests/existing-portraits-sha256.json').read_text())
    assert all(sha(OLD/name)==value for name,value in old_hash.items()),'Existing portraits changed'
    jobs=json.loads((BASE/'requests/generation.json').read_text(encoding='utf-8'))
    rows=[]
    for id,name in NAMES.items():
        source=BASE/'raw'/f'{id}.png';im=Image.open(source).convert('RGBA');px=list(im.getdata());colors=sorted({'#%02X%02X%02X'%p[:3] for p in px if p[3]});alpha=sorted({p[3] for p in px})
        assert im.size==(24,24) and alpha==[0,255] and len(colors)<=8,(id,im.size,colors,alpha)
        assert sum(p[3]==0 for p in px)>80,(id,'cutout required')
        dst=BASE/f'{id}.png'
        if not dst.exists() or dst.read_bytes()!=source.read_bytes():shutil.copy2(source,dst)
        old=Image.open(OLD/f'{id}.png').convert('RGBA').resize((24,24),Image.Resampling.NEAREST)
        assert im.tobytes()!=old.tobytes(),id+' is only resized old portrait'
        j=next((j for j in jobs if j['id']==id),None)
        if id=='rin' and (BASE/'rework-rin/generation.json').exists():j=json.loads((BASE/'rework-rin/generation.json').read_text(encoding='utf-8'))
        rows.append(dict(id=id,name=name,file=f'{id}.png',nativeSize=[24,24],palette=colors,colorCount=len(colors),alphaValues=alpha,transparentPixels=sum(p[3]==0 for p in px),boundsTopLeft=list(im.getbbox()),sha256=sha(dst),generationJob=j['result']['structuredContent']['job_id'] if j else None,method='PixelLab Pro Flash native24px redraw' if j else 'PixelLab exact recolor of new Tori silhouette',notSimpleResize=True))
    manifest=dict(schemaVersion=1,revision='Adventure-v5-portraits',status='planning-proposal-source-reviewed',scope='Planning only; no Unity, importer, GameData or existing Portraits-v1 changes.',nativeSize=[24,24],paletteTarget='6–8 maximum; measured5–8visiblecolors, unknown1',displaySuggestion=dict(width=144,height=144,integerScale=6,anchor='dialogue-panel-top-right',background='none',frame=False,notes='Cutout overlaps the upper edge of dialogue panel on its right. Keep dialogue text out of portrait footprint. Actual UI composition is owned by main planning.'),portraits=rows,review='review.png',comparison='comparison.png',license='license.json',unchangedExistingPortraitHashes=True,excluded=['new fifth NPC: identity not yet planned'])
    (BASE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    sheet=Image.new('RGB',(9*160,208),'#DBCFAE');draw=ImageDraw.Draw(sheet)
    comparison=Image.new('RGB',(9*160,376),'#DBCFAE');dc=ImageDraw.Draw(comparison)
    for i,row in enumerate(rows):
        im=Image.open(BASE/row['file']).convert('RGBA').resize((144,144),Image.Resampling.NEAREST)
        sheet.paste(im,(i*160+8,30),im);draw.text((i*160+8,8),row['id'],fill='#202D38');draw.text((i*160+8,184),f"24px / {row['colorCount']} colors",fill='#202D38')
        before=Image.open(OLD/row['file']).convert('RGBA').resize((144,144),Image.Resampling.NEAREST)
        comparison.paste(before,(i*160+8,24),before);comparison.paste(im,(i*160+8,218),im);dc.text((i*160+8,5),row['id']+'  OLD48px',fill='#202D38');dc.text((i*160+8,195),'NEW24px',fill='#202D38')
    sheet.save(BASE/'review.png');comparison.save(BASE/'comparison.png')
    (BASE/'validation.json').write_text(json.dumps(dict(status='passed',count=9,nativeSize=[24,24],binaryAlpha=True,maxColors=8,oldPortraitsUntouched=True,notSimpleResize=True,details=rows),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print([(r['id'],r['colorCount'],r['transparentPixels']) for r in rows])
if __name__=='__main__':main()
