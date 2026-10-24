"""Build a local, fetch-free animation comparison from unchanged source PNGs."""
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Planning/Story/Dragon-v1/Production/player-v2-review.html'
BASE = ROOT / 'Planning/Story/Dragon-v1/Production'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
manifest = read(ROOT / 'Client/Assets/Game/Art/Presentation/manifest.json')
old = next(x for x in manifest['actors'] if x['id'] == 'hero_on')
textures = {x['path']: x for x in manifest['textures']}
motion_labels = {'Run': '달리기', 'Attack': '베기', 'Idle': '대기', 'DodgeUp': '위 회피', 'DodgeBack': '뒤 회피', 'DodgeDown': '아래 쳐내기', 'Hit': '피격'}
new_clips = read(BASE / 'Player-v2/manifest.json')['actors'][0]['clips']
motions = [name for name in motion_labels if new_clips.get(name, {}).get('frames')]


def url(path):
    return os.path.relpath(path, OUT.parent).replace('\\', '/')


legacy = {'label': '기존 승인 동작', 'height': old['referenceHeightPixels'], 'clips': {}}
for motion in motions:
    name = 'AttackUp' if motion in ('Attack', 'DodgeDown') else motion
    clip = next(x for x in old['clips'] if x['motion'] == name)
    legacy['clips'][motion] = dict(frames=[url(ROOT / 'Client' / p) for p in clip['frames']],
        pivots=[[textures[p]['pivot']['x'], textures[p]['pivot']['y']] for p in clip['frames']],
        fps=clip['fps'], contact=clip['contactFrame'], holds=clip.get('frameDurationsMs'),
        height=clip.get('referenceHeightPixels', 0) or old['referenceHeightPixels'])
    if motion in ('DodgeUp', 'DodgeBack'):
        held_source = 4 if motion == 'DodgeUp' else 8
        legacy['clips'][motion]['contact'] = clip['sourceFrames'].index(held_source)
actors = [legacy]
for folder, label in [('Player', '직전 Dragon v1'), ('Player-v2', '새 모험가 v2')]:
    a = read(BASE / folder / 'manifest.json')['actors'][0]
    actor = {'label': label, 'height': a['referenceHeight'], 'clips': {}}
    for motion in motions:
        c = a['clips'][motion]
        files = [ROOT / p if (ROOT / p).is_file() else BASE / folder / p for p in c['frames']]
        actor['clips'][motion] = dict(frames=[url(p) for p in files],
            pivots=[c.get('pivotTopLeft', a['pivotTopLeft'])] * len(files),
            fps=c['fps'], contact=c.get('contactFrame', -1), holds=c.get('frameDurationsMs'),
            height=c.get('referenceHeight', a['referenceHeight']))
    actors.append(actor)

