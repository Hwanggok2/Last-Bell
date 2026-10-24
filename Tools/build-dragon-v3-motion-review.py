"""Build a native-frame review page from imported clips, without inventing poses."""
from pathlib import Path
import json
import os

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Planning/Story/Dragon-v1/Production/motion-v3-review.html'
manifest=json.loads((ROOT/'Client/Assets/Game/Art/Presentation/manifest.json').read_text(encoding='utf-8'))
catalog=json.loads((ROOT/'GameData/mvp-catalog.json').read_text(encoding='utf-8'))
textures={t['path']:t for t in manifest['textures']}
actors={a['id']:a for a in manifest['actors']}
enemies={e['id']:e for e in catalog['enemies']}
items=[('루카 · 플레이어','dragon_hero_on')]+[(f'Stage {s["number"]} · '+enemies[s['enemyId']]['displayName'],enemies[s['enemyId']]['artId']) for s in catalog['stages'] if s['number']<=20]
payload=[]
for label,actor_id in items:
    actor=actors[actor_id]
    clips=[]
    for c in actor['clips']:
        frames=[]
        for path in c['frames']:
            t=textures[path]
            frames.append(dict(src=Path(os.path.relpath(ROOT/'Client'/path,OUT.parent)).as_posix(),pivot=t['pivot'],width=t['width'],height=t['height']))
        clips.append(dict(motion=c['motion'],fps=c['fps'],loop=c['loop'],contact=c['contactFrame'],holds=c.get('frameDurationsMs') or [],frames=frames))
    scale=actor['height']/(6.2/720)/actor['referenceHeightPixels']
    payload.append(dict(label=label,id=actor_id,scale=scale,clips=clips))
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>루카·Stage1–20 모션과 접지</title><style>
*{box-sizing:border-box}body{margin:0;background:#f6edd8;color:#243b43;font:16px/1.65 "Malgun Gothic",sans-serif}main{max-width:1200px;margin:auto;padding:28px}h1{font-size:32px;margin:10px 0}a{color:#315e4d}button,select{font:inherit;padding:10px;border:2px solid #243b43;background:#fff8e6;color:#243b43;cursor:pointer}button[aria-pressed=true]{background:#315e4d;color:white}.controls{display:flex;gap:10px;flex-wrap:wrap;margin:20px 0}canvas{width:100%;height:auto;border:4px solid #243b43;background:#dbe8cc;image-rendering:pixelated}.stats{background:#fff8e6;padding:16px;border-left:6px solid #315e4d}.small{font-size:14px}input{width:100%}</style><main><a href="../index.html#unity-build">← 기획서</a><h1>루카와 Stage1–20 · 모션·접지 원본</h1><p>같은 발 피벗에 원본 프레임을 재생합니다. 점프·달리기의 의도한 높이는 남기며, 프레임별 몸을 강제로 재배치하지 않습니다. 실제 Unity의 접근 이동·보스 줌·공격 판정은 이 원본 뷰어와 별도입니다.</p>
<div class="controls"><select id="actor" aria-label="캐릭터 또는 스테이지"></select><select id="motion" aria-label="모션"></select><button id="play" aria-pressed="true">일시정지</button><button id="step">한 프레임</button><select id="speed" aria-label="재생 배속"><option value="1">1배</option><option value=".5">0.5배</option><option value=".25">0.25배</option></select></div>
<canvas id="view" width="1120" height="590" aria-label="원본 프레임과 공통 발 피벗"></canvas><input id="scrub" type="range" min="0" value="0" aria-label="프레임 선택"><div class="stats" id="stats"></div><p class="small">실선은 이 뷰어의 지면이고 십자는 원본 발 피벗입니다. 게임의 공통 지면은 계획Y590입니다. 루카의 아래 쳐내기는 베기 원본을 전용 타이밍으로 재사용합니다. 실제 지원 조작은 위·뒤 회피 + 아래 쳐내기입니다. 애니메이션이 반복 표시되어도 실제 전투에서 공격이 자동으로 반복되는 것은 아닙니다.</p></main><script>
const data=__DATA__;const actor=document.querySelector('#actor'),motion=document.querySelector('#motion'),play=document.querySelector('#play'),step=document.querySelector('#step'),speed=document.querySelector('#speed'),scrub=document.querySelector('#scrub'),stats=document.querySelector('#stats'),canvas=document.querySelector('#view'),ctx=canvas.getContext('2d');
let chosen,clip,frames=[],frame=0,age=0,playing=true,last=performance.now(),generation=0;
data.forEach((x,i)=>actor.add(new Option(x.label,i)));function selectMotion(){const token=++generation;clip=chosen.clips[+motion.value];frame=0;age=0;scrub.max=clip.frames.length-1;scrub.value=0;frames=clip.frames.map(f=>{const im=new Image;im.onload=()=>{if(token===generation)draw()};im.src=f.src;return im});draw()}
function selectActor(){chosen=data[+actor.value];motion.replaceChildren();chosen.clips.forEach((x,i)=>motion.add(new Option(x.motion,i)));const idle=chosen.clips.findIndex(x=>x.motion==='Idle');motion.value=Math.max(0,idle);selectMotion()}
function durations(){return clip.holds.length?clip.holds:clip.frames.map(()=>1000/clip.fps)}
function sample(ms){const d=durations(),total=d.reduce((a,b)=>a+b,0);let t=ms%total;for(let i=0;i<d.length;i++){if(t<d[i])return i;t-=d[i]}return d.length-1}
function draw(){if(!clip)return;ctx.imageSmoothingEnabled=false;ctx.clearRect(0,0,1120,590);ctx.fillStyle='#dbe8cc';ctx.fillRect(0,0,1120,590);ctx.fillStyle='#bc9968';ctx.fillRect(0,470,1120,120);ctx.fillStyle='#243b43';ctx.fillRect(0,470,1120,2);const meta=clip.frames[frame],im=frames[frame],s=chosen.scale;if(im&&im.complete&&im.naturalWidth)ctx.drawImage(im,560-meta.pivot.x*s,470-meta.pivot.y*s,meta.width*s,meta.height*s);ctx.strokeStyle='#c05536';ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(549,470);ctx.lineTo(571,470);ctx.moveTo(560,459);ctx.lineTo(560,481);ctx.stroke();stats.textContent=chosen.label+' · '+clip.motion+' · '+(frame+1)+'/'+clip.frames.length+' · 원본 배율 '+s.toFixed(0)+'배 · 발 피벗('+meta.pivot.x+', '+meta.pivot.y+')'+(frame===clip.contact?' · 접촉/유지 자세':'');scrub.value=frame}
actor.onchange=selectActor;motion.onchange=selectMotion;play.onclick=()=>{playing=!playing;play.textContent=playing?'일시정지':'재생';play.setAttribute('aria-pressed',playing)};step.onclick=()=>{playing=false;play.textContent='재생';play.setAttribute('aria-pressed','false');frame=(frame+1)%clip.frames.length;age=durations().slice(0,frame).reduce((a,b)=>a+b,0);draw()};scrub.oninput=()=>{playing=false;play.textContent='재생';play.setAttribute('aria-pressed','false');frame=+scrub.value;age=durations().slice(0,frame).reduce((a,b)=>a+b,0);draw()};
function tick(now){let dt=Math.min(100,now-last);last=now;if(playing&&clip){age+=dt*Number(speed.value);frame=sample(age);draw()}requestAnimationFrame(tick)}selectActor();requestAnimationFrame(tick);
</script></html>'''
OUT.write_text(page.replace('__DATA__',json.dumps(payload,ensure_ascii=False).replace('</','<\\/')),encoding='utf-8')
print(f'Created review page for{len(payload)} actor/stage bindings.')
