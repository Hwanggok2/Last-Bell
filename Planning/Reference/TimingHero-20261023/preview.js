'use strict';
const M=window.PreviewModel,D=window.REPORT_DATA,A=window.PREVIEW_ASSETS,$=id=>document.getElementById(id);
const fresh=()=>({screen:'lobby',w:960,h:540,star:3,levels:[10,10,10,10],gold:12345,selectedStat:0,notice:'',t:0,count:2,outcome:'perfect',revision:'proposed',paused:false,speed:1,selectedRelic:null,relicCommitted:false,relicMax:false,bookItem:0,feedback:'',currentAsset:'currentLobby'});
let state=fresh(),last=performance.now();
const explanations={lobby:'변경안: 별은 성장 적성, Lv.는 강화 횟수로 분리합니다. 구매 직전에 현재 → 다음 수치와 비용을 함께 보여줍니다. 담과 린은 현재 제작 중 상태를 유지합니다.',prologue:'변경안: 기존 18초·4문단 이야기를 유지하고 두루마리 안에서 한 문단씩 천천히 올립니다. 게임 시작을 늦추지 않도록 건너뛰기를 항상 노출합니다.',relics:'변경안: 화면 중앙의 작은 선택 창을 유지합니다. 후보는 이미지와 이름, 선택 후 하단에 신규/중복·이전/다음 수치, 별도 확정 버튼을 표시합니다. 도감의 전역 보유와 캐릭터별 장착은 구분합니다.',collection:'도감 배치 예시: 기존 10종의 아이콘은 자리 표시용 도형입니다. 도감은 보유와 장착 정보를 분리합니다. 이 미리보기에서는 실제 장착 슬롯을 편집하지 않습니다.',battle:'720ms 예고·240ms 깜빡임·480ms 연타 간격은 그대로입니다. 변경안은 첫 타격과 후속 타격의 파동을 모두 480ms로 통일합니다. 모든 방향을 먼저 예고한 뒤 순서대로 타격합니다. 중앙 원형 확산은 완벽 회피 시에만 나타납니다.',result:'결과 화면 유지·보강: 쓰러진 스테이지, 획득 재화, 다음 시작 지점을 즉시 알 수 있게 합니다. 10스테이지 클리어 후 다시하기는 11스테이지부터입니다.',current:'현재 게임의 저장 캡처입니다. 변경안과 기능 차이를 비교하기 위한 화면이며 여기서는 조작할 수 없습니다.'};
function render(){const out=M.scene(state,A,D);$('stage').innerHTML=out.svg;$('stage').style.maxWidth=state.w+'px';$('context').textContent=explanations[state.screen];if(state.screen==='battle'){$('scrub').max=M.schedule(state.count).at(-1).impact+1000;$('scrub').value=Math.round(state.t);$('clock').textContent=Math.round(state.t)+' ms';$('beats').textContent=M.schedule(state.count).map((q,i)=>`${i+1}타 예고 ${q.flashes.join(' / ')} → 타격 ${q.impact}ms`).join('　|　');}}
function controls(){for(const id of ['lobby','battle','relic','current'])$(id+'-options').hidden=state.screen!==(id==='relic'?'relics':id);$('timeline').hidden=state.screen!=='battle';$('screen').value=state.screen;$('play').textContent=state.paused?'재생':'일시정지';$('gold').value=state.gold;$('level').value=state.levels[state.selectedStat];}
function setScreen(s){state.screen=s;state.t=0;state.paused=false;state.feedback='';state.notice='';controls();render();}
function announce(t){$('status').textContent=t;}
function action(a){if(a.startsWith('upgrade-')){const i=+a.split('-')[1];state.selectedStat=i;const r=M.upgrade(state,i,D.catalog.economy.upgradeCosts);state.notice=r.ok?`${['공격','체력','행운','공명'][i]} 강화 · ${r.cost.toLocaleString()} G 사용`:r.reason==='MAX'?'최대 레벨입니다.':`골드 ${r.missing.toLocaleString()} 부족`;announce(state.notice);controls();}
else if(a.startsWith('relic-')){if(!state.relicCommitted)state.selectedRelic=+a.split('-')[1];}
else if(a==='confirm'){if(M.confirmRelic(state))announce(state.relicMax?'샘플 보상 확정: 골드 2,399':'샘플 유물 획득 확정');}
else if(a.startsWith('book-'))state.bookItem=+a.split('-')[1];
else if(a==='equip')announce('도감 배치 미리보기입니다. 실제 장착 슬롯 변경은 포함하지 않습니다.');
else if(a==='pause-prologue'||a==='pause-battle')state.paused=!state.paused;
else if(a==='next-paragraph'){state.t=(Math.floor(state.t/4500)+1)*4500;if(state.t>=18000)setScreen('lobby');}
else if(a==='skip')setScreen('lobby');
else if(['lobby','battle','prologue'].includes(a))setScreen(a);
else if(a==='relics')setScreen('collection');
else if(a.startsWith('dodge-')){if(state.paused){announce('재생 중 타이밍을 확인할 수 있습니다.');return;}let q=M.schedule(state.count).find(x=>x.impact>=state.t);if(q){const index=M.schedule(state.count).findIndex(x=>x.impact===q.impact),right=a==='dodge-'+['back','up','down'][index],result=M.defend(state.t,q.impact,right);state.feedback=(right?`${Math.round(q.impact-state.t)}ms 전 · `:'방향 불일치 · ')+({perfect:'완벽 회피',normal:'일반 회피',fail:'실패'})[result];announce('입력 표본: '+state.feedback);}}
else if(a==='attack')announce('공격 동작은 자동 시연합니다. 완벽 회피 직후의 제자리 복귀 → 전진 공격 순서를 확인하세요.');
controls();render();}
$('stage').addEventListener('click',e=>{let b=e.target.closest('[data-action]');if(b&&b.dataset.enabled==='true')action(b.dataset.action);});
$('stage').addEventListener('keydown',e=>{if(['Enter',' '].includes(e.key)){let b=e.target.closest('[data-action]');if(b&&b.dataset.enabled==='true'){e.preventDefault();action(b.dataset.action);}}});
window.addEventListener('keydown',e=>{if(state.screen!=='battle'||/INPUT|SELECT|BUTTON/.test(e.target.tagName))return;const k={ArrowLeft:'back',ArrowUp:'up',ArrowDown:'down',ArrowRight:'forward'}[e.key];if(k){e.preventDefault();if(!e.repeat)action('dodge-'+k);}});
$('screen').onchange=e=>setScreen(e.target.value);
$('viewport').onchange=e=>{[state.w,state.h]=e.target.value.split(',').map(Number);render();};
$('star').onchange=e=>{state.star=+e.target.value;state.notice='';render();};
$('level').onchange=e=>{let L=Math.max(0,Math.min(50,Math.trunc(+e.target.value)||0));state.levels=[L,L,L,L];state.notice='';controls();render();};
$('gold').onchange=e=>{state.gold=Math.max(0,Math.min(99999999999,Math.trunc(+e.target.value)||0));render();};
$('broke').onclick=()=>{state.gold=0;controls();render();};$('maxed').onclick=()=>{state.levels=[50,50,50,50];state.notice='모든 능력치가 최대 레벨입니다.';controls();render();};
$('reset').onclick=()=>{state=fresh();$('viewport').value='960,540';$('star').value='3';$('count').value='2';$('outcome').value='perfect';$('revision').value='proposed';$('relic-max').checked=false;$('speed').value='1';controls();render();};
$('count').onchange=e=>{state.count=+e.target.value;state.t=0;render();};$('outcome').onchange=e=>{state.outcome=e.target.value;render();};$('revision').onchange=e=>{state.revision=e.target.value;render();};
$('play').onclick=()=>{state.paused=!state.paused;controls();};$('restart').onclick=()=>{state.t=0;state.feedback='';state.paused=false;controls();};$('speed').onchange=e=>state.speed=+e.target.value;
$('scrub').oninput=e=>{state.t=+e.target.value;state.paused=true;controls();render();};$('capture').onchange=e=>{state.currentAsset=e.target.value;render();};$('relic-max').onchange=e=>{state.relicMax=e.target.checked;state.relicCommitted=false;render();};$('new-relic').onclick=()=>{state.selectedRelic=null;state.relicCommitted=false;render();};
document.addEventListener('visibilitychange',()=>{last=performance.now();});
function frame(now){const dt=Math.min(now-last,100);last=now;if(!document.hidden&&!state.paused&&['battle','prologue'].includes(state.screen)){state.t+=dt*state.speed;if(state.screen==='prologue'&&state.t>=18000)setScreen('lobby');else if(state.screen==='battle'&&state.t>M.schedule(state.count).at(-1).impact+1000){state.t=0;state.feedback='';}render();}requestAnimationFrame(frame);}
controls();render();requestAnimationFrame(frame);
