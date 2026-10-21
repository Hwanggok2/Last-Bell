from pathlib import Path
import json
R=Path(__file__).resolve().parent
p=R/'motion-v7-jobs.json';m=json.loads(p.read_text('utf-8'))
m['jobs'][0]['status']='rejected'
job={'id':'hero-v7-sprint','name':'온 · 신규 전력질주','jobId':'d79bd7aa-25c2-40c3-8f38-5e12fbcda61c','requestedGeneratedFrames':12,'inputFrameIncluded':True,'method':'PixelLab animate_image_pixminimax','seed':934,'input':'https://api.pixellab.ai/mcp/images/3da349c9-6849-4dd9-95b1-79e8415d82b7/download?index=0','fps':50,'loop':True,'status':'processing'}
m['jobs']=[x for x in m['jobs'] if x['id']!=job['id']]+[job]
m['poseAttempts']=[{'jobId':'aa8dd261-4bdf-4f98-a0db-9352dc9c5d50','status':'rejected','reason':'밝은 몸통, 비율 변화, 약한 전경'},{'jobId':'3da349c9-6849-4dd9-95b1-79e8415d82b7','status':'animation-input','source':'inputs/hero.png','seed':923}]
p.write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
e=json.loads((R/'edits.json').read_text('utf-8'))
e['hero-v7-sprint']={'status':'review','playbackFrames':list(range(1,13)),'fps':50,'loop':True,'pivot':[60,114],'note':'신규 생성12장. 낮은 전경·팔다리 교대·검·접지·루프 연결 실물 검토 대기.'}
(R/'edits.json').write_text(json.dumps(e,ensure_ascii=False,indent=2),encoding='utf-8')
s=(R/'review.js').read_text('utf-8').replace("!clip.id.startsWith('hero-v6-')","!clip.id.startsWith('hero-v7-')").replace("clip.id === 'hero-v6-run'","clip.id === 'hero-v7-sprint'")
(R/'review.js').write_text(s,encoding='utf-8')
s=(R/'review.html').read_text('utf-8').replace('MOTION v6','MOTION v7').replace('양팔 질주·베기·몬스터 피격','신규 질주·기존 네 방향 회피·피격')
(R/'review.html').write_text(s,encoding='utf-8')
print('Registered sprint source and archive statuses.')