html = r'''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>모험가 v2 · 동작 비교</title><style>
@font-face{font-family:pixel;src:url('../../../../Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf')}
*{box-sizing:border-box}body{margin:0;background:#fff1d1;color:#273d45;font:17px/1.7 sans-serif}main{max-width:1150px;margin:auto;padding:32px 24px}h1,h2,button,select{font-family:pixel,sans-serif}h1{font-size:38px}a{color:#315e4d}.controls{display:flex;flex-wrap:wrap;gap:12px;align-items:center;margin:28px 0}button,select{font-size:22px;padding:10px 18px;background:#fff9e9;color:#273d45;border:3px solid #273d45}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px}.card{padding:16px;border:4px solid #273d45;background:#fff9e9}h2{font-size:24px;margin:0 0 10px}canvas{display:block;width:100%;height:auto;image-rendering:pixelated;background:#9bc6b0}small{display:block}.note{padding:18px;border-left:6px solid #b77a35}img{max-width:100%;image-rendering:pixelated}@media(max-width:800px){.grid{grid-template-columns:1fr}}button:focus-visible,select:focus-visible{outline:3px solid #a14032;outline-offset:4px}
</style><main><a href="../index.html#unity-build">← 제작 현황</a><h1>같은 동작, 새 모험가</h1>
<p>기존 동작 원본과 직전 캐릭터, 새 모험가를 같은 시간축에서 비교합니다. 원본 PNG를 그대로 재생하며 체형은 비교를 위해 비슷한 키로 표시합니다.</p>
<div class="controls"><label>동작 <select id="motion"><option value="Run">달리기</option><option value="Attack">베기</option><option value="Idle">대기</option><option value="DodgeUp">위 회피</option><option value="DodgeBack">뒤 회피</option><option value="DodgeDown">아래 쳐내기</option><option value="Hit">피격</option></select></label><button id="play">일시정지</button><button id="restart">처음부터</button><label>속도 <select id="speed"><option value="1">1배</option><option value="0.5">0.5배</option><option value="0.25">0.25배</option></select></label><button id="step">한 프레임</button></div>
<div class="grid" id="actors"></div><p class="note">달리기는400ms 주기. 베기는120ms 준비 유지·200ms 접촉·500ms 복귀를 기준으로 봅니다. 회피는 자세 검토용 진입·유지·복귀 재생입니다. 이 페이지에는 게임의 실제 루트 이동·판정·참격 이펙트가 없으며, 실제 Unity 캡처와 구분합니다.</p>
<h2>체력바의 픽셀 단위</h2><p>기존24px 높이를36px로 높이고 계단형 양 끝과 구획된 채움으로 바꿨습니다. 위치와 실제 체력 값은 유지합니다.</p><a href="UI-v2/index.html"><img src="UI-v2/compare-1280.png" alt="기존과 새 체력바의0·소량·25·50·100퍼센트 비교"></a>
<p><a href="Player-v2/motion-reference-review.md">기존 동작 조사·독립 검토</a> · <a href="Player-v2/manifest.json">새 모션 원본</a> · <a href="UI-v2/handoff.md">체력바 제작 기록</a></p></main><script>
const data=__DATA__, motion=document.getElementById('motion'), speed=document.getElementById('speed');let elapsed=0,last=performance.now(),playing=true;
const panels=data.map(a=>{const el=document.createElement('article');el.className='card';el.innerHTML='<h2></h2><canvas width="320" height="240"></canvas><small></small>';el.querySelector('h2').textContent=a.label;document.getElementById('actors').append(el);for(const c of Object.values(a.clips))c.images=c.frames.map(src=>{const i=new Image();i.src=src;return i});return {a,c:el.querySelector('canvas'),label:el.querySelector('small')};});
function pose(c,m,t){const n=c.frames.length;if(m==='Run'||m==='Idle'){let holds=c.holds&&c.holds.length===n?c.holds:Array(n).fill(1000/c.fps);if(m==='Run')holds=[40,40,60,60,40,40,60,60];let age=t%holds.reduce((a,b)=>a+b,0);for(let i=0;i<n;i++){age-=holds[i];if(age<0)return i;}return n-1;}
let age=t%1000;if(m==='Attack'||m==='DodgeDown'){if(age>=500)return 0;const contact=c.contact>=0?c.contact:Math.floor(n/2);if(age<120)return 0;if(age<200)return Math.min(contact-1,Math.floor((age-120)/80*contact));return Math.min(n-1,contact+Math.floor((age-200)/300*(n-contact)));}
if(m.startsWith('Dodge')){if(age>=280)return 0;const hold=c.contact>=0?c.contact:Math.floor(n/2);if(age<50)return Math.min(hold,Math.floor(age/50*(hold+1)));if(age<150)return hold;return Math.min(n-1,hold+1+Math.floor((age-150)/130*(n-hold-1)));}
return age<300?Math.min(n-1,Math.floor(age/300*n)):0;}
function render(){for(const p of panels){const c=p.a.clips[motion.value],i=pose(c,motion.value,elapsed),img=c.images[i],ctx=p.c.getContext('2d');ctx.clearRect(0,0,320,240);ctx.fillStyle='#315e4d';ctx.fillRect(0,202,320,4);ctx.imageSmoothingEnabled=false;let s=154/c.height,pivot=c.pivots[i];if(img.complete&&img.naturalWidth)ctx.drawImage(img,160-pivot[0]*s,202-pivot[1]*s,img.naturalWidth*s,img.naturalHeight*s);p.label.textContent=`원본 몸높이 기준 ${c.height}px · ${i+1}/${c.frames.length} 프레임`;}}
function tick(now){if(playing)elapsed+=(now-last)*Number(speed.value);last=now;render();requestAnimationFrame(tick)}requestAnimationFrame(tick);
function state(){document.getElementById('play').textContent=playing?'일시정지':'재생'}document.getElementById('play').onclick=()=>{playing=!playing;state()};document.getElementById('restart').onclick=()=>{elapsed=0;render()};document.getElementById('step').onclick=()=>{playing=false;state();elapsed+=40;render()};motion.onchange=()=>{elapsed=0;render()};
</script></html>'''
import re
options = ''.join(f'<option value="{name}">{motion_labels[name]}</option>' for name in motions)
html = re.sub(r'(<select id="motion">).*?(</select>)', lambda m: m[1] + options + m[2], html)
OUT.write_text(html.replace('__DATA__', json.dumps(actors, ensure_ascii=False).replace('</', r'<\/')), encoding='utf-8')
print('Built unchanged-PNG animation comparison:', OUT)
