"""Package original 128px PixelLab cells without per-frame cropping or deformation."""
import json, math, pathlib
from PIL import Image, ImageDraw, ImageFont
ROOT=pathlib.Path(__file__).resolve().parent
jobs=json.loads((ROOT/'jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v10-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v10-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v9-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v9-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v8-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v8-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v7-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v7-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v6-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v6-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'player-v5-jobs.json').exists():jobs+=json.loads((ROOT/'player-v5-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'player-v5-run-job.json').exists():jobs.append(json.loads((ROOT/'player-v5-run-job.json').read_text(encoding='utf-8')))
if (ROOT/'player-v5-run-alt-job.json').exists():jobs.append(json.loads((ROOT/'player-v5-run-alt-job.json').read_text(encoding='utf-8')))
names={'hero-run':'온 · 달리기 A','hero-run-classic':'온 · 달리기 B','hero-attack':'온 · 사선 베기','sentinel-attack':'성문 파수병 · 공격','chain-attack':'사슬 망령 · 공격','wisp-attack':'재의 불씨 · 공격','warden-attack':'성문 수문장 · 공격','knight-attack':'종의 기사 · 공격','hero-dodge-up':'온 · 위 회피','hero-dodge-down':'온 · 아래 회피','hero-dodge-front':'온 · 앞 회피','hero-dodge-back':'온 · 뒤 회피'}
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',14)
names.update({'hero-attack-retry':'온 · 사선 베기 재작업','hero-run-retry':'온 · 달리기 C','knight-attack-retry':'종의 기사 · 단발 재작업'})
names.update({j['id']:j['name'] for j in jobs if 'name' in j})
overrides=json.loads((ROOT/'edits.json').read_text(encoding='utf-8')) if (ROOT/'edits.json').exists() else {}
clips=[]
def gif_durations(count,fps):
    # GIF stores centiseconds; alternate 20/30ms at40fps instead of shortening every25ms frame.
    return [max(10,(round((i+1)*100/fps)-round(i*100/fps))*10) for i in range(count)]
for job in jobs:
    folder=ROOT/'clips'/job['id'];paths=[folder/'frames'/f'{i:02}.png' for i in range(job['requestedGeneratedFrames']+int(job.get('inputFrameIncluded',True)))]
    if not all(p.exists() for p in paths):continue
    frames=[Image.open(p).convert('RGBA') for p in paths]
    assert all(f.size==(128,128) for f in frames)
    cols=5;rows=math.ceil(len(frames)/cols)
    sheet=Image.new('RGBA',(cols*128,rows*128))
    contact=Image.new('RGB',(cols*256,rows*286+48),'#101c2d');draw=ImageDraw.Draw(contact)
    draw.text((12,12),names[job['id']]+(' / 직접 설계한 연속 자세' if 'workbench' in job.get('method','') else ' / 입력 0 + 생성 프레임' if job.get('inputFrameIncluded',True) else ' / 전체 새 생성 프레임')+' / 동일 캔버스·피벗',fill='#ffe3a4',font=font)
    preview=[]
    for i,f in enumerate(frames):
        x=i%cols;y=i//cols;sheet.paste(f,(x*128,y*128))
        contact.paste(f.resize((256,256),Image.Resampling.NEAREST),(x*256,y*286+70),f.resize((256,256),Image.Resampling.NEAREST))
        draw.text((x*256+10,y*286+48),f'{i:02}'+(' INPUT' if i==0 and job.get('inputFrameIncluded',True) else ''),font=font,fill='#9aabbb')
        canvas=Image.new('RGBA',(384,384),'#101c2d');canvas.alpha_composite(f.resize((384,384),Image.Resampling.NEAREST));preview.append(canvas.convert('RGB'))
    sheet.save(folder/'sheet.png');contact.save(folder/'contact.png')
    fps=job.get('fps',20);preview[0].save(folder/'animation.gif',save_all=True,append_images=preview[1:],duration=gif_durations(len(preview),fps),loop=0,disposal=2)
    actor=job['id'].split('-')[0];pivot=[60,114] if actor=='hero' else [76 if actor=='wisp' else 78,114]
    clips.append(dict(id=job['id'],name=names[job['id']],actor=actor,action=job['id'].replace(actor+'-',''),sheet=f"clips/{job['id']}/sheet.png",frameWidth=128,frameHeight=128,columns=cols,frameCount=len(frames),fps=fps,loop='run' in job['id'],pivot=pivot,status='review',note='품질 검토 중. 첫 셀은 입력 원본이며 생성 프레임 수에 포함하지 않음. 무기·접지·반복 경계 확인 필요.',sourceJob=job['jobId'],generatedFrameCount=0 if 'workbench' in job.get('method','') else job['requestedGeneratedFrames'],authoredFrameCount=job['requestedGeneratedFrames'] if 'workbench' in job.get('method','') else 0,inputFrameIncluded=job.get('inputFrameIncluded',True),contact=f"clips/{job['id']}/contact.png",gif=f"clips/{job['id']}/animation.gif"))
    if job['id'].startswith('hero-v5-') or '-v6-' in job['id'] or '-v7-' in job['id'] or '-v8-' in job['id'] or '-v9-' in job['id'] or '-v10-' in job['id']:
        clips[-1]['frameBounds']=[list(f.getbbox() or (0,0,0,0)) for f in frames]
        clips[-1]['uniqueFrames']=len({f.tobytes() for f in frames[int(job.get('inputFrameIncluded',True)):]})
        clips[-1]['loop']=job.get('loop',False)
folder=ROOT/'clips'/'diagonal-slash'
if (folder/'recipe.json').exists():
    meta=json.loads((folder/'recipe.json').read_text());rects=[f['frame'] for f in meta['frames']]
    cols=Image.open(folder/'sheet.png').width//256
    clips.append(dict(id='diagonal-slash',name='온 · 긴 사선 참격 VFX',actor='vfx',action='slash',sheet='clips/diagonal-slash/sheet.png',frameWidth=256,frameHeight=128,columns=cols,frameCount=len(rects),fps=40,loop=False,pivot=[12,110],status='review',note='PixelLab VFX Workbench 제작. 250ms 단발, 크림 중심·금빛 테두리. 애니메이션이 피해를 추가하지 않음.',sourceJob='effect:faf3549e-3c15-4f10-aca4-5c0e8a54127a'))
for clip in clips:
    clip.update(overrides.get(clip['id'],{}))
    if 'playbackFrames' in clip:
        folder=ROOT/'clips'/clip['id'];preview=[]
        for i in clip['playbackFrames']:
            f=Image.open(folder/'frames'/f'{i:02}.png').convert('RGBA')
            canvas=Image.new('RGBA',(384,384),'#101c2d');canvas.alpha_composite(f.resize((384,384),Image.Resampling.NEAREST));preview.append(canvas.convert('RGB'))
        preview *= clip.get('previewLoopRepeats',1)
        preview[0].save(folder/'edited-preview.gif',save_all=True,append_images=preview[1:],duration=gif_durations(len(preview),clip['fps']),loop=0,disposal=2)
        clip['editedGif']=f"clips/{clip['id']}/edited-preview.gif"
clips.sort(key=lambda c:(5 if c['status']=='rejected' else 0 if c['id']=='hero-v10-blademage-run' else 1 if '-v7-' in c['id'] or '-v6-' in c['id'] else 2 if c['id'].startswith('hero-v5-') else 3 if c['actor']!='hero' else 4))
data=dict(version=10,date='2026-10-21',scope='Planning only',clips=clips)
(ROOT/'assets.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'assets.js').write_text('window.PIXELLAB_CLIPS = '+json.dumps(data,ensure_ascii=False,indent=2)+';\n',encoding='utf-8')
print('Packed '+str(len(clips))+' clips: '+', '.join(c['id'] for c in clips))
