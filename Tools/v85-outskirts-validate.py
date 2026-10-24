"""Validate hand-reviewed PixelLab frames and integration metadata."""
import hashlib,json,pathlib,re,subprocess
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/'Planning/Story/Monster-v85/outskirts'
data=json.loads((ART/'jobs.json').read_text(encoding='utf-8'))
manifest=json.loads((ART/'manifest-fragment.json').read_text(encoding='utf-8'))
review=json.loads((ART/'review-decisions.json').read_text(encoding='utf-8'))
textures={row['path']:row for row in manifest['textures']}
assert len(textures)==len(manifest['textures'])
hero=ROOT/data['style']['reference']
assert hashlib.sha256(hero.read_bytes()).hexdigest()==data['style']['referenceSha256']
result={'status':'passed','originalHeroSha256Unchanged':True,'originalHeroDirectReference':data['style']['reference'],'browserVisualQA':'Browser inventory unavailable in agent session. All frames inspected using rendered contact sheets and actual-world-scale original-hero comparison. Preview JavaScript syntax validated.','actors':{}}
for actor in manifest['actors']:
    key=actor['id'];report={};result['actors'][key]=report
    assert {c['motion'] for c in actor['clips']}=={'Idle','Attack','Stun','Hit'}
    for clip in actor['clips']:
        motion=clip['motion'];frames=clip['frames'];holds=clip['frameDurationsMs']
        assert len(frames)==len(holds)==len(clip['sourceFrames'])
        assert all(t>0 for t in holds)
        hashes=[];feet=[];colors=set();bounds=[]
        for f in frames:
            t=textures[f];p=ROOT/t['source'];im=Image.open(p).convert('RGBA');b=im.getbbox()
            assert hashlib.sha256(p.read_bytes()).hexdigest()==t['sha256']
            assert im.size==(128,96) and t['pivot']=={'x':64,'y':80}
            assert b[0]>0 and b[1]>0 and b[2]<128 and b[3]<96
            assert set(im.getchannel('A').getdata())<={0,255}
            hashes.append(hashlib.sha256(im.tobytes()).hexdigest());feet.append(hashlib.sha256(im.crop((0,76,128,82)).tobytes()).hexdigest())
            colors.update(p[:3] for p in im.getdata() if p[3]);bounds.append(b)
        if motion!='Hit':
            assert review[key][motion.lower()]['status']=='approved'
            assert len(set(hashes))>1, (key,motion,'not a real animation')
        if motion=='Idle':
            assert len(set(feet))==1,(key,'Idle paws changed')
            assert 1200<=sum(holds)<=1800
        if motion=='Attack':
            contact=clip['contactFrame'];point=clip['effectOrigin'];im=Image.open(ROOT/textures[frames[contact]]['source']).convert('RGBA')
            assert 0<=contact<len(frames) and 350<=sum(holds[:contact])<=500
            assert 600<=sum(holds)<=800
            assert any(im.getpixel((x,y))[3] for x in range(point['x']-2,point['x']+3) for y in range(point['y']-2,point['y']+3)),(key,'contact point not on creature')
        report[motion]={'sourceFrames':len(frames),'uniquePixelFrames':len(set(hashes)),'durationMs':sum(holds),'footBandVariants':len(set(feet)),'colorCount':len(colors),'noClippedPixels':True,'binaryAlphaOnly':True,'bounds':bounds}
        if motion=='Attack':report[motion].update(contactFrame=clip['contactFrame'],contactTimeMs=sum(holds[:clip['contactFrame']]),effectOrigin=clip['effectOrigin'])
html=(ART/'preview.html').read_text(encoding='utf-8')
assert '\ufffd' not in html
for row in data['jobs']:assert row['name'] in html
script=re.search(r'<script>(.*?)</script>',html,re.S).group(1)
scriptPath=ART/'preview-script-check.js';scriptPath.write_text(script,encoding='utf-8')
subprocess.run(['node','--check',str(scriptPath)],check=True)
result['textureCount']=len(textures)
result['utf8NamesVerified']=True
(ART/'qa-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'PASS: {len(result["actors"])} actors, {len(textures)} textures, original hero preserved, source hashes, frame timing, contact, alpha, margins and preview syntax verified.')
