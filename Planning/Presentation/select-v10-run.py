"""Select reviewed full-body PixelLab frames for the planning board only."""
from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent;PL=R/'PixelLab';ID='hero-v10-blademage-run';F=PL/'clips'/ID
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
assert (R/'index-v9.html').exists() and (R/'motion-catalog-v10.json').exists()
frames=[Image.open(F/'frames'/f'{i:02}.png').convert('RGBA') for i in range(9)]
assert all(f.size==(128,128) for f in frames)
assert len({f.tobytes() for f in frames[:8]})==8
edits=read(PL/'edits.json')
edits['hero-v9-sprint-authored'].update(status='rejected',note='사용자 반려: 고정 몸통과 붙인 팔다리 느낌, 회수발 가독성 부족. 이전 비교 자료로 보존.')
edits[ID]=dict(status='review',fps=16,loop=True,pivot=[60,114],playbackFrames=list(range(8)),note='SimpleGame 검술사의8장·500ms 달리기를 참고한 새 전체 몸체 생성. 검을 뒤로 낮게 잡고 다리 교대·옷자락 반응을 사용. 입력00+생성01~07을 재생, 돌아온 끝08은 중복 정지 없이 제외. 실시간 브라우저·Unity 품질 검증 전 시안.')
write(PL/'edits.json',edits)
jobs=read(PL/'motion-v10-jobs.json');jobs['jobs'][0]['status']='review';write(PL/'motion-v10-jobs.json',jobs)
p=read(R/'proposal.json');p.update(version='presentation-v10',motionCatalog='motion-catalog-v10.json',playerMotionManifest='PixelLab/motion-v10-jobs.json')
p['parallax'].update(selectedRunClip=ID,runFrames=8,selectedRunFrames=8,runCycleMs=500,status='SimpleGame 검술사 동작을 참고한8장·500ms 전체 생성 시안',referenceManifest='PixelLab/v10/reference/source-manifest.json')
assert p['parallax']['travelMs']==2000 and p['sequences']['normal']['totalMs']==3250
write(R/'proposal.json',p)
for name in ['motions','effects']:
    s=(R/f'{name}-v9.js').read_text(encoding='utf-8').replace('hero-v9-sprint-authored',ID)
    (R/f'{name}-v10.js').write_text(s,encoding='utf-8')
s=(PL/'pack-clips.py').read_text(encoding='utf-8').replace("c['id']=='hero-v9-sprint-authored'",f"c['id']=='{ID}'").replace('data=dict(version=9','data=dict(version=10')
(PL/'pack-clips.py').write_text(s,encoding='utf-8')
s=(PL/'review.js').read_text(encoding='utf-8').replace("startsWith('hero-v9-')","startsWith('hero-v10-')").replace('hero-v9-sprint-authored',ID);(PL/'review.js').write_text(s,encoding='utf-8')
s=(PL/'review.html').read_text(encoding='utf-8').replace('MOTION v9','MOTION v10').replace('몸을 숙인 질주·기존 회피·피격','검술사 참고 달리기·기존 회피·피격');(PL/'review.html').write_text(s,encoding='utf-8')
for src,dst in [('render-v9-run.mjs','render-v10-run.mjs'),('encode-v9-run.py','encode-v10-run.py'),('check-v9-controls.mjs','check-v10-controls.mjs')]:
    s=(R/src).read_text(encoding='utf-8').replace('Presentation-v9','Presentation-v10').replace('effects-v9.js','effects-v10.js').replace('motions-v9.js','motions-v10.js')
    if src.startswith('render'):
        s=s.replace('480ms','500ms').replace('Array.from({length:12}','Array.from({length:8}').replace('i*40*.48','i*62.5*.48').replace("label:i*40+'ms'","label:i*62.5+'ms'").replace('runCycleMs,480','runCycleMs,500').replace('runCycleMs:480','runCycleMs:500')
    (R/dst).write_text(s,encoding='utf-8')
items=[('대기 원형',Image.open(PL/'clips/hero-v5-idle/frames/01.png').convert('RGBA')),('새 시작 자세',frames[0])]+[(f'달리기 {i:02}',frames[i]) for i in range(8)]
board=Image.new('RGB',(1280,600),'#101c2d');d=ImageDraw.Draw(board);font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',17)
for i,(label,f) in enumerate(items):
    x=i%5*256;y=i//5*300;im=f.resize((256,256),Image.Resampling.NEAREST);board.paste(im,(x,y),im);d.text((x+12,y+265),label,font=font,fill='#ffe3a4')
board.save(F/'identity-contact.png')
print('Selected 8 source cells at16fps/500ms; existing combat and reward rules preserved.')
