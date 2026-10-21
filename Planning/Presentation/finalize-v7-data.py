from pathlib import Path
import json
R=Path(__file__).resolve().parent
p=R/'proposal.json';d=json.loads(p.read_text('utf-8'))
d['parallax'].update(selectedRunClip='hero-v7-run-authored',selectedRunFrames=12,runFrames=12,runCycleMs=240,status='PixelLab Workbench 직접 설계12자세 · 기획 시안 후보')
d['playerMotionManifest']='PixelLab/motion-v7-jobs.json'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
p=R/'PixelLab/review.js';s=p.read_text('utf-8').replace("clip.id === 'hero-v7-sprint'","clip.id === 'hero-v7-run-authored'");p.write_text(s,encoding='utf-8')
p=R/'PixelLab/pack-clips.py';s=p.read_text('utf-8').replace("names[job['id']]+(' / 입력 0 + 생성 프레임'", "names[job['id']]+(' / 직접 설계한 연속 자세' if 'workbench' in job.get('method','') else ' / 입력 0 + 생성 프레임'")
s=s.replace("generatedFrameCount=job['requestedGeneratedFrames']", "generatedFrameCount=0 if 'workbench' in job.get('method','') else job['requestedGeneratedFrames'],authoredFrameCount=job['requestedGeneratedFrames'] if 'workbench' in job.get('method','') else 0")
s=s.replace("c['id']=='hero-v7-run-rig'", "c['id']=='hero-v7-run-authored'")
p.write_text(s,encoding='utf-8')
print('v7 selected clip metadata updated.')
