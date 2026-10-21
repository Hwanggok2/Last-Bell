from pathlib import Path
import json
R=Path(__file__).resolve().parent
for p in [R/'index.html',R/'design-spec.md',R/'motion-catalog-v7.json',R.parent/'decisions.json']:
    s=p.read_text('utf-8')
    s=s.replace('현재 아트 담당의 수정 뒤 접지·실루엣·루프를 최종 검토 중이며','독립 아트 검토에서 전경·팔 교대·검 궤적을 확인했으며')
    s=s.replace('현재 아트 수정 뒤 최종 검토 중이며','독립 아트 검토에서 정지 시퀀스 기준 기획 후보 적합으로 판정했으며')
    s=s.replace('아트 최종 검토 중','정지 시퀀스 아트 검토 완료')
    s=s.replace('독립 아트 최종 검토 중','독립 아트 시퀀스 검토 완료')
    p.write_text(s,encoding='utf-8')
p=R/'index.html';s=p.read_text('utf-8').replace('<option value="normal">처치 → 전진','<option value="normal" selected>처치 → 전진').replace('max="2700" step="10" value="850"','max="2250" step="10" value="0"').replace('max="2250" step="10" value="2030"','max="2250" step="10" value="0"').replace('id="sequenceReadout">0.85s','id="sequenceReadout">0.00s')
p.write_text(s,encoding='utf-8')
p=R/'PixelLab/edits.json';d=json.loads(p.read_text('utf-8'));d['hero-v7-run-authored']['note']='PixelLab Workbench 직접 제작12키포즈. 원본 후드 보존, 몸통 전경·양팔 교대·무릎·검의 연속 궤적을 지정. 독립 정지 시퀀스 검토에서 기획 후보 적합. 240ms 반복·Unity 실제 입력 검증은 별도.';p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
# Keep packed metadata synchronized without re-encoding unchanged sheets.
p=R/'PixelLab/assets.json';d=json.loads(p.read_text('utf-8'));edit=json.loads((R/'PixelLab/edits.json').read_text('utf-8'))
for clip in d['clips']:
    if clip['id']=='hero-v7-run-authored':clip.update(edit[clip['id']])
p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8');(R/'PixelLab/assets.js').write_text('window.PIXELLAB_CLIPS = '+json.dumps(d,ensure_ascii=False,indent=2)+';\n',encoding='utf-8')
print('Current planning review status synchronized.')
