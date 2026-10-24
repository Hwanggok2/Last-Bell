"""Prepare reviewed art fragment only; root agent owns Unity integration."""
import hashlib,json,pathlib
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[1]
source=root/'Planning/Story/Monster-v84/residential'
jobs=json.loads((source/'jobs.json').read_text(encoding='utf-8'))['jobs']
decisions=json.loads((source/'review-decisions.json').read_text(encoding='utf-8'))
fragment={'version':'v84','status':'reviewed-source-fragment-not-yet-Unity-imported','ownership':'Root integration copies sources and merges actors; preserve all unrelated actors and stage10/20 bosses.','textures':[],'actors':[]}
for item in jobs:
    key=item['id'];decision=decisions[key]
    im=Image.open(source/'prepared'/f'{key}.png').convert('RGBA');bbox=im.getbbox()
    pivot={'x':64,'y':bbox[3]};ref=bbox[3]-bbox[1]
    palette={p[:3] for p in im.getdata() if p[3]}
    def texture(src,name):
        picture=Image.open(src).convert('RGBA')
        bounds=picture.getbbox();assert bounds and bounds[0]>0 and bounds[1]>0 and bounds[2]<128 and bounds[3]<96,(src,bounds)
        assert picture.size==(128,96),src
        assert set(picture.getchannel('A').getdata())<={0,255},src
        assert {p[:3] for p in picture.getdata() if p[3]}<=palette,(src,'Color outside actor base palette')
        target=f'Assets/Game/Art/Presentation/Clips/monster-v84/{key}/{name}.png'
        fragment['textures'].append({'path':target,'source':src.relative_to(root).as_posix(),'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'width':128,'height':96,'pivot':pivot})
        return target
    idle=texture(source/'prepared'/f'{key}.png','idle')
    def clip(motion,frames,indices,holds,job,contact=-1,loop=False):
        return {'motion':motion,'sourceId':f'v84-{key}-{motion.lower()}','sourceJob':job,'note':'Native64 pixel clusters preserved on a padded128x96 transparent canvas. Reviewed selected PixelLab frames. Static Idle and shared white-flash Hit; no Death clip.','frames':frames,'sourceFrames':indices,'frameDurationsMs':holds,'fps':25,'loop':loop,'contactFrame':contact,'referenceHeightPixels':0,'lowPoseFrames':[],'recoverFrames':[],'faceAnchors':[],'effectOrigin':{'x':bbox[0]+3,'y':bbox[1]+ref*.4},'staticFallback':False,'legacyStun':False}
    clips=[clip('Idle',[idle],[0],[1000],item['base'],loop=True),clip('Hit',[idle],[0],[120],item['base'])]
    for motion in ['attack','stun']:
        d=decision[motion];folder=item.get(motion+'Dir',motion)
        frames=[texture(source/key/folder/f'{i:02}.png',f'{motion}/{i:02}') for i in d['sourceFrames']]
        clips.append(clip(motion.title(),frames,d['sourceFrames'],d['holdsMs'],item[motion],d.get('contactFrame',-1),loop=motion=='stun'))
        if motion=='attack':clips[-1]['effectOrigin']={key:decision['attack']['contactPoint'][key] for key in ['x','y']}
    fragment['actors'].append({'id':key,'facesRight':False,'height':decision['worldHeight'],'referenceHeightPixels':ref,'clips':clips})
(source/'manifest-fragment.json').write_text(json.dumps(fragment,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'Packaged {len(fragment["actors"])} reviewed actors and {len(fragment["textures"])} hashed native textures; Unity files untouched')
lookup={t['path']:t['source'] for t in fragment['textures']}
preview=[]
for actor in fragment['actors']:
    entry={'id':actor['id'],'clips':{}}
    for clip in actor['clips']:
        entry['clips'][clip['motion']]={'frames':[(root/lookup[p]).relative_to(source).as_posix() for p in clip['frames']],'holds':clip['frameDurationsMs'],'sourceFrames':clip['sourceFrames'],'contact':clip['contactFrame']}
    preview.append(entry)
(source/'preview-data.js').write_text('window.reviewActors='+json.dumps(preview,ensure_ascii=False)+';\n',encoding='utf-8')
