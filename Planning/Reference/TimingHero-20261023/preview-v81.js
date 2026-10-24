'use strict';
const M=window.PreviewV81,D=window.REPORT_DATA,A=window.PREVIEW_ASSETS,P=window.PLAN_V81,$=id=>document.getElementById(id);
let state=M.fresh(),last=performance.now();
const isSettlement=s=>['run-end','milestone','relics','result'].includes(s);
const help={selection:'레퍼런스처럼 왼쪽은 종지기 목록, 오른쪽은 선택한 종지기의 상세 정보입니다. 선택을 누르면 별도의 강화 화면으로 이동합니다. 담·린은 제작 중 상태를 유지합니다.',upgrade:'왼쪽에서 능력치를 고르고 오른쪽에서 현재→다음 값과 비용을 확인합니다. 1Lv. / MAX는 견적 방식만 바꾸며, 하단 강화를 눌러야 골드가 차감됩니다. MAX는 현재 골드로 가능한 만큼입니다.',prologue:'프롤로그는 18초 자동 위쪽 스크롤과 건너뛰기 하나만 제공합니다. 일시정지·다음 문단·페이지 표시를 제거했습니다.',battle:'제안값: 0·300·600ms 깜빡임, 단타 900ms 접촉. 연속 공격은 각 방향을 모두 예고한 뒤 750ms 간격으로 타격합니다. ← 후방 회피 / ↑ 점프 / ↓ 쳐내기. 보스도 하단 체력 막대 아래에 레벨과 이름을 표시합니다. 판정창은 일반 240ms, 완벽 90ms로 제안합니다.', 'run-end':'런 종료를 먼저 알립니다. 이번 런에서 실제로 쓰러뜨린 보스의 유물만 정산합니다. 보스 종 파편은 처치할 때 이미 지급되어 있어 여기서 또 더하지 않습니다.',milestone:'낮은 스테이지의 미수령 보상부터 표시합니다. 죽은 스테이지와 클리어한 보스 스테이지를 혼동하지 않도록 별도 안내합니다.',relics:'후보 ID는 보스 처치 때 고정합니다. 설명은 지금 보유한 레벨에서 계산하므로 앞 보상에서 같은 유물을 받았다면 다음 보상의 증가량도 갱신됩니다.',result:'모든 돌파 유물 선택을 마친 뒤에만 다시하기 / 로비를 표시합니다. 10 클리어 기록이 있는 캐릭터의 다음 출발은 11입니다. 이 프로토타입은 20에서 종료합니다.',collection:'기존 유물 도감의 배치 예시입니다. 보유는 공용, 장착은 캐릭터별 3슬롯 체계를 유지합니다. 이 미리보기에서는 실제 장착을 편집하지 않습니다.'};
function controls(){$('screen').value=state.screen;$('growth-options').hidden=!['selection','upgrade'].includes(state.screen);$('battle-options').hidden=state.screen!=='battle';$('settle-options').hidden=!isSettlement(state.screen);$('timeline').hidden=state.screen!=='battle';$('play').textContent=state.paused?'재생':'일시정지';$('gold').value=state.gold;$('level').value=state.levels[state.selectedStat];}
function render(){const r=M.scene(state,A,D);$('stage').innerHTML=r.svg;$('stage').style.maxWidth=state.w+'px';$('context').textContent=help[state.screen];if(state.screen==='battle'){const q=M.schedule(state.count);$('scrub').max=q.at(-1).impact+P.timing.idleAfterLastHitMs[state.kind];$('scrub').value=Math.round(state.t);$('clock').textContent=Math.round(state.t)+' ms';$('beats').textContent=q.map((r,i)=>`${i+1}타 예고 ${r.flashes.join('/')} → 타격 ${r.impact}ms`).join('　|　');}}
function announce(s){$('status').textContent=s;}
function show(screen){state.screen=screen;state.t=0;state.paused=false;state.feedback='';controls();render();}
function prepareSettlement(screen='run-end'){M.beginSettlement(state,$('scenario').value,D);if(['milestone','relics'].includes(screen)&&!M.currentOffer(state))screen='result';state.screen=screen;controls();render();}
function act(a){if(a.startsWith('character-')){state.character=+a.split('-')[1];}
else if(a==='select-character'){if(D.catalog.characters[state.character].availableInMvp)show('upgrade');}
else if(a.startsWith('stat-')){state.selectedStat=+a.split('-')[1];state.notice='';}
else if(a==='mode-one'||a==='mode-max'){state.mode=a==='mode-one'?'one':'max';state.notice='';}
else if(a==='buy'){const r=M.purchase(state,D);state.notice=r.ok?`Lv.${r.from} → ${r.to} · ${r.total.toLocaleString()} G 사용`:r.reason;announce(state.notice);}
else if(a==='skip')show('selection');
else if(a==='selection'||a==='lobby')show('selection');
else if(a==='battle'||a==='retry'){state.kind='normal';state.count=state.checkpoint===1?1:2;$('kind').value='normal';$('count').value=String(state.count);show('battle');}
else if(a==='collection')show('collection');
else if(a.startsWith('book-'))state.bookItem=+a.split('-')[1];
else if(a==='equip')announce('도감의 배치 시안입니다. 실제 장착 슬롯을 변경하지 않습니다.');
else if(a==='settle-next')show(M.currentOffer(state)?'milestone':'result');
else if(a==='open-relic'){state.selectedRelic=null;show('relics');}
else if(a.startsWith('relic-')){const id=a.slice(6);if(M.currentOffer(state)?.ids.includes(id))state.selectedRelic=id;}
else if(a==='confirm-relic'){if(M.claim(state,D))announce('보상을 확정했습니다. 다음 미수령 보상 또는 최종 결과로 이동합니다.');}
else if(a==='pause-battle')state.paused=!state.paused;
else if(a.startsWith('dodge-')){if(state.paused){announce('전투 재생 중에 입력 표본을 확인할 수 있습니다.');return;}const q=M.schedule(state.count),i=q.findIndex(x=>x.impact>=state.t);if(i>=0){const right=a==='dodge-'+['back','up','down'][i],r=M.defend(state.t,q[i].impact,right);state.feedback=(right?`${Math.round(q[i].impact-state.t)}ms 전 · `:'방향 불일치 · ')+{perfect:'완벽 회피',normal:'일반 회피',fail:'실패'}[r];announce('입력 표본: '+state.feedback);}}
else if(a==='attack')announce('자동 공격 연출입니다. 완벽 회피 후 기준 위치로 돌아갔다가 전진합니다.');
controls();render();}
$('stage').addEventListener('click',e=>{const b=e.target.closest('[data-action]');if(b?.dataset.enabled==='true')act(b.dataset.action);});
$('stage').addEventListener('keydown',e=>{if(['Enter',' '].includes(e.key)){const b=e.target.closest('[data-action]');if(b?.dataset.enabled==='true'){e.preventDefault();act(b.dataset.action);}}});
window.addEventListener('keydown',e=>{if(state.screen!=='battle'||/INPUT|SELECT|BUTTON/.test(e.target.tagName))return;const dir={ArrowLeft:'back',ArrowUp:'up',ArrowDown:'down',a:'back',w:'up',s:'down'}[e.key];if(dir){e.preventDefault();if(!e.repeat)act('dodge-'+dir);}else if(['ArrowRight','d'].includes(e.key)){e.preventDefault();}});
$('screen').onchange=e=>{const screen=e.target.value;if(isSettlement(screen))prepareSettlement(screen);else show(screen);};
$('viewport').onchange=e=>{[state.w,state.h]=e.target.value.split(',').map(Number);render();};
$('gold').onchange=e=>{state.gold=Math.max(0,Math.min(99999999999,Math.trunc(+e.target.value)||0));state.notice='';render();};
$('level').onchange=e=>{const L=Math.max(0,Math.min(50,Math.trunc(+e.target.value)||0));state.levels=[L,L,L,L];state.notice='';controls();render();};
$('broke').onclick=()=>{state.gold=0;controls();render();};$('maxed').onclick=()=>{state.levels=[50,50,50,50];controls();render();};
$('kind').onchange=e=>{state.kind=e.target.value;state.t=0;render();};$('count').onchange=e=>{state.count=+e.target.value;state.t=0;render();};$('outcome').onchange=e=>{state.outcome=e.target.value;render();};
$('play').onclick=()=>{state.paused=!state.paused;controls();};$('restart').onclick=()=>{state.t=0;state.paused=false;controls();};$('speed').onchange=e=>state.speed=+e.target.value;
$('scrub').oninput=e=>{state.t=+e.target.value;state.paused=true;controls();render();};
$('start-settlement').onclick=()=>prepareSettlement();$('scenario').onchange=()=>prepareSettlement();$('reload-sample').onclick=()=>{state=JSON.parse(JSON.stringify(state));announce('직렬화한 샘플 상태를 복구했습니다. 보상 순서·후보·수령 기록을 유지합니다. 실제 게임 저장 검증은 별도입니다.');controls();render();};
$('reset').onclick=()=>{state=M.fresh();$('viewport').value='960,540';$('count').value='2';$('kind').value='boss';$('outcome').value='perfect';$('speed').value='1';$('scenario').value='death17';controls();render();};
document.addEventListener('visibilitychange',()=>last=performance.now());
function tick(now){const dt=Math.min(100,now-last);last=now;if(!document.hidden&&!state.paused&&['battle','prologue'].includes(state.screen)){state.t+=dt*state.speed;if(state.screen==='prologue'&&state.t>=18000)show('selection');else if(state.screen==='battle'&&state.t>M.schedule(state.count).at(-1).impact+P.timing.idleAfterLastHitMs[state.kind]){state.t=0;state.feedback='';}render();}requestAnimationFrame(tick);}
controls();render();requestAnimationFrame(tick);
