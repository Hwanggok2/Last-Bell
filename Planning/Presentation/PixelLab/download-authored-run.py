from pathlib import Path
import urllib.request,json
from PIL import Image
R=Path(__file__).resolve().parent;D=R/'clips/hero-v7-run-authored';D.mkdir(parents=True,exist_ok=True)
base='https://api.pixellab.ai/mcp/pixel-tools/148d66b5-7d30-4b4a-847d-6668715fd590/'
for remote,name in [('assets/east/full.png','source-sheet.png'),('recipe.json','recipe.json')]:
    (D/name).write_bytes(urllib.request.urlopen(base+remote,timeout=45).read())
sheet=Image.open(D/'source-sheet.png').convert('RGBA');columns=sheet.width//128
(D/'frames').mkdir(exist_ok=True)
for i in range(12):
    x=i%columns*128;y=i//columns*128;sheet.crop((x,y,x+128,y+128)).save(D/'frames'/f'{i:02}.png')
p=R/'motion-v7-jobs.json';m=json.loads(p.read_text('utf-8'))
m['jobs']=[j for j in m['jobs'] if j['id']!='hero-v7-run-authored']
for j in m['jobs']:j['status']='rejected'
m['jobs'].append({'id':'hero-v7-run-authored','name':'온 · 직접 설계한 12자세 질주','jobId':'drawing:148d66b5-7d30-4b4a-847d-6668715fd590','requestedGeneratedFrames':12,'inputFrameIncluded':False,'method':'PixelLab pixelart_workbench; original hood + authored complete poses','fps':50,'loop':True,'status':'review','input':'inputs/hero.png'})
p.write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
p=R/'edits.json';e=json.loads(p.read_text('utf-8'))
for id in ['hero-v7-run-rig','hero-v7-sprint','hero-v7-sprint-alt']:e[id].update(status='rejected',note='반려: 직립, 팔 교대·검·원형 비율의 불일치. 신규 키포즈 방식으로 대체.')
e['hero-v7-run-authored']={'status':'review','playbackFrames':list(range(12)),'fps':50,'loop':True,'pivot':[60,114],'note':'PixelLab Workbench 직접 제작12자세. 원본 후드 보존, 몸통 전경·양팔 교대·두 다리 접지·검 궤적을 프레임별로 지정. 240ms 반복. 독립 아트 검토 중.'}
p.write_text(json.dumps(e,ensure_ascii=False,indent=2),encoding='utf-8')
print('Saved authored 12 poses',sheet.size)
