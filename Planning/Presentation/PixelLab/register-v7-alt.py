from pathlib import Path
import json
R=Path(__file__).resolve().parent;p=R/'motion-v7-jobs.json';m=json.loads(p.read_text('utf-8'))
j={'id':'hero-v7-sprint-alt','name':'온 · 원본 기반 새 질주','jobId':'91a46b97-bdc9-4ca4-aca9-6ea01dc0fa58','requestedGeneratedFrames':8,'inputFrameIncluded':True,'method':'PixelLab animate_image_pixminimax','seed':943,'input':'inputs/hero.png','fps':33.3333333333,'loop':True,'status':'completed'}
m['jobs']=[x for x in m['jobs'] if x['id']!=j['id']]+[j];p.write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
e=json.loads((R/'edits.json').read_text('utf-8'));e[j['id']]={'status':'review','playbackFrames':list(range(1,9)),'fps':1000/30,'loop':True,'pivot':[60,114],'note':'원본 온에서 새로 생성한 8장 질주. 입력 대기0 제외, 자세/교대/접지/루프 검수 중.'};(R/'edits.json').write_text(json.dumps(e,ensure_ascii=False,indent=2),encoding='utf-8')
