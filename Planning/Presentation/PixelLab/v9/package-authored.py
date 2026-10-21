"""Package unmodified Workbench sprite cells and record actual generation outcomes."""
from pathlib import Path
import json,shutil
from PIL import Image
R=Path(__file__).resolve().parent;PL=R.parent
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def write(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
folder=PL/'clips/hero-v9-sprint-authored';(folder/'frames').mkdir(exist_ok=True)
source=folder/'workbench-sheet.png'
if not source.exists():shutil.copy2(folder/'sheet.png',source)
im=Image.open(source).convert('RGBA');assert im.size==(768,256)
for i in range(12):
    x=i%6*128;y=i//6*128;im.crop((x,y,x+128,y+128)).save(folder/'frames'/f'{i:02}.png')
jobs=read(PL/'motion-v9-jobs.json')
for j in jobs['jobs']:
    if j['id']=='hero-v9-sprint-b':j['status']='rejected'
if not any(j['id']=='hero-v9-sprint-authored' for j in jobs['jobs']):
    jobs['jobs'].append(dict(id='hero-v9-sprint-authored',name='온 · 몸을 숙인 질주',jobId='drawing:75cf1cc9-ad20-46b0-a8c0-50929c57b623',requestedGeneratedFrames=12,inputFrameIncluded=False,method='PixelLab pixelart_workbench: new keypose + authored articulated limbs',fps=25,loop=True,status='review',input='v9/sprint-keypose-b.png'))
for j in jobs['jobs']:
    if j['id']=='hero-v9-sprint-authored':j['jobId']='drawing:a37e3f5e-26e9-40b5-a1ef-115fa3741489'
write(PL/'motion-v9-jobs.json',jobs)
edits=read(PL/'edits.json');edits['hero-v9-sprint-b']=dict(status='rejected',note='검이 중간 프레임에서 작아졌다 커지고 공격처럼 흔들려 제외. 원본 비교 자료로 보존.')
write(PL/'edits.json',edits)
p=read(R/'generation-requests.json')
for a in p['animationAttempts']:
    if a['jobId']=='24e1fe78-a558-4441-94c3-426ed2bc27dc':a.update(status='rejected',review='Sword shrinks in frames03-07 then grows at08-09; weapon changes shape and looks like an attack. Not selected.')
p['authoredAnimation']=dict(tool='pixelart_workbench',drawingId='75cf1cc9-ad20-46b0-a8c0-50929c57b623',status='review',sourceKeypose='sprint-keypose-b.png',recipe='authored-run-recipe.json',deliveredRecipe='../clips/hero-v9-sprint-authored/recipe.json',clipId='hero-v9-sprint-authored',method='Copy the new forward-lean hood, clothing and waist bell; author 12 upper/lower arm and leg poses with one rigid sword-hand source patch. No per-frame scaling or normalization.',frameCount=12,durationMs=480,bodyBobPx=1)
p['authoredAnimation'].update(drawingId='a37e3f5e-26e9-40b5-a1ef-115fa3741489',previousDrawingId='75cf1cc9-ad20-46b0-a8c0-50929c57b623',revision='Remove fixed cream pixels from old far arm in hood mask; shift both arms, gloves and sword six frames against leg cycle.')
c=read(R/'opposite-keypose-c-request.json')
if not any(a['jobId']==c['jobId'] for a in p['keyposeAttempts']):p['keyposeAttempts'].append(c)
write(R/'generation-requests.json',p)
print('Packaged 12 unchanged Workbench frames; rejected raw AI loops retained.')
