(function(root){
'use strict';
const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&apos;'}[c]));
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
const round=v=>Math.floor(v+.5);
const num=n=>Math.round(n).toLocaleString('en-US');
const colors={ink:'#0b1725',panel:'#182d40',edge:'#456077',gold:'#e8bd66',cream:'#ffe1a0',muted:'#9cabb7',blue:'#c0e6f4',red:'#ec8174',green:'#9cd1ad'};
function stats(star,L){const b=Math.max(0,Math.floor((L-1)/10)),q=[.5,.75,1,1.25,1.5][star-1];return {attack:round((10+4*q*L)*(2.3+.05*(star-1))**b),hp:round((100+24*q*L)*(1.7+.05*(star-1))**b),crit:Math.min(60,5+Math.floor(L*[2,3,4,5,6][star-1]/10)+star*b),gold:Math.min(150,L*[1,1,1,2,2][star-1]+star*b),resonance:Math.min(600,140+star*(L+10*b))};}
function schedule(count){return Array.from({length:count},(_,i)=>({cue:i*720,flashes:[i*720,i*720+240,i*720+480],impact:count*720+i*480,waveStart:count*720+i*480-480}));}
function wave(t,count,revision='proposed'){const q=schedule(count),i=q.findIndex(x=>t<=x.impact);if(i<0)return null;const h=q[i],start=revision==='current'?h.impact-600:h.waveStart;return t<start?null:{index:i,p:clamp((t-start)/(h.impact-start),0,1),start,impact:h.impact};}
function defend(at,impact,right=true){const lead=impact-at;return !right||lead<0||lead>160?'fail':lead<=60?'perfect':'normal';}
function upgrade(state,index,costs){const L=state.levels[index];if(L>=50)return {ok:false,reason:'MAX'};const cost=costs[L+1];if(state.gold<cost)return {ok:false,reason:'골드 부족',missing:cost-state.gold};state.gold-=cost;state.levels[index]++;return {ok:true,cost};}
function confirmRelic(state){if(state.selectedRelic==null||state.relicCommitted)return false;state.relicCommitted=true;return true;}
function text(x,y,s,size=20,fill=colors.cream,anchor='start',extra=''){return `<text x="${x}" y="${y}" font-size="${size}" fill="${fill}" text-anchor="${anchor}" ${extra}>${esc(s)}</text>`;}
function rect(x,y,w,h,fill=colors.panel,stroke=colors.edge,sw=1,extra=''){return `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;}
function button(x,y,w,h,label,action,active=false,enabled=true,size=20){return `<g role="button" tabindex="0" aria-label="${esc(label)}" data-action="${action}" data-enabled="${enabled}" opacity="${enabled?1:.45}">${rect(x,y,w,h,active?'#70572e':colors.panel,active?colors.gold:colors.edge,2)}${text(x+w/2,y+h/2+size*.35,label,size,colors.cream,'middle')}</g>`;}
function stars(n){return '★'.repeat(n)+'☆'.repeat(5-n);}
function bell(x,y,s=16,active=true,ordinal=''){return `<g transform="translate(${x} ${y})" opacity="${active?1:.25}"><path d="M${-.8*s} ${.45*s} L${-.5*s} ${.1*s} V${-.4*s} Q0 ${-1.15*s} ${.5*s} ${-.4*s} V${.1*s} L${.8*s} ${.45*s} Z M${-.15*s} ${.8*s} H${.15*s}" fill="none" stroke="${colors.gold}" stroke-width="3"/>${ordinal?text(0,s+17,ordinal,13,colors.cream,'middle'):''}</g>`;}
function icon(x,y,i,size=52){const s=size/52;return `<g transform="translate(${x} ${y}) scale(${s})" fill="${colors.gold}" stroke="${colors.cream}" stroke-width="2">${i%3===0?'<circle cx="26" cy="26" r="18" fill="none" stroke-width="6"/><path d="M26 10 V42 M10 26 H42" stroke-width="6"/>':i%3===1?'<path d="M8 8 H43 V23 H31 V46 H21 V23 H8Z"/>':'<path d="M26 2 L33 18 L48 26 L33 34 L26 50 L19 34 L4 26 L19 18Z"/>'}</g>`;}
function world(w,h){const sy=h/540;let out=rect(0,0,w,540,'#203b59','none');out+=`<circle cx="${w*.83}" cy="98" r="26" fill="#edc56c" opacity=".85"/>`;
for(let i=0;i<14;i++){let x=i*90-40;out+=rect(x,195+(i%3)*30,65,275,'#193249','none');out+=rect(x+18,165+(i%3)*30,28,70,'#193249','none');}
for(let i=0;i<7;i++){let x=i*180-30;out+=rect(x,250,145,200,'#112638','none');out+=rect(x-8,242,161,12,'#264358','none');}
out+=rect(0,442,w,98,'#091521','none');for(let i=0;i<Math.ceil(w/40);i++)out+=rect(i*40,442,36,10,'#435c75','none')+rect(i*40+8,456,33,16,'#182b41','none');return sy===1?out:`<g transform="scale(1 ${sy})">${out}</g>`;}
function sprite(uri,x,y,size,extra=''){return `<image href="${uri}" x="${x}" y="${y}" width="${size}" height="${size}" style="image-rendering:pixelated" ${extra}/>`;}
function scene(state,assets,data){const w=state.w||960,h=state.h||540,c=h<=360,F=h/540;let body='',bounds=[];
function btn(x,y,bw,bh,label,action,active=false,enabled=true,size=20){bounds.push({action,x,y,w:bw,h:bh,enabled});return button(x,y,bw,bh,label,action,active,enabled,size);}
const clip=`<defs><clipPath id="screen"><rect width="${w}" height="${h}"/></clipPath><filter id="glow"><feGaussianBlur stdDeviation="6"/></filter><filter id="hit-white"><feColorMatrix type="matrix" values="0 0 0 0 1 0 0 0 0 1 0 0 0 0 1 0 0 0 1 0"/></filter></defs>`;
if(state.screen==='current') {body=`<image href="${assets[state.currentAsset||'currentLobby']}" width="${w}" height="${h}" preserveAspectRatio="xMidYMid meet"/>`;}
else if(state.screen==='prologue'){
 body=world(w,h)+rect(0,0,w,h,'#07101c','none',0,'opacity=".35"');let sw=c?440:620,sh=c?248:368,x=(w-sw)/2,y=c?28:42;
 body+=rect(x-9,y,sw+18,sh,'#654829','none')+rect(x,y+10,sw,sh-20,'#e4d1a1','#a98348',3)+rect(x-13,y,sw+26,13,'#c49c61','#775532',3)+rect(x-13,y+sh-13,sw+26,13,'#c49c61','#775532',3);
 body+=`<circle cx="${x-36}" cy="${y+64}" r="70" fill="#ffc957" opacity=".12" filter="url(#glow)"/>`+rect(x-48,y-12,4,55,'#8b693b','none')+rect(x-59,y+38,28,54,'#b98436','none')+rect(x-53,y+45,16,39,'#ffcb54','none')+rect(x-49,y+51,8,26,'#fff0af','none');
 body+=text(w/2,y+42,'마지막 불씨',c?20:25,'#664723','middle');const t=clamp(state.t||0,0,17999),page=Math.floor(t/4500),phase=t%4500;
 let iy=y+(c?74:102);body+=`<g opacity=".5">${bell(w/2,iy,c?19:28)}</g>`;
 const paras=['종소리가 멎자,|마을의 등불이 꺼졌습니다.','어둠 속에 잠든 사람들은|아침을 기다리고 있습니다.','마지막 불씨를 지닌 종지기여.|등불을 밝히고 다섯 종을 깨우세요.','성문 너머, 첫 번째 종이 있습니다.|이제 당신의 여정이 시작됩니다.'];
 const yy=y+(c?132:204)-Math.min(12,phase/375);body+=`<g opacity="${Math.min(1,phase/400+.15)}">`+paras[page].split('|').map((s,i)=>text(w/2,yy+i*(c?31:39),s,c?18:25,'#513e2a','middle')).join('')+'</g>';
 for(let i=0;i<4;i++)body+=rect(w/2-54+i*29,y+sh-35,20,4,i<=page?'#795632':'#b6a078','none');
 body+=btn(24,h-62,c?130:180,48,state.paused?'계속':'일시정지','pause-prologue',false,true,c?17:20)+btn(w/2-(c?80:100),h-62,c?160:200,48,'다음 문단','next-paragraph',false,true,c?17:20)+btn(w-(c?154:204),h-62,c?130:180,48,'건너뛰기','skip',false,true,c?17:20);
}
else if(state.screen==='lobby'){
 body=rect(0,0,w,h,colors.ink,'none');let rosterW=c?166:188,statX=c?194:410,rw=w-statX-24,top=c?62:78,rh=c?50:69,gap=c?5:10;
 body+=text(24,c?35:42,'종지기',c?22:28);body+=text(w-24,c?32:38,`골드 ${num(state.gold)} · 파편 4`,c?16:21,colors.cream,'end');
 ['온','담 · 제작 중','린 · 제작 중'].forEach((n,i)=>{body+=btn(16,(c?62:78)+i*(c?54:74),rosterW-16,c?48:62,n,'character-'+i,i===0,i===0,c?18:22);});
 if(c){body+=sprite(assets.hero,32,217,100)+text(128,257,'온',22)+text(128,282,'균형형',13,colors.muted);}
 else{body+=sprite(assets.hero,209,116,175)+text(294,316,'온',27,colors.cream,'middle')+text(294,346,'균형형 종지기',16,colors.muted,'middle');}
 const labels=['공격','체력','행운','공명'];for(let i=0;i<4;i++){let L=state.levels[i],a=stats(state.star,L),b=stats(state.star,Math.min(50,L+1)),y=top+i*(rh+gap),key=['attack','hp','crit','resonance'][i];body+=rect(statX,y,rw,rh,colors.panel,i===state.selectedStat?colors.gold:colors.edge,1.5);body+=text(statX+10,y+20,labels[i],c?16:20)+text(statX+(c?62:78),y+20,stars(state.star),c?12:14,colors.gold);body+=text(statX+rw-84,y+20,`Lv.${L}${[10,20,30,40].includes(L)?' → 돌파':''}`,c?12:15,colors.muted,'end');
 let value=i===2?`${a.crit} → ${b.crit}%  · 골드 +${a.gold} → +${b.gold}%`:i===3?`${a.resonance} → ${b.resonance}%`:`${num(a[key])} → ${num(b[key])}`;
 body+=text(statX+10,y+(c?41:49),value,c?14:22,i===2?colors.cream:colors.green);
 let price=data.catalog.economy.upgradeCosts[Math.min(50,L+1)],priceLabel=price>=1000000000?(price/1000000000).toFixed(2)+'B':price>=1000000?(price/1000000).toFixed(2)+'M':price>=10000?(price/1000).toFixed(1)+'K':num(price);
 body+=text(statX+rw-84,y+(c?41:49),L>=50?'':`${c?priceLabel:num(price)} G`,c?12:16,colors.gold,'end');
 body+=btn(statX+rw-72,y+(rh-48)/2,64,48,L>=50?'MAX':'＋','upgrade-'+i,false,L<50,c?22:27);
 }
 body+=text(statX,c?291:414,state.notice||(c?'별: 성장 적성 · Lv.: 강화 횟수':'별은 성장 적성입니다. 강화 레벨과는 달라요.'),c?12:16,colors.muted);
 body+=btn(statX,h-64,(rw-16)*.43,48,'유물','relics',false,true,c?18:22)+btn(statX+(rw-16)*.43+16,h-64,(rw-16)*.57,48,'11스테이지 출전','battle',true,true,c?18:22);
 if(!c)body+=btn(220,402,164,48,'프롤로그','prologue',false,true,17);
}
else if(state.screen==='relics'||state.screen==='collection'){
 body=world(w,h)+rect(0,0,w,h,'#07101b','none',0,'opacity=".45"');const isBook=state.screen==='collection',dw=isBook?(c?600:760):(c?480:560),dh=isBook?(c?324:432):(c?282:344),x=(w-dw)/2,y=(h-dh)/2;
 body+=rect(x,y,dw,dh,'#112333',colors.edge,2)+text(w/2,y+33,isBook?'유물 도감':'유물 하나를 선택하세요',c?20:24,colors.cream,'middle');
 if(isBook){let tw=c?72:92;for(let i=0;i<10;i++){let xx=x+20+(i%5)*(tw+8),yy=y+60+Math.floor(i/5)*(c?76:102);body+=btn(xx,yy,tw,c?68:92,'','book-'+i,i===state.bookItem,true,12)+icon(xx+(tw-42)/2,yy+5,i,42)+text(xx+tw/2,yy+(c?59:76),'Lv.'+(i%5+1),13,colors.cream,'middle');}
 let dx=x+dw-(c?172:210),item=data.catalog.relics[state.bookItem||0];body+=text(dx,y+84,item.displayName,c?16:22)+text(dx,y+115,'장착 중 · 온',c?13:16,colors.gold)+text(dx,y+147,'보유 유물은 공용',c?13:16,colors.muted)+text(dx,y+175,'장착 효과는 캐릭터별',c?13:16,colors.muted);
 body+=btn(dx,y+dh-112,c?150:180,48,'장착 유지','equip',true,true,c?16:19)+btn(x+dw-124,y+dh-56,104,48,'닫기','lobby',false,true,17);
 }else{
 const labels=['청동 매듭','종망치 파편','새벽의 조각'],cw=c?136:156,g=16,sx=x+(dw-cw*3-g*2)/2,cy=y+(c?48:56);
 labels.forEach((label,i)=>{body+=btn(sx+i*(cw+g),cy,cw,c?98:126,'','relic-'+i,state.selectedRelic===i,!state.relicCommitted)+icon(sx+i*(cw+g)+(cw-(c?48:66))/2,cy+8,i,c?48:66)+text(sx+i*(cw+g)+cw/2,cy+(c?84:109),label,c?16:19,colors.cream,'middle');});
 let line1='이미지를 눌러 효과를 확인하세요.',line2='';if(state.selectedRelic!=null){const i=state.selectedRelic;line1=state.relicMax?'최대 레벨 · 골드 2,399 획득':i===1?'Lv.2 → Lv.3 · 기절 적 피해 4 → 6%p':i===0?'신규 Lv.1 · 최대 체력 +6%':'신규 Lv.1 · 최대 체력 +6%';line2=state.relicCommitted?'획득했습니다. 로비에서 장착할 수 있어요.':'보유는 공용 · 장착한 종지기에게 적용';}
 body+=text(w/2,y+(c?178:224),line1,c?15:18,colors.cream,'middle')+text(w/2,y+(c?203:253),line2,c?13:16,colors.muted,'middle');body+=btn(w/2-90,y+dh-60,180,48,state.relicCommitted?'계속':'선택',state.relicCommitted?'lobby':'confirm',true,state.selectedRelic!=null,c?20:23);
 }
}
else if(state.screen==='result'){
 body=world(w,h)+rect(0,0,w,h,colors.ink,'none',0,'opacity=".75"');const dw=c?448:560,x=(w-dw)/2,y=c?24:76;
 body+=rect(x,y,dw,c?312:390,'#112333',colors.edge,2)+text(w/2,y+48,'17스테이지에서 쓰러졌습니다',c?23:29,colors.cream,'middle')+text(w/2,y+99,'획득 골드 3,487',c?24:30,colors.gold,'middle')+text(w/2,y+139,'종 파편 1 · 완벽 회피 12회',c?18:22,colors.muted,'middle')+text(w/2,y+188,'다음 도전은 11스테이지부터',c?17:21,colors.cream,'middle');
 body+=btn(x+20,y+(c?238:286),(dw-56)/2,56,'다시하기','battle',true,true,c?21:24)+btn(w/2+8,y+(c?238:286),(dw-56)/2,56,'로비','lobby',false,true,c?21:24);
}
else{
 const t=state.t||0,count=state.count||2,sc=schedule(count),resolved=sc.filter(a=>t>a.impact+12).length,impact=sc.findLast(a=>t>=a.impact),age=impact?t-impact.impact:Infinity;
 body=world(w,h);const sx=(w-960*F)/2,homeX=sx+375*F,homeY=442*F,enemyX=sx+675*F;
 const dirs=['back','up','down'],currentIndex=Math.min(count-1,resolved),active=dirs[currentIndex];
 let dodge=0,heroDx=0,heroDy=0,attacking=false,good=state.outcome!=='fail';const lead=state.outcome==='perfect'?45:100,defence=sc.find(a=>t>=a.impact-lead&&t<a.impact+160);if(good&&defence){dodge=1;let dir=dirs[sc.indexOf(defence)],q=Math.min(1,(t-defence.impact+lead)/lead);heroDx=dir==='back'?-90*F*q:0;heroDy=dir==='up'?-108*F*q:0;}
 const attackStart=state.outcome==='perfect'?180:300;if(good&&!dodge&&age>=attackStart&&age<attackStart+340){attacking=true;const q=(age-attackStart)/340;heroDx=165*F*(q<.35?1-Math.pow(1-q/.35,3):1-Math.pow((q-.35)/.65,2));heroDy=0;}
 const strike=sc.find(a=>Math.abs(t-a.impact)<=120),enemyDx=strike?-160*F*(1-Math.abs(t-strike.impact)/120):0;
 // Direction clues remain world hit boxes, not verbal instructions.
 for(let i=0;i<count;i++){let local=t-sc[i].cue,flashing=local>=0&&local<720&&local%240<84,recalled=i===currentIndex&&t>=count*720-120&&t<=sc[i].impact;
 if(flashing||recalled){let r=i%3===0?[340,306,284,136]:i%3===1?[248,406,376,36]:[248,270,376,88];body+=rect(sx+r[0]*F,r[1]*F,r[2]*F,r[3]*F,colors.gold,colors.gold,1.5,`fill-opacity="${flashing?.16:.06}" stroke-opacity="${flashing?.85:.42}"${recalled?' stroke-dasharray="5 5"':''}`);}}
 const parrying=dodge&&defence&&dirs[sc.indexOf(defence)]==='down';body+=sprite(attacking||parrying?assets.slash:assets.hero,homeX-64*F+heroDx,homeY-114*F+heroDy,128*F);body+=sprite(assets.enemy,enemyX-94*F+enemyDx,homeY-173*F,192*F);
 const hitAge=age-attackStart-200;if(good&&hitAge>=0&&hitAge<120)body+=sprite(assets.enemy,enemyX-94*F+enemyDx,homeY-173*F,192*F,`filter="url(#hit-white)" opacity="${hitAge<45?1:1-(hitAge-45)/75}"`);
 if(good&&hitAge>=0&&hitAge<300){body+=text(enemyX,homeY-175*F-hitAge*.06,'130',c?24:32,colors.cream,'middle','stroke="#0b1725" stroke-width="3" paint-order="stroke"');for(let j=0;j<5;j++){let a=j*1.25;body+=rect(enemyX+Math.cos(a)*hitAge*.18,homeY-75*F+Math.sin(a)*hitAge*.12,4*F,4*F,colors.gold,'none',0,`opacity="${1-hitAge/300}"`);}}
 if(attacking){body+=`<path d="M${homeX+heroDx+35*F} ${homeY-100*F} Q${homeX+heroDx+116*F} ${homeY-70*F} ${homeX+heroDx+56*F} ${homeY-15*F}" fill="none" stroke="${colors.cream}" stroke-width="${7*F}"/>`;}
 if(age>=0&&age<120){body+=`<path d="M${homeX-50*F} ${homeY-120*F} L${homeX+150*F} ${homeY-38*F}" stroke="${colors.gold}" stroke-width="${5*F}" opacity="${1-age/120}"/>`;}
 // Screen-space waves remain centered in the viewport regardless of character displacement.
 const wa=wave(t,count,state.revision||'proposed');if(wa){for(const x of [-w*.125,w*1.125])body+=`<circle cx="${x}" cy="${h/2}" r="${w*.625*wa.p}" fill="none" stroke="${colors.blue}" stroke-width="${c?3:4}" opacity=".8"/>`;}
 if(state.outcome==='perfect'&&age>=0&&age<340){body+=`<circle cx="${w/2}" cy="${h/2}" r="${Math.hypot(w,h)*.56*age/340}" fill="none" stroke="#fff0bb" stroke-width="${c?3:5}" opacity="${1-age/340}"/>`;}
 if(state.outcome==='fail'&&age>=0&&age<400)body+=text(homeX,homeY-140*F,'−180',c?25:32,colors.red,'middle');
 body+=rect(0,0,w,c?72:94,colors.ink,'none',0,'opacity=".68"')+text(22,c?28:36,'Stage 11',c?18:23)+text(22,c?54:66,'체력 655 / 655',c?14:18,colors.green)+rect(22,c?60:77,c?142:200,5,colors.green,'none');
 body+=text(w/2,c?28:34,'사슬 망령',c?18:22,colors.cream,'middle')+rect(w/2-(c?90:160),c?39:49,c?180:320,c?8:10,'#0e1c28',colors.edge)+rect(w/2-(c?90:160),c?39:49,(c?180:320)*.72,c?8:10,colors.gold,'none')+text(w/2,c?64:81,'864 / 1,200',c?12:15,colors.muted,'middle');
 body+=text(w-(c?78:106),c?28:36,'G 1,058',c?14:18,colors.gold,'end')+btn(w-(c?64:80),c?8:14,48,48,'Ⅱ','pause-battle',false,true,21);
 for(let i=0;i<count;i++){const bx=w/2+(i-(count-1)/2)*(c?40:48),by=c?93:121;body+=bell(bx,by,c?12:15,i>=resolved,String(i+1));if(i===currentIndex&&resolved<count)body+=rect(bx-19,by-23,38,56,'none',colors.blue,1.2);}
 const ctrl=c?{up:[108,148],back:[44,212],down:[108,276],forward:[172,212],size:56}:{up:[128,278],back:[56,350],down:[128,422],forward:[200,350],size:64};
 for(const [dir,label] of [['up','▲'],['back','◀'],['down','▼'],['forward','▶']]){const a=ctrl[dir];body+=btn(a[0],a[1],ctrl.size,ctrl.size,label,'dodge-'+dir,false,true,c?25:29);}
 body+=btn(w-(c?136:192),c?236:360,c?88:112,c?88:112,'⚔','attack',state.outcome==='perfect'&&age>=0&&age<500,true,c?37:48);
 if(state.feedback)body+=text(w/2,h-(c?22:38),state.feedback,c?16:22,colors.cream,'middle');
 }
const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" style="font-family:Malgun Gothic,Arial,sans-serif">${clip}<g clip-path="url(#screen)">${body}</g></svg>`;return {svg,bounds};
}
root.PreviewModel={stats,schedule,wave,defend,upgrade,confirmRelic,scene,esc};
root.PreviewDrawing={text,rect,button,stars,bell,icon,world,sprite,colors,clamp,num};
})(typeof window==='undefined'?globalThis:window);
