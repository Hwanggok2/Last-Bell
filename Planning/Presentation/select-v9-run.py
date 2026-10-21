"""Select a reviewed v9 clip in the planning preview only; raw frames stay intact."""
from pathlib import Path
import json,sys
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent;PL=R/'PixelLab';clip_id=sys.argv[1]
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def write(p,data):p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
job=next(j for j in read(PL/'motion-v9-jobs.json')['jobs'] if j['id']==clip_id)
folder=PL/'clips'/clip_id;n=job['requestedGeneratedFrames'];input_count=int(job.get('inputFrameIncluded',True));order=list(range(input_count,n+input_count))
frames=[Image.open(folder/'frames'/f'{i:02}.png').convert('RGBA') for i in range(n+input_count)]
assert all(im.size==(128,128) for im in frames)
assert (R/'motion-catalog-v9.json').exists(),'Document owner must create the v9 catalog first'
edits=read(PL/'edits.json')
edits['hero-v8-run-consistent'].update(status='rejected',note='사용자 재수정 요청: 원형은 유지했으나 몸을 숙여 달리는 인상이 약함. 이전 비교 자료로 보존.')
edits[clip_id]=dict(status='review',fps=n/.48,loop=True,pivot=[47,114],playbackFrames=order,note='새 전경 키포즈의 후드·옷·종을 보존하고 PixelLab Workbench에서 양팔·양다리 관절 12자세를 직접 설계. 동일한 검을 같은 손에 연결. 00~11을480ms에 재생. 기획 검토 시안, Unity 미적용.')
write(PL/'edits.json',edits)
p=read(R/'proposal.json');p.update(version='presentation-v9',motionCatalog='motion-catalog-v9.json',playerMotionManifest='PixelLab/motion-v9-jobs.json')
p['parallax'].update(selectedRunClip=clip_id,runFrames=n,selectedRunFrames=n,status='새 전경 키포즈 + 관절 12자세 직접 설계 · 기획 검토 시안')
assert p['parallax']['travelMs']==2000 and p['parallax']['runCycleMs']==480
write(R/'proposal.json',p)
for kind in ['motions','effects']:
    s=(R/f'{kind}-v8.js').read_text(encoding='utf-8').replace('hero-v8-run-consistent',clip_id)
    (R/f'{kind}-v9.js').write_text(s,encoding='utf-8')
s=(PL/'pack-clips.py').read_text(encoding='utf-8').replace("c['id']=='hero-v8-run-consistent'",f"c['id']=='{clip_id}'").replace('data=dict(version=8','data=dict(version=9');(PL/'pack-clips.py').write_text(s,encoding='utf-8')
s=(PL/'review.js').read_text(encoding='utf-8').replace("startsWith('hero-v8-')","startsWith('hero-v9-')").replace('hero-v8-run-consistent',clip_id);(PL/'review.js').write_text(s,encoding='utf-8')
s=(PL/'review.html').read_text(encoding='utf-8').replace('MOTION v8','MOTION v9').replace('원본 의상 보존 달리기·기존 회피·피격','몸을 숙인 질주·기존 회피·피격');(PL/'review.html').write_text(s,encoding='utf-8')
s=(R/'check-v8-controls.mjs').read_text(encoding='utf-8').replace('Presentation-v8','Presentation-v9').replace('effects-v8.js','effects-v9.js').replace('motions-v8.js','motions-v9.js');(R/'check-v9-controls.mjs').write_text(s,encoding='utf-8')
# The comparison is an inspection board; it does not repaint or normalize source frames.
items=[('실제 대기 원본',Image.open(PL/'clips/hero-v5-idle/frames/01.png').convert('RGBA')),('이전 v8',Image.open(PL/'clips/hero-v8-run-consistent/frames/00.png').convert('RGBA')),('새 전경 원형',Image.open(PL/'v9/sprint-keypose-b.png').convert('RGBA'))]+[(f'새 질주 {i:02}',frames[i]) for i in order]
cols=5;rows=(len(items)+cols-1)//cols;board=Image.new('RGB',(cols*256,rows*300),'#101c2d');d=ImageDraw.Draw(board);font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',17)
for index,(label,im) in enumerate(items):
    x=index%cols*256;y=index//cols*300;up=im.resize((256,256),Image.Resampling.NEAREST);board.paste(up,(x,y),up);d.text((x+12,y+265),label,font=font,fill='#ffe3a4')
board.save(folder/'identity-contact.png')
print('Selected reviewed v9 clip for planning:',clip_id)
