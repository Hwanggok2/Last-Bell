"""Use the imported Unity frames and timing verbatim in the local review page."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
target = root / 'Planning/Reference/TimingHero-20261023/feedback-v85.html'
manifest = json.loads((root/'Client/Assets/Game/Art/Presentation/manifest.json').read_text(encoding='utf-8'))
before = json.loads((root/'Planning/Evidence/Feedback-v85/art-manifest-before.json').read_text(encoding='utf-8'))
names = {'hero_on':'온', 'wild_dog':'들개', 'grey_wolf':'회색 늑대', 'brown_bear':'불곰', 'sewer_rat':'하수구 쥐', 'drowned_resident':'침수된 잔상', 'water_wraith':'물의 망령', 'bell_knight_concept':'성문 문지기', 'chain_jailer':'사슬 간수'}
def export(data):
    textures = {t['path']:t for t in data['textures']}
    actors = []
    for a in data['actors']:
        if a['id'] not in names: continue
        clips = {}
        for c in a['clips']:
            if c['motion'] not in ('Idle','Attack','Hit','Stun'): continue
            clips[c['motion']] = {**c, 'frames':[{'url':'../../../Client/'+f, 'pivot':textures[f]['pivot'], 'height':textures[f]['height']} for f in c['frames']]}
        actors.append({'id':a['id'], 'name':names[a['id']], 'facesRight':a['facesRight'], 'height':a['height'], 'referenceHeightPixels':a['referenceHeightPixels'], 'clips':clips})
    return actors
page = r'''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Last Bell · v8.5 Idle와 도트 검수</title><style>
@font-face{font-family:neo;src:url('../../../Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf')}
*{box-sizing:border-box}body{margin:0;background:#0b1720;color:#f4e4bd;font-family:neo,monospace;line-height:1.65}main{max-width:1260px;margin:auto;padding:30px 24px}h1{font-size:28px;margin:8px 0}h2{font-size:20px}p{color:#b6c6bd}a{color:#efbc62}button,select{background:#18303b;border:1px solid #58747b;color:#f4e4bd;padding:9px 14px;font:inherit;cursor:pointer}button:hover{border-color:#efbc62}.tag{color:#e9af59;letter-spacing:.1em}.row{display:flex;flex-wrap:wrap;gap:12px;align-items:center}.hero,.card{border:1px solid #34505b;background:#10232d}.hero{display:grid;grid-template-columns:280px 1fr;margin:24px 0;padding:20px;gap:24px;align-items:center}.hero canvas{width:100%;height:230px;background:#122832}.hero p{margin:5px 0}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:18px;margin-top:18px}.card h2{margin:12px 16px 0}.card p{font-size:14px;margin:4px 16px 12px}.card canvas{width:100%;height:260px;display:block;image-rendering:pixelated}.controls{position:sticky;top:0;z-index:2;background:#0b1720ed;padding:16px 0;border-bottom:1px solid #34505b}.fine{font-size:14px}.label{color:#efbc62}footer{margin:28px 0;border-top:1px solid #34505b;padding-top:18px} @media(max-width:850px){.grid{grid-template-columns:repeat(2,1fr)}.hero{grid-template-columns:1fr}}@media(max-width:540px){.grid{grid-template-columns:1fr}}
</style><main><div class="tag">LAST BELL / v8.5</div><h1>대기 중에도 살아 있는 캐릭터</h1>
<p>주인공의 기존 Idle을 선택 미리보기에 연결했습니다. 몬스터는 원본 주인공의 도트와 명암을 기준으로 다시 제작하고, 보스는 기존 외형과 공격을 보존한 채 Idle을 추가했습니다.</p>
<div class="row"><a href="../../../Builds/Windows/LastBellMvp.exe">Windows 실행 파일</a><a href="../../../Builds/LastBellMvp-Windows.zip">공유용 ZIP</a><a href="../../Evidence/Feedback-v85/verification.json">검증 결과</a></div>
<section class="hero"><canvas id="portrait" width="280" height="230"></canvas><div><div class="tag">용사 선택 미리보기</div><h2>온 · 기존 Idle 재생</h2><p>화면 갱신이나 능력치 선택 때 모션을 처음부터 다시 시작하지 않습니다.</p><p>실제로 보고 있는 캐릭터를 기준으로 재생하며, 일시정지 중에는 멈춥니다. 제작 중인 캐릭터에 다른 캐릭터의 이미지를 대신 표시하지 않습니다.</p><p class="fine">이 페이지의 애니메이션은 Unity에 가져간 동일 PNG와 프레임 유지 시간을 사용합니다.</p></div></section>
<div class="controls row"><label>모션 <select id="motion"><option>Idle</option><option>Attack</option><option>Stun</option><option>Hit</option></select></label><button id="pause">일시정지</button><label>속도 <select id="speed"><option value="1">1×</option><option value=".5">0.5×</option></select></label><label><input type="checkbox" id="old"> 이전 v8.4와 비교</label><span class="fine">왼쪽 작은 온 = 실제 상대 크기 기준</span></div>
<div id="grid" class="grid"></div><h2>실제 Windows 빌드 화면</h2><div class="grid">
<a href="../../Evidence/Feedback-v85/desktop/v85-selection-idle.png"><img style="width:100%" src="../../Evidence/Feedback-v85/desktop/v85-selection-idle.png" alt="선택 화면 Idle 재생 중"></a>
<a href="../../Evidence/Feedback-v85/desktop/v85-idle-01.png"><img style="width:100%" src="../../Evidence/Feedback-v85/desktop/v85-idle-01.png" alt="성 밖 전투 Idle"></a>
<a href="../../Evidence/Feedback-v85/desktop/v85-idle-12.png"><img style="width:100%" src="../../Evidence/Feedback-v85/desktop/v85-idle-12.png" alt="주거구 전투 Idle"></a></div>
<footer><p>발 기준점과 캐릭터 크기는 모션 전체에서 고정합니다. 부유형은 작은 상하 움직임을 유지합니다. 피격은 현재 동작 위에 흰색 플래시로 표현하며, 사망 모션은 추가하지 않았습니다. 예고 초반에는 Idle을 유지하고 실제 공격 준비 시점부터 공격 모션으로 전환합니다.</p><p class="fine">현재 전투 범위 1–20 스테이지 · 공격 판정 시간, 능력치, 강화 비용, 유물 규칙 유지.</p></footer></main>
<script>const current=__CURRENT__,previous=__PREVIOUS__;
const images=new Map();for(const a of [...current,...previous])for(const c of Object.values(a.clips))for(const f of c.frames)if(!images.has(f.url)){const i=new Image;i.src=f.url;images.set(f.url,i)}
const portrait=document.querySelector('#portrait'),select=document.querySelector('#motion'),old=document.querySelector('#old'),speed=document.querySelector('#speed');let paused=false,elapsed=0,last=performance.now();
document.querySelector('#pause').onclick=e=>{paused=!paused;e.target.textContent=paused?'재생':'일시정지'};select.onchange=()=>elapsed=0;
const ids=['wild_dog','grey_wolf','brown_bear','sewer_rat','drowned_resident','water_wraith','bell_knight_concept','chain_jailer'];
const cards=ids.map(id=>{const a=current.find(x=>x.id===id);const el=document.createElement('section');el.className='card';el.innerHTML=`<h2>${a.name}</h2><canvas width="400" height="260"></canvas><p class="meta"></p>`;document.querySelector('#grid').append(el);return {id,canvas:el.querySelector('canvas'),meta:el.querySelector('.meta')}});
function sample(c,t){const holds=c.frameDurationsMs?.length===c.frames.length?c.frameDurationsMs:c.frames.map(()=>1000/(c.fps||8));const total=holds.reduce((a,b)=>a+b,0);t=t%(total+(c.loop?0:500));if(!c.loop)t=Math.min(t,total-1);let n=0;while(n<holds.length-1&&t>=holds[n])t-=holds[n++];return c.frames[n]}
function sprite(ctx,a,motion,t,x,y,unitScale=1){const c=a.clips[motion]||a.clips.Idle,f=sample(c,t),im=images.get(f.url);if(!im?.complete||!im.naturalWidth)return;const s=a.height/(c.referenceHeightPixels||a.referenceHeightPixels)/(.008611111)*unitScale;ctx.save();ctx.translate(x,y);if(a.id!=='hero_on'&&a.facesRight)ctx.scale(-1,1);ctx.drawImage(im,-f.pivot.x*s,-f.pivot.y*s,im.width*s,im.height*s);if(motion==='Hit'&&(t%900)<100){const flash=document.createElement('canvas');flash.width=im.width;flash.height=im.height;const fc=flash.getContext('2d');fc.drawImage(im,0,0);fc.globalCompositeOperation='source-in';fc.fillStyle='#fff';fc.fillRect(0,0,flash.width,flash.height);ctx.drawImage(flash,-f.pivot.x*s,-f.pivot.y*s,im.width*s,im.height*s)}ctx.restore()}
function ground(ctx,w,y){ctx.fillStyle='#10232d';ctx.fillRect(0,0,w,260);ctx.fillStyle='#34505b';ctx.fillRect(0,y,w,2);ctx.imageSmoothingEnabled=false}
function frame(now){if(!paused)elapsed+=(now-last)*Number(speed.value);last=now;const hero=current.find(a=>a.id==='hero_on');let pc=portrait.getContext('2d');ground(pc,280,215);sprite(pc,hero,'Idle',elapsed,140,215,1.3);
for(const card of cards){const ctx=card.canvas.getContext('2d');ground(ctx,400,235);const a=current.find(a=>a.id===card.id),prev=previous.find(a=>a.id===card.id);sprite(ctx,hero,'Idle',elapsed,38,235,.44);if(old.checked){sprite(ctx,prev,select.value,elapsed,155,235,.44);sprite(ctx,a,select.value,elapsed,300,235,.44);ctx.fillStyle='#b6c6bd';ctx.font='14px neo';ctx.fillText('v8.4',123,24);ctx.fillText('v8.5',270,24)}else sprite(ctx,a,select.value,elapsed,245,235,.48);const clip=a.clips[select.value]||a.clips.Idle;card.meta.textContent=`${select.value} · ${clip.frames.length}프레임 · ${clip.frameDurationsMs?.reduce((a,b)=>a+b,0)||Math.round(clip.frames.length/(clip.fps||8)*1000)}ms${clip.loop?' 반복':''}`}
requestAnimationFrame(frame)}requestAnimationFrame(frame);
</script></html>'''
page = page.replace('__CURRENT__',json.dumps(export(manifest),ensure_ascii=False)).replace('__PREVIOUS__',json.dumps(export(before),ensure_ascii=False))
target.write_text(page,encoding='utf-8')
print(target)
