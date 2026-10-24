const plan=window.DRAGON_V6_PLAN;
const preview=document.querySelector('#layout-preview'),caption=document.querySelector('#layout-caption');
document.querySelectorAll('[data-layout]').forEach(button=>button.addEventListener('click',()=>{const key=button.dataset.layout;preview.src=key+'-layout.png';preview.alt=key==='growth'?'기존 선택·견적·강화 방식의 업그레이드 배치도':'동료 목록과 전용 스킬·현재 다음 성장 견적 배치도';caption.textContent='1280×720 · SafeRoot 좌상단 기준 · '+(key==='growth'?'업그레이드':'동료')+' 기획용 배치도';document.querySelectorAll('[data-layout]').forEach(x=>x.setAttribute('aria-pressed',String(x===button)))}));
const level=document.querySelector('#level'),label=document.querySelector('#level-label'),summary=document.querySelector('#level-summary'),rows=document.querySelector('#growth-rows');
function chance(l){const g=plan.companionGrowth;return Math.min(g.chanceCapPercent,g.chanceBasePercent+(l-1)*g.chancePerLevelPercentagePoints)}
function period(base,l){const g=plan.companionGrowth;return Math.trunc(base*Math.max(g.periodFloorPercent,100-(l-1)*g.periodReductionPerLevelPercent)/100)}
function seconds(ms){return(ms/1000).toFixed(2)+'초'}
function updateGrowth(){const current=Number(level.value),next=Math.min(plan.companionGrowth.maxLevel,current+1),max=current===next;label.textContent=max?'Lv.30 · 최대 레벨':`Lv.${current} → Lv.${next}`;summary.textContent=max?`발동률${chance(current)}% · 현재 성장 한도입니다.`:`발동률${chance(current)}% → ${chance(next)}% · ${current===10||current===20?'다음 보스 해금 조건 충족 시 강화할 수 있습니다.':'공격 주기가 짧아집니다.'}`;rows.innerHTML=plan.companions.map(n=>`<tr><td>${n.name}</td><td class="row-number">${chance(current)}%${max?'':` → <span class="good">${chance(next)}%</span>`}</td><td class="row-number">${seconds(period(n.basePeriodMs,current))}${max?'':` → <span class="good">${seconds(period(n.basePeriodMs,next))}</span>`}</td><td>${n.skillDamagePercent}% · 고정 배율</td></tr>`).join('')}
level.addEventListener('input',updateGrowth);updateGrowth();

let healthAnimation;
const actualHp=document.querySelector('#health-actual'),trailHp=document.querySelector('#health-trail'),healthReadout=document.querySelector('#health-readout');
document.querySelectorAll('[data-health-damage]').forEach(button=>button.addEventListener('click',()=>{
 cancelAnimationFrame(healthAnimation);const damage=Number(button.dataset.healthDamage),fraction=(300-damage)/300,start=performance.now();
 actualHp.style.width=(fraction*100)+'%';healthReadout.textContent=`HP ${300-damage} / 300 · ${damage>=100?'주황 잔상 → 실제 HP':'작은 피해 · 즉시 반영'}`;
 function frame(now){const t=Math.min(1,Math.max(0,(now-start-120)/550));const value=damage>=100?1+(fraction-1)*t:fraction;
  trailHp.style.width=(Math.ceil(value*180)/180*100)+'%';if(damage>=100&&t<1)healthAnimation=requestAnimationFrame(frame);
 }
 frame(start);
}));
