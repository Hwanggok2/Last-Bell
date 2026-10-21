"""Package PixelLab source-part artwork and update only the planning preview."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json
R=Path(__file__).resolve().parent
PL=R/'PixelLab'
def read(p): return json.loads(p.read_text(encoding='utf-8'))
def write(p,v): p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
clip_id='hero-v8-run-consistent'
folder=PL/'clips'/clip_id
sheet=Image.open(folder/'workbench-sheet.png').convert('RGBA')
assert sheet.size==(768,256),sheet.size
for i in range(12):sheet.crop((i%6*128,i//6*128,i%6*128+128,i//6*128+128)).save(folder/'frames'/f'{i:02}.png')
write(PL/'motion-v8-jobs.json',dict(version=8,date='2026-10-21',scope='Planning only',jobs=[dict(id=clip_id,name='온 · 원본 의상 보존 달리기',jobId='drawing:e5e8514e-3ea4-4c39-9521-4c858b48bf3f',requestedGeneratedFrames=12,inputFrameIncluded=False,method='PixelLab pixelart_workbench; Idle01 source garment, hood, gloves, bell, boots and sword parts',fps=25,loop=True,status='review',input='clips/hero-v5-idle/frames/01.png')]))
edits=read(PL/'edits.json')
edits['hero-v7-run-authored'].update(status='rejected',note='사용자 반려: 달릴 때 의상·비율·무기가 달라지고 움직임이 지나치게 빠름. 이전 비교용 보존, 현재 미리보기 제외.')
edits[clip_id]=dict(status='review',playbackFrames=list(range(12)),fps=25,loop=True,pivot=[60,114],sourceIdentity='hero-v5-idle/frames/01.png',note='실제 대기01의 후드·크림 의상·허리 종·장갑·부츠·손과 검 원본 패치를 사용. 12자세/480ms, 상하 움직임1px, 이동960px/2초. 현재 검토용 시안이며 출시용 확정 아님.')
write(PL/'edits.json',edits)
pack=PL/'pack-clips.py';s=pack.read_text(encoding='utf-8')
if "if (ROOT/'motion-v8-jobs.json')" not in s:s=s.replace("if (ROOT/'motion-v7-jobs.json')", "if (ROOT/'motion-v8-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v8-jobs.json').read_text(encoding='utf-8'))['jobs']\nif (ROOT/'motion-v7-jobs.json')",1)
s=s.replace("or '-v7-' in job['id']:","or '-v7-' in job['id'] or '-v8-' in job['id']:")
s=s.replace("c['id']=='hero-v7-run-authored'","c['id']=='hero-v8-run-consistent'").replace('data=dict(version=7','data=dict(version=8')
pack.write_text(s,encoding='utf-8')
p=read(R/'proposal.json');p['version']='presentation-v8';p['motionCatalog']='motion-catalog-v8.json'
p['parallax'].update(travelMs=2000,runCycleMs=480,selectedRunClip=clip_id,status='대기 원본 의상·무기 보존 / 반속도 달리기 검토 시안')
p['sequences']['normal'].update(totalMs=3250,segments=[[0,550,'적 분해·복귀'],[550,2550,'2초 이동 · 960px'],[2550,2750,'새 적과 조우·정착'],[2750,3030,'Stage 도트 조립'],[3030,3150,'완성 유지'],[3150,3250,'소멸 → 첫 예고']])
write(R/'proposal.json',p)
for name in ['motions','effects']:
    s=(R/f'{name}-v7.js').read_text(encoding='utf-8').replace('hero-v7-run-authored',clip_id).replace('1초 질주','2초 이동')
    (R/f'{name}-v8.js').write_text(s,encoding='utf-8')
s=(R/'app.js').read_text(encoding='utf-8').replace('[200,950,1650,2030]','[200,1550,2650,3030]');(R/'app.js').write_text(s,encoding='utf-8')
s=(PL/'review.js').read_text(encoding='utf-8').replace("startsWith('hero-v7-')","startsWith('hero-v8-')").replace('hero-v7-run-authored',clip_id);(PL/'review.js').write_text(s,encoding='utf-8')
s=(PL/'review.html').read_text(encoding='utf-8').replace('MOTION v7','MOTION v8').replace('신규 질주·기존 네 방향 회피·피격','원본 의상 보존 달리기·기존 회피·피격');(PL/'review.html').write_text(s,encoding='utf-8')
# Historical pages must retain historical data and code.
s=(R/'index-v7.html').read_text(encoding='utf-8').replace('src="data.js"','src="data-v7.js"').replace('src="app.js"','src="app-v7.js"').replace('src="PixelLab/assets.js"','src="PixelLab/assets-v7.js"').replace('PixelLab/review.html','PixelLab/review-v7.html');(R/'index-v7.html').write_text(s,encoding='utf-8')
s=(PL/'review-v7.html').read_text(encoding='utf-8').replace('src="assets.js"','src="assets-v7.js"').replace('src="review.js"','src="review-v7.js"');(PL/'review-v7.html').write_text(s,encoding='utf-8')
# Contact board: source + consecutive poses, without repainting or deforming asset pixels.
images=[Image.open(PL/'clips/hero-v5-idle/frames/01.png').convert('RGBA')]+[Image.open(folder/'frames'/f'{i:02}.png').convert('RGBA') for i in range(12)]
board=Image.new('RGB',(1792,820),'#101c2d');d=ImageDraw.Draw(board);font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',19)
for i,im in enumerate(images):
    x=(i%7)*256;y=(i//7)*380+45;up=im.resize((256,256),Image.Resampling.NEAREST);board.paste(up,(x,y),up);d.text((x+10,y+265),'실제 대기 원본' if i==0 else f'달리기 {i-1:02} / {(i-1)*40}ms',font=font,fill='#ffe3a4')
board.save(folder/'identity-contact.png')
print('Prepared source-part run, v8 data/code, and identity comparison.')
