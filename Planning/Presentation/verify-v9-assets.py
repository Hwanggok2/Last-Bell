"""Check preserved sprite cells, playback metadata, and local review links."""
from pathlib import Path
from PIL import Image
import json, re, hashlib
R=Path(__file__).resolve().parent;P=R/'PixelLab';F=P/'clips/hero-v9-sprint-authored'
assets=json.loads((P/'assets.json').read_text(encoding='utf-8'))
c=next(c for c in assets['clips'] if c['id']=='hero-v9-sprint-authored')
assert c['playbackFrames']==list(range(12)) and c['fps']==25
assert c['authoredFrameCount']==12 and c['generatedFrameCount']==0 and not c['inputFrameIncluded']
assert c['pivot']==[47,114]
source=Image.open(F/'workbench-sheet.png').convert('RGBA');packed=Image.open(F/'sheet.png').convert('RGBA')
palette=set(Image.open(P/'v9/sprint-keypose-b.png').convert('RGBA').get_flattened_data())
hashes=[]
for i in range(12):
    frame=Image.open(F/'frames'/f'{i:02}.png').convert('RGBA')
    x=i%6*128;y=i//6*128
    assert frame.tobytes()==source.crop((x,y,x+128,y+128)).tobytes()
    x=i%5*128;y=i//5*128
    assert frame.tobytes()==packed.crop((x,y,x+128,y+128)).tobytes()
    assert all(p[3]==0 or p in palette for p in frame.get_flattened_data())
    box=frame.getbbox();assert box[0]>0 and box[1]>0 and box[2]<128 and box[3]<128
    hashes.append(hashlib.sha256(frame.tobytes()).hexdigest())
assert len(set(hashes))==12
gif=Image.open(F/'edited-preview.gif');duration=0
for i in range(gif.n_frames):gif.seek(i);duration+=gif.info['duration']
assert duration==480
missing=[]
for path in [R/'index.html',R.parent/'index.html',P/'review.html',R/'index-v8.html',P/'review-v8.html']:
    for link in re.findall(r'(?:src|href)=[\"\']([^\"\']+)',path.read_text(encoding='utf-8')):
        if re.match(r'^(https?:|data:|#)',link):continue
        target=(path.parent/link.split('#')[0].split('?')[0]).resolve()
        if not target.exists():missing.append(str(target))
assert not missing,missing
report=dict(scope='Planning assets and local links',uniqueFrames=12,authoredFrames=12,loopMs=duration,pivot=c['pivot'],sourcePalettePreserved=True,packedCellsUnchanged=True,clearCanvasBorders=True,missingLinks=missing,frameHashes=hashes,browserPlaybackVerified=False,unityApplied=False)
(R.parent/'Evidence/Presentation-v9/asset-check.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('12 distinct cells preserved; source palette, 480ms loop, border, pivot and current/archive links verified.')
