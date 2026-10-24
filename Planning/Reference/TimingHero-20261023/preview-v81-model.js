(function(root){
'use strict';
const P=root.PLAN_V81,B=root.PreviewModel,G=root.PreviewDrawing;
const {text,rect,button,stars,bell,icon,world,sprite,colors,clamp,num}=G;
const dirs=['back','up','down'],labels=['공격력','체력','행운','공명'];
function fresh(){return {screen:'selection',w:960,h:540,character:0,levels:[10,10,10,10],selectedStat:0,gold:12345,fragments:4,mode:'one',notice:'',t:0,count:2,kind:'boss',outcome:'perfect',paused:false,speed:1,feedback:'',scenario:'death17',queue:[],cursor:0,claimed:[],owned:{},selectedRelic:null,bookItem:0,runGold:0,runFragments:0,checkpoint:11,runStage:17,completed:false};}
function schedule(count){const t=P.timing;return Array.from({length:count},(_,i)=>({cue:i*t.cueMs,flashes:[i*t.cueMs,i*t.cueMs+t.beatMs,i*t.cueMs+t.beatMs*2],impact:count*t.cueMs+i*t.gapMs,waveStart:count*t.cueMs+i*t.gapMs-t.waveMs}));}
function wave(t,count){const q=schedule(count).find(a=>t<=a.impact);return !q||t<q.waveStart?null:{progress:clamp((t-q.waveStart)/P.timing.waveMs,0,1),...q};}
function defend(at,impact,right=true){const lead=impact-at;return !right||lead<0||lead>P.timing.dodgeActiveMs?'fail':lead<=P.timing.perfectMs?'perfect':'normal';}
function quote(s,data){const L=s.levels[s.selectedStat],costs=data.catalog.economy.upgradeCosts;let end=L,total=0;while(end<50){const next=costs[end+1];if(total+next>s.gold)break;total+=next;end++;if(s.mode==='one')break;}return {from:L,to:end,count:end-L,total,next:L<50?costs[L+1]:0,missing:L<50?Math.max(0,costs[L+1]-s.gold):0};}
function purchase(s,data){const q=quote(s,data);if(!q.count)return {ok:false,reason:q.from===50?'최대 레벨입니다.':`골드 ${num(q.missing)} 부족`};s.gold-=q.total;s.levels[s.selectedStat]=q.to;return {ok:true,...q};}
function bossFragments(kind){return kind==='boss'?P.rewards.bossFragments:0;}
function beginSettlement(s,key,data){const demo=P.scenarios[key];s.scenario=key;s.runStage=demo.stage;s.checkpoint=demo.checkpoint;s.completed=demo.completed;s.runGold=demo.gold;s.runFragments=demo.cleared.length*bossFragments('boss');s.fragments=s.runFragments;s.gold=demo.gold;s.cursor=0;s.claimed=[];s.selectedRelic=null;s.owned={};s.owned[data.catalog.relics[4].id]=2;s.queue=demo.cleared.map((stage,i)=>({rewardId:`demo-${key}:${stage}`,stage,ids:(i===0?[4,3,0]:[4,2,1]).map(k=>data.catalog.relics[k].id)}));s.screen='run-end';s.notice='';}
function currentOffer(s){return s.queue[s.cursor]||null;}
function relicQuote(s,data,id){const r=data.catalog.relics.find(r=>r.id===id),level=s.owned[id]||0;return {r,level,next:Math.min(5,level+1),max:level>=5};}
function claim(s,data){const offer=currentOffer(s);if(!offer||s.selectedRelic==null||s.claimed.includes(offer.rewardId)||!offer.ids.includes(s.selectedRelic))return false;const q=relicQuote(s,data,s.selectedRelic);if(q.max){const amount=data.catalog.economy.maxRelicGoldPerZone[Math.floor((offer.stage-1)/10)];s.gold+=amount;s.runGold+=amount;}else s.owned[s.selectedRelic]=q.next;s.claimed.push(offer.rewardId);s.cursor++;s.selectedRelic=null;s.screen=currentOffer(s)?'milestone':'result';return true;}
function heroStats(s,data,next=false){const ch=data.catalog.characters[s.character],L=s.levels;return {attack:B.stats(ch.stars[0],L[0]).attack,hp:B.stats(ch.stars[1],L[1]).hp,luck:B.stats(ch.stars[2],L[2]),resonance:B.stats(ch.stars[3],L[3]).resonance};}
function statValue(i,S){return i===0?num(S.attack):i===1?num(S.hp):i===2?`${S.crit}% · 골드 +${S.gold}%`:`${S.resonance}%`;}
function scene(s,A,D){const w=s.w,h=s.h,c=h<=360,F=h/540;let body='',bounds=[],regions=[];
function btn(x,y,bw,bh,label,action,active=false,enabled=true,size=20,diamond=false){bounds.push({x,y,w:bw,h:bh,action,enabled});if(!diamond)return button(x,y,bw,bh,label,action,active,enabled,size);return `<g role="button" tabindex="0" aria-label="${label}" data-action="${action}" data-enabled="${enabled}">${rect(x,y,bw,bh,'transparent','none')}<path d="M${x+bw/2} ${y} L${x+bw} ${y+bh/2} L${x+bw/2} ${y+bh} L${x} ${y+bh/2}Z" fill="#263e50" stroke="${active?colors.cream:colors.gold}" stroke-width="3"/>${text(x+bw/2,y+bh/2+size*.35,label,size,colors.cream,'middle')}</g>`;}
function pane(x,y,bw,bh,name){regions.push({x,y,w:bw,h:bh,name});return rect(x,y,bw,bh,colors.panel,colors.edge,2);}
function header(title){return text(w/2,c?36:43,title,c?25:30,colors.cream,'middle')+text(w-24,c?32:39,`◆ ${num(s.fragments)}`,c?16:20,colors.gold,'end');}
function bg(){return world(w,h)+rect(0,0,w,h,'#07101b','none',0,'opacity=".4"');}
if(s.screen==='selection'||s.screen==='upgrade'){
 const upgrade=s.screen==='upgrade',left=c?170:236,x=left+32,y=c?72:92,ph=h-y-78,rw=w-x-24;
 body=rect(0,0,w,h,colors.ink,'none')+header(upgrade?'능력치 강화':'종지기 선택');
 body+=pane(16,y,left,ph,upgrade?'STAT LIST':'CHARACTER LIST')+pane(x,y,rw,ph,upgrade?'UPGRADE DETAIL':'CHARACTER DETAIL');
 if(!upgrade){const ch=D.catalog.characters[s.character],stats=heroStats(s,D);D.catalog.characters.forEach((ch,i)=>body+=btn(24,y+12+i*(c?56:80),left-16,c?48:64,ch.displayName.split(' · ')[0]+(ch.availableInMvp?'':' · 제작 중'),'character-'+i,s.character===i,true,c?17:21));
 body+=text(x+18,y+(c?29:38),ch.displayName,c?20:26)+text(x+18,y+(c?55:74),`${['균형형','생존형','공격형'][s.character]} · 강화 합계 ${s.levels.reduce((a,b)=>a+b,0)}Lv.`,c?13:17,colors.muted)+text(x+18,y+(c?77:102),'최고 도달 17 · 다음 출전 11',c?13:17,colors.muted);
 const rows=[`공격력  ${num(stats.attack)}`,`체력  ${num(stats.hp)}`,`치명타  ${stats.luck.crit}%`, `공명  ${stats.resonance}%`];rows.forEach((v,i)=>body+=text(x+18,y+(c?108:150)+i*(c?22:30),v,c?15:21));
 if(ch.availableInMvp)body+=sprite(A.hero,w-(c?157:232),y+(c?74:104),c?128:196);else body+=text(w-(c?85:140),y+(c?140:220),'제작 중',c?20:27,colors.muted,'middle');
 body+=text(x+18,y+ph-14,`보유 골드 ${num(s.gold)}`,c?15:21,colors.gold)+btn(24,h-64,c?144:190,48,'유물 보기','collection',false,true,c?17:20)+btn(w-(c?188:224),h-64,c?164:200,48,ch.availableInMvp?'선택':'제작 중','select-character',true,ch.availableInMvp,c?20:24);
 }else{
 body+=btn(16,12,c?108:154,48,'뒤로가기','selection',false,true,c?16:19);
 const ch=D.catalog.characters[s.character];for(let i=0;i<4;i++){let yy=y+10+i*(c?48:68);body+=btn(24,yy,left-16,c?48:60,'','stat-'+i,s.selectedStat===i,true);body+=text(36,yy+(c?20:25),labels[i],c?16:21)+text(36,yy+(c?39:48),stars(ch.stars[i]),c?12:16,colors.gold);}
 const q=quote(s,D),i=s.selectedStat,L=s.levels[i],target=q.count?q.to:Math.min(50,L+1),a=B.stats(ch.stars[i],L),b=B.stats(ch.stars[i],target);
 body+=text(x+16,y+(c?35:50),`${labels[i]} Lv.${L} / 50`,c?17:25)+btn(w-(c?148:192),y+12,c?56:72,48,'1Lv.','mode-one',s.mode==='one',true,c?16:19)+btn(w-(c?86:108),y+12,c?56:80,48,'MAX','mode-max',s.mode==='max',true,c?15:18);
 let value=L===50?statValue(i,a):i===2?`${a.crit} → ${b.crit}%`:i===3?`${a.resonance} → ${b.resonance}%`:`${statValue(i,a)} → ${statValue(i,b)}`;
 body+=text(x+18,y+(c?94:140),value,c?28:40,colors.green);if(i===2)body+=text(x+18,y+(c?120:172),`골드 +${a.gold} → +${b.gold}%`,c?15:20,colors.green);
 body+=text(x+18,y+(c?150:222),L===50?'최대 레벨입니다.':`Lv.${L} → ${target} · ${num(q.count?q.total:q.next)} G 필요`,c?16:24,colors.gold)+text(x+18,y+(c?177:259),`보유 골드 ${num(s.gold)}`,c?14:19,colors.muted);
 body+=text(x+18,y+ph-10,s.notice||(q.count&&s.mode==='max'?`현재 골드로 ${q.count}회 강화합니다.`:L<50&&target>=11&&L<11?'Lv.11 돌파 · 능력치와 비용이 크게 증가합니다.':''),c?12:16,colors.muted);
 body+=btn(24,h-64,c?112:168,48,'유물 보기','collection',false,true,c?16:20)+btn(w/2-(c?75:100),h-64,c?150:200,48,L===50?'MAX':'강화','buy',true,L<50,c?20:24)+btn(w-(c?180:224),h-64,c?156:200,48,'전투 시작','battle',true,true,c?20:24);
 }
}else if(s.screen==='prologue'){
 body=bg();let pw=c?438:620,ph=c?252:384,x=(w-pw)/2,y=c?22:40;regions.push({x,y,w:pw,h:ph,name:'AUTO SCROLL'});
 body+=rect(x,y,pw,ph,'#e4d1a1','#9a7541',3)+rect(x-10,y,pw+20,12,'#bd955d','#6d4d29',2)+rect(x-10,y+ph-12,pw+20,12,'#bd955d','#6d4d29',2)+`<circle cx="${x-38}" cy="${y+60}" r="62" fill="#ffd266" opacity=".15"/>`+rect(x-52,y+28,26,52,'#956b30','none')+rect(x-46,y+35,14,37,'#ffdc83','none');
 body+=text(w/2,y+40,'마지막 불씨',c?21:27,'#674823','middle');const top=y+65,ih=ph-88,block=c?94:126,scroll=clamp(s.t,0,18000)/18000*block*3;
 body+=`<defs><clipPath id="story"><rect x="${x+16}" y="${top}" width="${pw-32}" height="${ih}"/></clipPath></defs><g clip-path="url(#story)">`;
 const ps=D.catalog.onboarding.prologueParagraphs;for(let i=0;i<4;i++)for(let [j,line] of ps[i].split('\n').entries())body+=text(w/2,top+ih*.6+i*block-scroll+j*(c?28:38),line,c?17:24,'#513e2a','middle');body+='</g>';
 body+=btn(w-(c?168:208),h-64,c?144:184,48,'건너뛰기','skip',false,true,c?18:22);
}else if(['run-end','milestone','relics','result'].includes(s.screen)){
 body=bg();let offer=currentOffer(s),pw=c?480:600,ph=c?284:366,x=(w-pw)/2,y=(h-ph)/2;body+=pane(x,y,pw,ph,'RUN REWARDS');
 if(s.screen==='run-end'){body+=text(w/2,y+(c?66:86),s.completed?'모든 전장을 돌파했습니다':'게임 오버',c?30:40,colors.cream,'middle')+text(w/2,y+(c?110:142),s.completed?'20스테이지 클리어':`${s.runStage}스테이지에서 쓰러졌습니다`,c?20:27,colors.muted,'middle')+text(w/2,y+(c?156:204),offer?`받을 유물 ${s.queue.length}개`:'이번 도전의 결과를 확인하세요.',c?18:24,colors.gold,'middle')+btn(w/2-95,y+ph-64,190,48,'계속','settle-next',true,true,22);
 }else if(s.screen==='milestone'){body+=bell(w/2,y+(c?60:86),c?27:38,true)+text(w/2,y+(c?126:174),`스테이지 ${offer?offer.stage:10} 클리어!`,c?28:37,colors.cream,'middle')+text(w/2,y+(c?166:220),'돌파 보상 · 유물 하나',c?18:23,colors.muted,'middle')+btn(w/2-95,y+ph-64,190,48,'유물 선택','open-relic',true,true,22);
 }else if(s.screen==='relics'){
 body+=text(w/2,y+32,`유물 선택 · ${offer?offer.stage:10}스테이지`,c?20:26,colors.cream,'middle');const ids=offer?offer.ids:D.catalog.relics.slice(0,3).map(x=>x.id),cw=c?136:168,gap=12,start=(w-3*cw-2*gap)/2,cy=y+48;
 ids.forEach((id,i)=>{const r=D.catalog.relics.find(r=>r.id===id),xx=start+i*(cw+gap);body+=btn(xx,cy,cw,c?94:126,'','relic-'+id,s.selectedRelic===id,true)+icon(xx+(cw-(c?46:62))/2,cy+8,i,c?46:62)+text(xx+cw/2,cy+(c?80:108),r.displayName,c?14:18,colors.cream,'middle');});
 let line='이미지와 이름을 보고 하나를 고르세요.',detail='';if(s.selectedRelic){const q=relicQuote(s,D,s.selectedRelic);line=q.max?'최대 레벨 · 골드로 전환':q.level?`Lv.${q.level} → Lv.${q.next}`:'신규 획득 · Lv.1';if(q.max)detail=`${num(D.catalog.economy.maxRelicGoldPerZone[Math.floor((offer.stage-1)/10)])} G`;else {const values=P.relicPercentValues[q.r.id]||q.r.values.map(x=>Math.round(x*100)),unit=['stunDamageBonus','critChanceBonus'].includes(q.r.effect)?'%p':'%';detail=`${q.r.effectLabel} ${q.level?values[q.level-1]+' → ':''}${values[q.next-1]}${unit}`;}}
 body+=text(w/2,y+(c?169:222),line,c?16:21,colors.cream,'middle')+text(w/2,y+(c?195:254),detail,c?15:20,colors.muted,'middle')+btn(w/2-90,y+ph-60,180,48,'선택','confirm-relic',true,s.selectedRelic!=null,c?20:23);
 }else {body+=text(w/2,y+(c?44:60),s.completed?'20스테이지 클리어':`${s.runStage}스테이지에서 쓰러졌습니다`,c?24:30,colors.cream,'middle')+text(w/2,y+(c?88:120),`획득 골드 ${num(s.runGold)}`,c?24:31,colors.gold,'middle')+text(w/2,y+(c?122:164),`종 파편 ${s.runFragments} · 유물 ${s.claimed.length}개`,c?19:24,colors.muted,'middle')+text(w/2,y+(c?165:224),`다음 도전은 ${s.checkpoint}스테이지부터`,c?17:22,colors.cream,'middle')+btn(x+20,y+ph-68,(pw-56)/2,52,'다시하기','retry',true,true,22)+btn(w/2+8,y+ph-68,(pw-56)/2,52,'로비','selection',false,true,22);}
}else if(s.screen==='collection'){
 const old=B.scene({...s,screen:'collection',bookItem:s.bookItem},A,D);return old;
}else if(s.screen==='battle'){
 body=rect(0,0,w,h,'#0b1725','none')+world(w,h*.80);const ground=h*.655,hx=c?224:w*.3125,ex=c?438:w*.7083,hs=c?106:150,es=c?146:210,barY=c?258:390,stage=s.kind==='boss'?20:s.checkpoint===1?1:11,enemyData=D.catalog.stages[stage-1],enemyName=D.catalog.enemies.find(x=>x.id===enemyData.enemyId).displayName;
 const scheduleNow=schedule(s.count),resolved=scheduleNow.filter(q=>s.t>q.impact).length,current=scheduleNow.find(q=>s.t<=q.impact),last=scheduleNow.findLast(q=>s.t>=q.impact),age=last?s.t-last.impact:Infinity;
 const lead=s.outcome==='perfect'?65:170,defence=scheduleNow.find(q=>s.t>=q.impact-lead&&s.t<q.impact+175),di=defence?scheduleNow.indexOf(defence):-1,good=s.outcome!=='fail';let dx=0,dy=0,attack=false;
 if(defence&&good){const q=clamp((s.t-defence.impact+lead)/lead,0,1);if(dirs[di]==='back')dx=-70*F*q;if(dirs[di]==='up')dy=-100*F*q;}
 const attackStart=s.outcome==='perfect'?180:380;if(!defence&&good&&age>=attackStart&&age<attackStart+500){attack=true;let a=age-attackStart;dx=(ex-hx-85*F)*(a<120?1-(1-a/120)**3:a<230?1:Math.max(0,1-(a-230)/270));}
 for(let i=0;i<s.count;i++){let local=s.t-scheduleNow[i].cue,flash=local>=0&&local<900&&local%300<100,recall=current===scheduleNow[i]&&s.t>=s.count*900-180;if(flash||recall){const r=i===0?[hx-20*F,ground-128*F,ex-hx-20*F,128*F]:i===1?[hx-95*F,ground-30*F,ex-hx+60*F,30*F]:[hx-95*F,ground-155*F,ex-hx+60*F,66*F];body+=rect(...r,colors.gold,colors.gold,1.3,`fill-opacity="${flash?.16:.05}" stroke-opacity="${flash?.85:.4}"${recall?' stroke-dasharray="5 5"':''}`);}}
 const strike=scheduleNow.find(q=>Math.abs(s.t-q.impact)<=140),edx=strike?-(ex-hx-50*F)*(1-Math.abs(s.t-strike.impact)/140):0,parry=defence&&di===2&&good;
 body+=sprite(attack||parry?A.slash:A.hero,hx-hs/2+dx,ground-hs*.89+dy,hs)+sprite(s.kind==='boss'?(A.boss||A.enemy):stage===1?(A.firstEnemy||A.enemy):(A.normalEnemy||A.enemy),ex-es/2+edx,ground-es*.9,es);
 if(strike&&s.t>=strike.impact)body+=`<path d="M${hx-35*F} ${ground-110*F} L${hx+120*F} ${ground-30*F}" stroke="${colors.gold}" stroke-width="${4*F}" opacity="${1-(s.t-strike.impact)/140}"/>`;
 if(attack)body+=`<path d="M${hx+dx+30*F} ${ground-100*F} Q${hx+dx+95*F} ${ground-55*F} ${hx+dx+40*F} ${ground-10*F}" fill="none" stroke="${colors.cream}" stroke-width="${5*F}"/>`;
 const wa=wave(s.t,s.count);if(wa)for(let x of [-w*.125,w*1.125])body+=`<circle cx="${x}" cy="${h/2}" r="${w*.625*wa.progress}" fill="none" stroke="${colors.blue}" stroke-width="${c?3:4}" opacity=".8"/>`;
 if(s.outcome==='perfect'&&age<340)body+=`<circle cx="${w/2}" cy="${h/2}" r="${Math.hypot(w,h)*.56*age/340}" fill="none" stroke="#ffe3a2" stroke-width="${c?3:5}" opacity="${1-age/340}"/>`;
 if(!good&&age<300)body+=text(hx,ground-hs-10,`−${enemyData.damage}`,c?24:34,colors.red,'middle');
 const hp=(x,width,p,label)=>rect(x-width/2,barY,width,c?10:13,'#0a1520',colors.edge,2)+rect(x-width/2+3,barY+3,(width-6)*p,c?4:7,colors.gold,'none')+text(x,barY+(c?27:38),label,c?13:18,colors.cream,'middle');
 const heroHp=heroStats(s,D).hp;body+=text(hx,barY-9,`${num(heroHp)} / ${num(heroHp)}`,c?11:14,colors.muted,'middle')+hp(hx,c?100:144,1,'온');
 body+=text(ex,barY-9,`${num(Math.round(enemyData.maxHp*.72))} / ${num(enemyData.maxHp)}`,c?11:14,colors.muted,'middle')+hp(ex,c?138:176,.72,`Lv.${stage} ${enemyName}`);
 body+=text(24,34,`획득 골드 ${num(1058)}`,c?16:21)+text(24,c?57:66,`종 파편 ${s.kind==='boss'?4:0}`,c?13:16,colors.muted)+btn(w-72,14,48,48,'Ⅱ','pause-battle',false,true,21);
 for(let i=0;i<s.count;i++){let bx=w/2+(i-(s.count-1)/2)*(c?40:48),by=c?86:106;body+=bell(bx,by,c?12:16,i>=resolved,String(i+1));if(i===resolved)body+=rect(bx-20,by-24,40,56,'none',colors.blue,1);}
 const controls=c?[[92,204,56,'▲','up'],[36,276,56,'◀','back'],[148,276,56,'▼','down']]:[[108,334,68,'▲','up'],[40,418,68,'◀','back'],[176,418,68,'▼','down']];
 for(let [x,y,sz,label,dir]of controls)body+=btn(x,y,sz,sz,label,'dodge-'+dir,false,true,c?24:29,true);
 body+=btn(w-(c?120:176),c?270:402,c?88:116,c?76:104,'⚔','attack',good&&s.outcome==='perfect'&&age<1200,true,c?37:49,true);
 if(s.feedback)body+=text(w/2,h-20,s.feedback,c?14:19,colors.cream,'middle');
 regions.push({x:0,y:0,w,h:78,name:'GOLD / PAUSE'},{x:w/2-80,y:c?60:78,w:160,h:72,name:'BELLS'},{x:hx-90,y:barY-22,w:ex-hx+180,h:70,name:'HP / LEVEL / NAME'});
}
return {svg:`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" style="font-family:Malgun Gothic,Arial,sans-serif"><defs><clipPath id="screen"><rect width="${w}" height="${h}"/></clipPath></defs><g clip-path="url(#screen)">${body}</g></svg>`,bounds,regions};
}
root.PreviewV81={fresh,schedule,wave,defend,quote,purchase,bossFragments,beginSettlement,currentOffer,relicQuote,claim,scene};
})(typeof window==='undefined'?globalThis:window);
