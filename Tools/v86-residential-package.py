"""Add reviewed Idle clips; retain each existing Attack/Stun record byte-for-byte structurally."""
import pathlib,json,hashlib,copy,os
from PIL import Image,ImageDraw
R=pathlib.Path(__file__).resolve().parents[1];P=R/'Planning/Story/Monster-v86/residential'
snapshot=json.loads((P/'existing-snapshot.json').read_text(encoding='utf-8'))
jobs={j['id']:j for j in json.loads((P/'jobs.json').read_text(encoding='utf-8'))['jobs']}
selections=json.loads((P/'review-decisions.json').read_text(encoding='utf-8'))
textures={t['path']:t for t in snapshot['textures']};out={'version':'v86','status':'reviewed-fragment-root-integrates','textures':[],'actors':[]};preview=[];qa={}
for original in snapshot['actors']:
    key=original['id'];a=copy.deepcopy(original);j=jobs[key];s=selections[key];assert s['accepted']
    idle=next(c for c in a['clips'] if c['motion']=='Idle');oldIdle=copy.deepcopy(idle);baseMeta=textures[oldIdle['frames'][0]];base=Image.open(R/'Client'/oldIdle['frames'][0]).convert('RGBA')
    frames=[];paths=[];hashes=[];registered=[];seen=set()
    for sourceIndex in s['sourceFrames']:
        path=P/key/j.get('folder','idle')/f'{sourceIndex:02}.png';im=Image.open(path).convert('RGBA');b=im.getbbox()
        assert im.size==base.size==(128,128) and b and b[0]>0 and b[1]>0 and b[2]<128 and b[3]<128,(path,b)
        assert set(im.getchannel('A').getdata())<={0,255},path
        target=f'Assets/Game/Art/Presentation/Clips/monster-v86/{key}/idle/{sourceIndex:02}.png'
        if target not in seen:
            out['textures'].append({'path':target,'source':path.relative_to(R).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'width':128,'height':128,'pivot':baseMeta['pivot']});seen.add(target)
        frames.append(target);paths.append(path.relative_to(P).as_posix());hashes.append(hashlib.sha256(im.tobytes()).hexdigest());registered.append(im)
    assert len(frames)==len(s['holdsMs']) and len(set(hashes))>=2
    idle.update({'frames':frames,'sourceFrames':s['sourceFrames'],'frameDurationsMs':s['holdsMs'],'sourceId':key+'-v86-idle','sourceJob':j['jobId'],'note':'PixelLab natural Idle from unchanged approved original base. See v86 residential review-decisions.json for native-frame selection.','fps':round(1000/(sum(s['holdsMs'])/len(frames)),3),'loop':True,'staticFallback':False})
    assert [c for c in a['clips'] if c['motion']!='Idle']==[c for c in original['clips'] if c['motion']!='Idle']
    out['actors'].append(a)
    prev={'id':key,'name':s['displayName'],'stage':j['stage'],'height':a['height'],'referenceHeightPixels':a['referenceHeightPixels'],'clips':{}}
    for c in a['clips']:
        fs=c['frames'] or oldIdle['frames'];holds=c.get('frameDurationsMs') or [round(1000/max(c.get('fps',1),1))]*len(fs)
        ps=paths if c['motion']=='Idle' else [os.path.relpath(R/'Client'/f,P).replace('\\','/') for f in fs]
        prev['clips'][c['motion']]={'frames':ps,'holds':holds,'sourceFrames':c.get('sourceFrames') or list(range(len(fs))),'contact':c['contactFrame']}
        if not c['frames']:prev['clips'][c['motion']]['sourceFrames']=[0]
    prev['clips']['Hit']={'frames':[os.path.relpath(R/'Client'/oldIdle['frames'][0],P).replace('\\','/')],'holds':[120],'sourceFrames':[0],'contact':-1}
    preview.append(prev)
    floor=baseMeta['pivot']['y']-1;first=registered[0];last=registered[-1];ba=first.getchannel('A')
    qa[key]={'idleSourceFrames':s['sourceFrames'],'idleUniqueFrames':len(set(hashes)),'idleUniqueSilhouettes':len({hashlib.sha256(im.getchannel('A').tobytes()).hexdigest() for im in registered}),'idleTotalMs':sum(s['holdsMs']),'dimensions':[128,128],'pivot':baseMeta['pivot'],'floorY':floor,'floorAlphaChangesFromFirstSelectedFrame':[sum(ba.getpixel((x,floor))!=im.getchannel('A').getpixel((x,floor)) for x in range(128)) for im in registered],'loopSeamAlphaChanges':sum(x!=y for x,y in zip(first.getchannel('A').getdata(),last.getchannel('A').getdata())),'binaryAlpha':True,'edgeClipping':False,'attackAndStunUnchanged':True,'legacyStunPreserved':any(c.get('legacyStun') for c in original['clips'])}
    sheet=Image.new('RGB',(1088,((len(registered)+3)//4)*294+30),'#192c36');dr=ImageDraw.Draw(sheet);dr.text((8,8),key+' SELECTED IDLE / 1600ms',fill='white');gif=[]
    for n,im in enumerate(registered):
        x=n%4*272;y=n//4*294+30;up=im.resize((256,256),Image.Resampling.NEAREST);sheet.paste(up,(x+8,y+20),up);dr.text((x+8,y),str(n)+' source '+str(s['sourceFrames'][n]),fill='#f9d296')
        bg=Image.new('RGB',(256,256),'#192c36');bg.paste(up,(0,0),up);gif.append(bg)
    sheet.save(P/f'{key}-idle-selected.png');gif[0].save(P/f'{key}-idle-selected.gif',save_all=True,append_images=gif[1:],duration=s['holdsMs'],loop=0)
(P/'manifest-fragment.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(P/'preview-data.js').write_text('window.reviewActors='+json.dumps(preview,ensure_ascii=False)+';\n',encoding='utf-8')
(P/'selected-qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for actor in preview:
    for motion,c in actor['clips'].items():
        assert len(c['frames'])==len(c['holds'])==len(c['sourceFrames']),(actor['id'],motion)
        assert all((P/f).is_file() for f in c['frames']),(actor['id'],motion)
print(f'{len(out["actors"])} existing actors, {len(out["textures"])} new Idle textures. All Attack/Stun records preserved.')
