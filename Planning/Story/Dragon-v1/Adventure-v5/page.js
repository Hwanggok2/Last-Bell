/* Planning controls only. No game state, save, purchases or network. */
const screen=document.querySelector('#preview-screen'),caption=document.querySelector('#preview-caption');
const descriptions={dialogue:'NPC 대화 시안 · 오른쪽 위 초상 / 왼쪽 위 이름표','dialogue-player':'플레이어 대화 시안 · 왼쪽 위 미엘 초상 / 초상 오른쪽 이름표',inn:'여관 동료 탭 시안 · 왼쪽 목록·편성 / 오른쪽 역할·성장',rhythm:'공격 리듬 도식 · 실제 애니메이션이 아닌 동작 순서 설계',growth:'여관 성장 탭 시안 · 다섯 번째 스탯 회복력'};
let selected='dialogue',gray=false;
function show(){screen.src=selected+'-'+(gray&&['dialogue','dialogue-player','inn','growth'].includes(selected)?'wireframe':'draft')+'.png';screen.alt=descriptions[selected];caption.textContent=descriptions[selected]+' · 1280×720 기준 · Unity 적용 진행 중';document.querySelectorAll('[data-scene]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.scene===selected)));document.querySelector('#gray').setAttribute('aria-pressed',String(gray));}
document.querySelectorAll('[data-scene]').forEach(b=>b.addEventListener('click',()=>{selected=b.dataset.scene;show()}));
document.querySelector('#gray').addEventListener('click',()=>{gray=!gray;show()});
function calculate(){const hp=Number(document.querySelector('#hp').value),max=Number(document.querySelector('#maxhp').value),rec=Number(document.querySelector('#recovery-value').value);const out=document.querySelector('#heal-result');if(!Number.isFinite(hp+max+rec)||hp<0||max<=0||rec<0||hp>max){out.textContent='0 ≤ 현재 HP ≤ 최대 HP, 회복력 ≥ 0을 입력하세요.';return;}const healed=hp===0?0:Math.min(max-hp,Math.floor(rec));out.textContent=hp===0?'HP 0 · 사망 시 회복하지 않습니다.':`${hp} + ${healed} = ${hp+healed} / ${max} HP`;}
document.querySelectorAll('.calculator input').forEach(el=>el.addEventListener('input',calculate));calculate();
