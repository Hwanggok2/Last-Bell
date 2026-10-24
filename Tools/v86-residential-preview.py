import pathlib
R=pathlib.Path(__file__).resolve().parents[1]
s=(R/'Planning/Story/Monster-v85/residential/preview.html').read_text(encoding='utf-8')
s=s.replace('v8.5','v8.6').replace('물에 잠긴 사슬 주거구 · 몬스터 모션','주거구 14~19스테이지 · 기존 몬스터 재사용')
start=s.index('<p>영웅과');end=s.index('</p>',start)
s=s[:start]+'<p>기존 기본 이미지·공격·기절을 보존하고, 자연스러운 대기 모션만 추가했습니다. 14~19스테이지에 각기 다른 적이 한 번씩 등장합니다. 피격은 흰색 점멸입니다.'+s[end:]
s=s.replace('width:384px;height:320px','width:256px;height:256px').replace('height:330px','height:270px').replace('width="192" height="160"','width="128" height="128"').replace('clearRect(0,0,192,160)','clearRect(0,0,128,128)')
s=s.replace('names[actor.id]',"actor.stage+' · '+actor.name").replace('descriptions[actor.id]',"'기존 공격·기절 보존 / 신규 대기 루프'")
s=s.replace("current==='Idle'?'자연스러운 대기 루프'","current==='Idle'?'대기 · 원본 '+String(clip.sourceFrames[frame]).padStart(2,'0')")
s=s.replace('window.reviewActors||[]','(window.reviewActors||[]).slice().sort((a,b)=>a.stage-b.stage)')
(R/'Planning/Story/Monster-v86/residential/preview.html').write_text(s,encoding='utf-8')
print('v86 preview renderer prepared.')
