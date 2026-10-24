"""Package only inspected source frames; no Unity/runtime/shared manifest edits."""
import pathlib,json,hashlib
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[1]
source=ROOT/'Planning/Story/Monster-v85/residential'
jobs=json.loads((source/'jobs.json').read_text(encoding='utf-8'))['jobs']
review=json.loads((source/'review-decisions.json').read_text(encoding='utf-8'))
fragment={'version':'v85','status':'reviewed-source-fragment-not-yet-Unity-imported','ownership':'Root integrates textures and actors; preserve unrelated art and both existing bosses.','textures':[],'actors':[]}
preview=[]
textureCache={}
for item in jobs:
    key=item['id'];decision=review[key];assert decision['accepted'] is True
    base=Image.open(source/'prepared'/f'{key}.png').convert('RGBA');bbox=base.getbbox();ref=bbox[3]-bbox[1];pivot={'x':96,'y':130}
    def texture(path,name):
        im=Image.open(path).convert('RGBA');b=im.getbbox();assert im.size==(192,160),(path,im.size)
        assert b and b[0]>0 and b[1]>0 and b[2]<192 and b[3]<160,(path,b)
        assert set(im.getchannel('A').getdata())<={0,255},path
        target=f'Assets/Game/Art/Presentation/Clips/monster-v85/{key}/{name}.png'
        if target in textureCache:
            assert textureCache[target]==path
            return target
        textureCache[target]=path
        fragment['textures'].append({'path':target,'source':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'width':192,'height':160,'pivot':pivot})
        return target
    baseTexture=texture(source/'prepared'/f'{key}.png','base')
    clips=[];previewEntry={'id':key,'height':decision['worldHeight'],'referenceHeightPixels':ref,'clips':{}}
    def clip(motion,paths,indices,holds,job,contact=-1,loop=False,effect=None):
        assert len(paths)==len(indices)==len(holds)
        return {'motion':motion,'sourceId':f'v85-{key}-{motion.lower()}','sourceJob':job,'note':'Original hero128 direct style reference. Coarse chunky proportions; native PixelLab frames on a transparent192x160 motion canvas. White-flash Hit; no Death.','frames':paths,'sourceFrames':indices,'frameDurationsMs':holds,'fps':25,'loop':loop,'contactFrame':contact,'referenceHeightPixels':0,'lowPoseFrames':[],'recoverFrames':[],'faceAnchors':[],'effectOrigin':effect or {'x':bbox[0]+3,'y':bbox[1]+ref*.4},'staticFallback':False,'legacyStun':False}
    for motion in ['idle','attack','stun']:
        selection=decision[motion];sourceClip=item['clips'][motion];indices=selection['sourceFrames'];paths=[];previewPaths=[]
        for i in indices:
            src=source/key/sourceClip['folder']/f'{i:02}.png';paths.append(texture(src,f'{motion}/{i:02}'));previewPaths.append(src.relative_to(source).as_posix())
        effect={k:selection['contactPoint'][k] for k in ['x','y']} if motion=='attack' else None
        entry=clip(motion.title(),paths,indices,selection['holdsMs'],sourceClip['job'],selection.get('contactFrame',-1),loop=motion!='attack',effect=effect)
        if motion=='attack':entry['contactAtMs']=selection['contactAtMs']
        if sourceClip.get('registration'):entry['sourceRegistration']=sourceClip['registration']
        if selection.get('excludedFrames'):entry['excludedSourceFrames']=selection['excludedFrames']
        clips.append(entry)
        previewEntry['clips'][motion.title()]={'frames':previewPaths,'holds':selection['holdsMs'],'sourceFrames':indices,'contact':selection.get('contactFrame',-1)}
    clips.append(clip('Hit',[baseTexture],[0],[120],item['base']))
    previewEntry['clips']['Hit']={'frames':[f'prepared/{key}.png'],'holds':[120],'sourceFrames':[0],'contact':-1}
    fragment['actors'].append({'id':key,'facesRight':False,'height':decision['worldHeight'],'referenceHeightPixels':ref,'clips':clips});preview.append(previewEntry)
(source/'manifest-fragment.json').write_text(json.dumps(fragment,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(source/'preview-data.js').write_text('window.reviewActors='+json.dumps(preview,ensure_ascii=False)+';\n',encoding='utf-8')
print(f'Packaged {len(fragment["actors"])} actors / {len(fragment["textures"])} validated native textures')
