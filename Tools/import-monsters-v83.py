"""Append reviewed PixelLab clips to the existing saved-art manifest."""
import hashlib,json,pathlib,shutil
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[1]
source=root/'Planning/Story/Monster-v83'
path=root/'Client/Assets/Game/Art/Presentation/manifest.json'
d=json.loads(path.read_text(encoding='utf-8'))
backup=root/'Planning/Evidence/Feedback-v83/before/art-manifest.json'
if not backup.exists():shutil.copy2(path,backup)
jobs=json.loads((source/'jobs.json').read_text(encoding='utf-8'))['jobs']
heights={'wild_dog':.92,'grey_wolf':1.20,'brown_bear':1.85,'sewer_rat':.80,'drowned_resident':1.60,'water_wraith':1.80}
contacts={'wild_dog':9,'grey_wolf':8,'brown_bear':8,'sewer_rat':8,'drowned_resident':9,'water_wraith':7}
review=[]
for item in jobs:
    key=item['id'];base=source/f'raw/{key}.png';im=Image.open(base).convert('RGBA');bbox=im.getbbox()
    pivot={'x':64,'y':bbox[3]};ref=bbox[3]-bbox[1]
    def copy_frame(src,name):
        target=root/f'Client/Assets/Game/Art/Presentation/Clips/monster-v83/{key}/{name}.png'
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,target)
        tex={'path':target.relative_to(root/'Client').as_posix(),'source':src.relative_to(root).as_posix(),
             'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'width':128,'height':128,'pivot':pivot}
        d['textures']=[t for t in d['textures'] if t['path']!=tex['path']];d['textures'].append(tex)
        return tex['path']
    idle=copy_frame(base,'idle')
    def clip(motion,frames,indices,holds,job,contact=-1,loop=False):
        return {'motion':motion,'sourceId':f'v83-{key}-{motion.lower()}','sourceJob':job,
          'note':'Reviewed native PixelLab frames. Static Idle; damage uses the shared white-flash shader; no death clip.',
          'frames':frames,'sourceFrames':indices,'frameDurationsMs':holds,'fps':25,'loop':loop,'contactFrame':contact,
          'referenceHeightPixels':0,'lowPoseFrames':[],'recoverFrames':[],'faceAnchors':[],
          'effectOrigin':{'x':32,'y':bbox[1]+ref*.4},'staticFallback':False,'legacyStun':False}
    clips=[clip('Idle',[idle],[0],[1000],item['base'],loop=True),clip('Hit',[idle],[0],[120],item['base'])]
    for motion,count in [('Attack',17),('Stun',9)]:
        frames=[copy_frame(source/f'raw/{key}/{motion.lower()}/{i:02}.png',f'{motion.lower()}/{i:02}') for i in range(count)]
        holds=[40]*count if motion=='Attack' else [85]*count
        contact=contacts[key] if motion=='Attack' else -1
        if contact>=0:holds[contact]=60
        clips.append(clip(motion,frames,list(range(count)),holds,item[motion.lower()],contact,loop=motion=='Stun'))
    actor={'id':key,'facesRight':False,'height':heights[key],'referenceHeightPixels':ref,'clips':clips}
    d['actors']=[a for a in d['actors'] if a['id']!=key];d['actors'].append(actor)
    review.append({'id':key,'baseBounds':bbox,'pivot':pivot,'height':heights[key],'attackContactFrame':contacts[key],
                   'attackDurationMs':700,'attackContactMs':contacts[key]*40,'review':'All frames inspected: silhouette, left facing, consistent limbs, strike and recovery, no added death or idle loop.'})
path.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(source/'review.json').write_text(json.dumps(review,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Added 6 actors / 162 textures. Preserved all existing actors and their clips.')
