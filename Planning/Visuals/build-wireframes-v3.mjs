import {writeFileSync} from "node:fs";
import {dirname,join} from "node:path";
import {fileURLToPath} from "node:url";

const model={
  characters:[
    {id:"on",name:"온",stats:[3,3,3,3]},
    {id:"dam",name:"담",stats:[2,5,2,3]},
    {id:"rin",name:"린",stats:[4,2,4,2]},
  ],
  statNames:["공격","체력","행운","공명"],
  sampleGold:0,sampleFragments:0,maxLevel:50,
  regularUpgradeCosts:[10,20,30,40,50],breakthroughUpgradeCosts:[60,90,120,150],
  slotCosts:[4,8],slotClearRequirements:[10,20],exchangeFragments:1,exchangeGold:100,
  status:"V3 layout; integrated growth/economy contract, browser review pending",
};
function upgradeCost(level,m=model){
  if(level<1||level>m.maxLevel)return 0;
  const tier=Math.max(0,Math.floor((level-1)/10));
  return level>1&&level%10===1?m.breakthroughUpgradeCosts[tier-1]:m.regularUpgradeCosts[tier];
}
function statValue(index,level,stars){
  const q=1+.05*(stars-1),b=Math.max(0,Math.floor((level-1)/10)),E=q*(level+10*b);
  if(index===0)return ((10+3*q*level)*(2+.15*(stars-1))**b).toFixed(2);
  if(index===1)return String(Math.round((100+20*q*level)*(1.6+.1*(stars-1))**b));
  if(index===2)return `치명${(Math.min(.3,.05+.0015*E)*100).toFixed(1)}% 골드+${(Math.min(.2,.0018*E)*100).toFixed(1)}%`;
  return `기절 적 ×${Math.min(1.75,1.4+.002*E).toFixed(2)}`;
}

function renderV3(kind,opts={},m=model){
  const {width:W=960,height:H=540,left=24,right=24,top=12,bottom=12,state="normal",color=false,large=false,guides=false,
    character="on",levels=[0,0,0,0],gold=m.sampleGold,fragments=m.sampleFragments,stage=1,ng=0,checkpoint=1,
    motion="",damage="",reduced=false,slotCount=1,highestClear=0,requiredGold=10}=opts;
  const L=left,R=W-right,T=top,B=H-bottom,SW=R-L,SH=B-T,small=H<400;
  const K=small?48:64,G=small?6:8,P=small?2:4,F=Math.round(H*.82/P)*P;
  const c=m.characters.find(v=>v.id===character)||m.characters[0],items=[],regions=[];
  const p=color?{bg:"#101d2c",mid:"#304b62",ink:"#efd39d",hot:"#d89335",paper:"#101d2c",line:"#70818a"}:
    {bg:"#fff",mid:"#ddd",ink:"#222",hot:"#666",paper:"#fff",line:"#999"};
  const esc=s=>String(s).replace(/[&<>"']/g,v=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&apos;"}[v]));
  const rect=(x,y,w,h,fill=p.paper,stroke=p.ink,extra="")=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}" stroke="${stroke}" ${extra}/>`;
  const text=(x,y,s,size=16,align="start",fill=p.ink,bold=false,leading=1.3)=>{
    const fs=large?Math.round(size*1.2):size;
    return `<text x="${x}" y="${y}" font-size="${fs}" text-anchor="${align}" fill="${fill}" font-weight="${bold?700:400}">${String(s).split("\n").map((line,i)=>`<tspan x="${x}" dy="${i?Math.ceil(fs*leading):0}">${esc(line)}</tspan>`).join("")}</text>`;
  };
  const item=(id,x,y,w,h,anchor,priority,hit,svg,extra={})=>{
    items.push({id,x,y,w,h,anchor,priority,hit,...extra});
    return `<g data-id="${id}" data-bounds="${x},${y},${w},${h}" data-anchor="${anchor}" data-priority="${priority}" data-hit="${hit}"${extra.action?` data-action="${extra.action}" role="button" tabindex="${hit?0:-1}" aria-label="${esc(extra.accessible||id)}"`:""}>${svg}</g>`;
  };
  const label=(id,x,y,w,h,s,size=16,anchor="TL",priority=2,center=false)=>
    item(id,x,y,w,h,anchor,priority,false,text(center?x+w/2:x,y+(large?Math.round(size*1.2):size)+3,s,size,center?"middle":"start"));
  const bell=(x,y,size,filled=false,fill=p.ink)=>`<g transform="translate(${x},${y}) scale(${size/12})"><path d="M5 1h2v2h2v2h1v4h1v1H1V9h1V5h1V3h2Z M5 11h2" fill="${filled?fill:"none"}" stroke="${fill}" stroke-width="1"/></g>`;
  const star=(x,y,size,filled=false)=>`<g transform="translate(${x},${y}) scale(${size/12})"><path d="M6 .8 7.6 4.1 11.3 4.6 8.7 7.2 9.3 10.8 6 9.1 2.7 10.8 3.3 7.2 .7 4.6 4.4 4.1Z" fill="${filled?p.ink:"none"}" stroke="${p.ink}" stroke-width=".8"/></g>`;
  const sword=(x,y,size,fill=p.ink)=>`<g transform="translate(${x},${y}) scale(${size/32})"><path d="M24 2l6 0 0 6-14 14-6-6z M6 14l12 12-3 3L3 17z M7 22l4 4-6 6-4-4z" fill="${fill}" stroke="${fill}" stroke-width="1"/></g>`;
  const triangle=(x,y,w,dir,fill=p.ink)=>{
    const cx=x+w/2,cy=y+w/2,r=w*.23;
    const points={up:`${cx},${cy-r} ${cx-r},${cy+r} ${cx+r},${cy+r}`,down:`${cx-r},${cy-r} ${cx+r},${cy-r} ${cx},${cy+r}`,
      back:`${cx-r},${cy} ${cx+r},${cy-r} ${cx+r},${cy+r}`,front:`${cx+r},${cy} ${cx-r},${cy-r} ${cx-r},${cy+r}`};
    return `<polygon points="${points[dir]}" fill="${fill}"/>`;
  };
  const button=(id,x,y,w,h,s,primary=false,disabled=false,anchor="B",action=id,icon="")=>{
    const fs=large?19:16,lines=String(s).split("\n").length;
    const fill=primary&&!disabled?p.paper:p.ink;
    let content=rect(x,y,w,h,disabled?p.mid:primary?p.ink:p.paper);
    if(icon==="sword")content+=sword(x+(w-32)/2,y+(h-32)/2,32,fill);
    else if(["up","back","down","front"].includes(icon))content+=triangle(x,y,w,icon,fill);
    else content+=text(x+w/2,y+h/2+(large?7:6)-(lines-1)*Math.ceil(fs*1.25)/2,s,16,"middle",fill,true,1.25);
    return item(id,x,y,w,h,anchor,primary||id.startsWith("dodge-")||id==="attack"?1:3,!disabled,content,{control:true,action,accessible:s||({up:"위로 회피",back:"뒤로 회피",down:"아래로 회피",front:"앞으로 회피",sword:"공격"}[icon])});
  };
  const panel=(id,x,y,w,h,svg="",anchor="C")=>item(id,x,y,w,h,anchor,2,false,rect(x,y,w,h,p.paper,p.line)+svg);
  const modal=(title,body,yes="확인",no="닫기",action="close")=>{
    items.forEach(i=>{i.hit=false;});
    const w=Math.min(488,SW-24),h=228,x=L+(SW-w)/2,y=T+(SH-h)/2,bw=(w-48)/2;
    return rect(0,0,W,H,p.bg,"none",'fill-opacity=".93"')+panel("modal",x,y,w,h)+
      label("modal-title",x+16,y+12,w-32,36,title,22,"C",1,true)+
      label("modal-body",x+20,y+62,w-40,80,body,16,"C",2,true)+
      button("modal-primary",x+16,y+h-64,bw,48,yes,true,false,"C",action)+
      button("modal-close",x+32+bw,y+h-64,bw,48,no,false,false,"C","close");
  };
  const hero=(x,y,scale,variant="on")=>{
    const body=color&&variant==="dam"?p.mid:color&&variant==="rin"?p.hot:p.ink;
    const cells=[[5,0,7,3,body],[3,3,11,5,body],[4,5,7,6,p.bg],[9,6,2,3,p.hot],[4,11,9,8,body],[2,12,3,6,body],
      [12,12,4,3,p.hot],[5,19,3,5,p.mid],[10,19,3,5,p.mid],[4,23,4,1,p.ink],[10,23,4,1,p.ink]];
    return `<g shape-rendering="crispEdges">${cells.map(([a,b,w,h,f])=>rect(x+a*scale,y+b*scale,w*scale,h*scale,f,"none")).join("")}</g>`;
  };
  const enemy=(x,y,scale)=>`<g shape-rendering="crispEdges">${[[8,0,8,4,p.ink],[4,4,16,4,p.ink],[2,8,20,12,p.mid],[4,20,16,8,p.ink],[4,28,6,8,p.hot],[14,28,6,8,p.hot],[3,34,7,2,p.ink],[14,34,7,2,p.ink],[0,12,4,18,p.hot],[20,12,4,14,p.ink]].map(([a,b,w,h,f])=>rect(x+a*scale,y+b*scale,w*scale,h*scale,f,"none")).join("")}</g>`;
  const digitMap={0:["111","101","101","101","111"],1:["010","110","010","010","111"],2:["111","001","111","100","111"],3:["111","001","111","001","111"],4:["101","101","111","001","001"],5:["111","100","111","001","111"],6:["111","100","111","101","111"],7:["111","001","010","010","010"],8:["111","101","111","101","111"],9:["111","101","111","001","111"]};
  const digits=(x,y,value,scale=3)=>`<g shape-rendering="crispEdges">${String(value).split("").map((n,i)=>(digitMap[n]||[]).map((row,r)=>row.split("").map((bit,col)=>bit==="1"?rect(x+(i*4+col)*scale,y+r*scale,scale,scale,p.hot,"none"):"").join("")).join("")).join("")}</g>`;
  let body=rect(0,0,W,H,p.bg),world="",hud="";
  if(kind==="battle"||kind==="tutorial"){
    const cross=3*K+2*G,lx=L+12,ly=B-cross-12,aw=small?72:96,ah=small?72:88;
    const leftZone={id:"left-thumb",x:0,y:B-cross-32,w:L+cross+28,h:cross+32};
    const rightZone={id:"right-thumb",x:R-aw-56,y:B-ah-56,w:W-R+aw+56,h:ah+56};
    regions.push(leftZone,rightZone);
    const PL=leftZone.w+12,PR=rightZone.x-12,PW=PR-PL,hx=Math.round((PL+PW*.28)/P)*P,ex=Math.round((PL+PW*.78)/P)*P;
    const hw=16*P,hh=24*P,ew=24*P,eh=36*P,jump=small?32:64,travel=small?24:48;
    const isTutorial=kind==="tutorial",tutorial2=state==="attack",boss=state==="boss5"||state==="boss10";
    if(color)world+=rect(0,Math.round(H*.2/P)*P,W,P*8,p.mid,"none")+
      Array.from({length:Math.ceil(W/(P*44))},(_,i)=>rect(i*P*44,P*12,P*5,F-P*12,p.mid,"none")).join("");
    world+=rect(0,F,W,H-F,p.mid,"none")+`<path d="M0 ${F}H${W}" stroke="${p.ink}"/>`;
    let dx=motion==="back"?-travel:motion==="front"?travel:0,dy=motion==="up"?-jump:motion==="down"?hh/3:0;
    world+=item("hero",hx-hw/2,F-hh,hw,hh,"C",1,false,
      `<g class="${motion?"dodge-motion":""}" style="--dodge-x:${dx}px;--dodge-y:${dy}px" transform="translate(${dx},${dy})"><g${motion==="down"?` transform="translate(${hx-hw/2},${F-hh}) scale(1,.67) translate(${-hx+hw/2},${-F+hh})"`:""}>${hero(hx-hw/2,F-hh,P,c.id)}</g></g>`,{world:true,critical:true});
    world+=item("movement-envelope",hx-hw/2-travel,F-hh-jump,hw+2*travel,hh+jump,"C",1,false,
      guides?rect(hx-hw/2-travel,F-hh-jump,hw+2*travel,hh+jump,"none",p.line,'stroke-dasharray="3 4"'):"",{world:true,critical:true});
    world+=item("enemy",ex-ew/2,F-eh,ew,eh,"C",1,false,enemy(ex-ew/2,F-eh,P),{world:true,critical:true});
    const danger=state==="upper"?"up":state==="rear"?"back":state==="front"?"front":"down";
    const activeDanger=!["stunned","hit","critical","player-hit"].includes(state)&&!(isTutorial&&tutorial2);
    let warning="",wx,wy,ww,wh;
    if(danger==="up"){wx=hx-hw/2-8;wy=F-hh-20;ww=hw+16;wh=12;warning=rect(wx,wy,ww,4,p.hot,"none")+[0,1,2,3].map(i=>rect(wx+i*ww/4,wy+4,4,8,p.hot,"none")).join("");}
    else if(danger==="down"){wx=hx-hw/2-8;wy=F-8;ww=hw+16;wh=8;warning=Array.from({length:Math.floor(ww/10)},(_,i)=>`<path d="M${wx+i*10} ${wy+8}l4 -8 4 8Z" fill="${p.hot}"/>`).join("");}
    else{wx=danger==="back"?hx-hw/2-16:hx+hw/2+8;wy=F-hh+4;ww=8;wh=hh-8;warning=rect(wx,wy,8,wh,p.hot,"none");}
    if(activeDanger)hud+=item("danger",wx,wy,ww,wh,"C",1,false,warning,{critical:true});
    if(state==="stunned")world+=item("stun-stars",ex-ew/2,F-eh-24,ew,20,"C",1,false,
      [ex-16,ex,ex+16].map((x,i)=>`<path d="M${x} ${F-eh-22+i%2*4}v12m-6 -6h12" stroke="${p.hot}" stroke-width="3"/>`).join(""),{world:true,critical:true});
    if(damage||["hit","critical","player-hit"].includes(state)){
      const target=state==="player-hit"?hx:ex,ty=state==="player-hit"?F-hh:F-eh,amount=damage|| (state==="critical"?"48":"24");
      world+=item("damage-popup",target-20,ty-28,44,20,"C",1,false,`<g class="damage-rise">${digits(target-18,ty-26,amount,small?2:3)}</g>`,{world:true,critical:true});
      world+=item("hit-fragments",target-(small?28:48),ty+(small?16:28),small?56:96,small?40:72,"C",1,false,
        [[-22,8],[20,0],[-28,28],[26,32],[4,-4]].map(([a,b],i)=>`<g class="fragment-fall" style="animation-delay:${i*12}ms">${rect(target+a*(small?1:1.6),ty+(small?18:40)+b*(small?.7:1),P+(i%2)*P,P+(i%2)*P,p.hot,"none")}</g>`).join(""),{world:true,critical:true});
    }
    const whp=small?48:80,hpy=F+(small?6:8);
    hud+=item("player-hp",hx-whp/2,hpy,whp,small?5:6,"C",2,false,rect(hx-whp/2,hpy,whp,small?5:6,p.bg,p.ink)+rect(hx-whp/2,hpy,whp*(state==="player-hit"?.58:1),small?5:6,p.ink,"none"),{critical:true});
    if(!boss)hud+=item("enemy-hp",ex-whp/2,hpy,whp,small?5:6,"C",2,false,rect(ex-whp/2,hpy,whp,small?5:6,p.bg,p.ink)+rect(ex-whp/2,hpy,whp*.7,small?5:6,p.hot,"none"),{critical:true});
    else hud+=item("boss-hp",L+156,T+16,SW-232,12,"T",2,false,rect(L+156,T+16,SW-232,12,p.mid,p.line)+rect(L+156,T+16,(SW-232)*.72,12,p.ink,"none"));
    const gaugeW=Math.min(480,SW-224),gx=(L+R-gaugeW)/2,gy=T+68;
    hud+=item("bell-meter",gx-32,gy,gaugeW+32,24,"T",1,false,bell(gx-30,gy,24,state==="stunned")+
      rect(gx,gy+5,gaugeW,14,p.mid,p.line)+rect(gx,gy+5,gaugeW*(state==="stunned"?.85:state==="resist"?.08:.45),14,state==="resist"?p.line:p.hot,"none"));
    hud+=label("stage",L+12,T+8,128,32,`${boss?state==="boss10"?10:5:stage} / 50`,18,"TL",2)+
      button("pause",R-60,T+8,48,48,"Ⅱ",false,false,"TR","pause");
    const keys=[["up",lx+K+G,ly],["back",lx,ly+K+G],["down",lx+K+G,ly+2*(K+G)],["front",lx+2*(K+G),ly+K+G]];
    for(const [dir,x,y] of keys)hud+=button(`dodge-${dir}`,x,y,K,K,"",isTutorial&&!tutorial2&&dir==="up",isTutorial&&(tutorial2||dir!=="up"),"BL",`dodge-${dir}`,dir);
    hud+=button("attack",R-aw-12,B-ah-12,aw,ah,"",isTutorial&&tutorial2||state==="stunned",isTutorial&&!tutorial2,"BR","attack","sword");
    if(isTutorial)hud+=panel("tutorial-copy",(L+R)/2-116,T+112,232,52,text((L+R)/2,T+146,tutorial2?"공격해!":"▲를 눌러 피해!",20,"middle",p.ink,true));
    body+=`<g data-world="true" class="${state==="critical"&&!reduced?"world-shake":""}">${world}</g><g data-hud="true">${hud}</g>`;
    if(state==="paused")body+=modal("잠시 멈춤",reduced?"흔들림 줄이기 켜짐":"흔들림 줄이기 꺼짐","계속","로비","resume");
  }else if(kind==="lobby"){
    const listW=SW*.25,previewX=L+listW+12,previewW=small?84:156,sx=previewX+previewW+12,sw=R-12-sx;
    const rowY=T+(small?48:68),rh=small?52:78,rg=small?2:8,cardH=small?62:80;
    body+=panel("character-list",L,T,listW,SH,"","L");
    body+=label("roster-title",L+12,T+8,listW-24,32,"종지기",20,"TL",2)+
      label("wallet",Math.max(L+listW+12,R-324),T+8,Math.min(312,SW-listW-24),32,`골드 ${gold} · 종 파편 ${fragments}`,16,"TR",2);
    m.characters.forEach((ch,i)=>{
      const y=T+(small?56:72)+i*(cardH+6),selected=ch.id===c.id;
      body+=button(`character-${ch.id}`,L+8,y,listW-16,cardH,ch.name,selected,false,"TL",`character-${ch.id}`);
    });
    const py=small?T+130:T+180,ps=small?2:4;
    body+=item("idle-preview",previewX+(previewW-16*ps)/2,py,16*ps,24*ps,"C",2,false,`<g class="idle-preview">${hero(previewX+(previewW-16*ps)/2,py,ps,c.id)}</g>`,{world:true});
    body+=label("character-name",previewX,py+24*ps+8,previewW,36,c.name,22,"C",1,true)+
      label("ng",previewX,T+(small?64:96),previewW,44,`NG+ ${ng}`,14,"C",2,true);
    for(let i=0;i<4;i++){
      const y=rowY+i*(rh+rg),lv=Math.min(m.maxLevel,levels[i]||0),tier=lv===0?1:Math.ceil(lv/10),fill=lv===0?0:(lv-1)%10+1;
      const fs=small?11:16,cell=Math.min(32,Math.floor((sw-76)/10)),bs=small?12:16,by=y+(small?38:55);
      const starSize=small?10:14,starX=sx+(small?42:60),starY=y+(small?3:4);
      body+=panel(`stat-${i}`,sx,y,sw,rh,text(sx+8,y+(large?Math.round(fs*1.2):fs)+3,m.statNames[i],fs)+
        `<g role="img" aria-label="${m.statNames[i]} 적성 ${c.stats[i]}/5">${Array.from({length:5},(_,n)=>star(starX+n*(starSize+2),starY,starSize,n<c.stats[i])).join("")}</g>`+
        text(sx+sw-64,y+(large?Math.round(fs*1.2):fs)+3,`Lv.${lv} · ${tier}단계`,small?11:14,"end")+
        text(sx+8,y+(small?32:46),`${statValue(i,lv,c.stats[i])} · ${lv===m.maxLevel?"최대":upgradeCost(lv+1,m)+"G"}`,small?10:14)+
        Array.from({length:10},(_,n)=>bell(sx+8+n*cell,by,bs,n<fill)).join(""));
      body+=button(`upgrade-${i}`,sx+sw-(small?52:60),y+(rh-(small?48:56))/2,small?48:56,small?48:56,"+",false,lv>=m.maxLevel,"R",`upgrade-${i}`);
    }
    const footerY=B-60,bw=Math.floor((R-previewX-36)/3);
    body+=button("slots",previewX,footerY,bw,48,"슬롯",false,false,"B","slots")+
      button("exchange",previewX+bw+12,footerY,bw,48,"교환",false,false,"B","exchange")+
      button("start",previewX+2*(bw+12),footerY,bw,48,`${checkpoint}층 출전`,true,false,"B","start");
    if(["death9","death19","next11","ngplus"].includes(state)){
      const message=state==="death9"?"9층 사망":state==="death19"?"19층 사망":state==="next11"?"10층 돌파":"새 회차";
      body+=label("lobby-status",previewX,B-104,previewW,32,message,12,"B",2,true);
    }
    if(state==="insufficient")body+=modal("골드가 부족해요",`필요 골드 ${requiredGold}`,"확인","닫기");
    if(state==="settings")body+=modal("설정",reduced?"흔들림 줄이기 켜짐":"흔들림 줄이기 꺼짐","바꾸기","닫기","toggle-reduced");
  }else if(kind==="progress"){
    const end=state==="ending",y=T+72,w=(SW-36)/2;
    body+=label("title",L+12,T+8,SW-24,40,end?"마침내, 아침이 돌아왔다.":"10층 돌파",22,"TL",1)+
      panel("progress-card",L+12,y,w,SH-152,text(L+28,y+40,end?`${c.name} · NG+ ${ng+1}`:"체크포인트 11층",22)+text(L+28,y+82,end?"다음 출전은 더 강한 1층":"11층으로 이어진다",16))+
      panel("character-progress",L+24+w,y,w,SH-152,text(L+40+w,y+40,c.name,22)+text(L+40+w,y+82,"이 종지기의 기록에 저장",16))+
      button("progress-continue",R-220,B-64,208,48,end?"로비로":"11층으로",true,false,"BR",end?"finish50":"finish10")+
      button("progress-home",L+12,B-64,144,48,end?"건너뛰기":"귀환",false,false,"BL",end?"finish50":"return10");
  }else if(kind==="slots"||kind==="exchange"){
    const y=T+80,w=(SW-36)/2;
    body+=label("title",L+12,T+8,SW-96,40,kind==="slots"?"유물 슬롯":"종 파편 교환",22,"TL",1)+
      button("back",R-60,T+8,48,48,"뒤로",false,false,"TR","lobby")+
      label("wallet",L+12,T+50,SW-24,28,`골드 ${gold} · 종 파편 ${fragments}`,16);
    if(kind==="slots"){
      const full=slotCount>=3,price=m.slotCosts[slotCount-1]||0,need=m.slotClearRequirements[slotCount-1]||0,locked=!full&&highestClear<need;
      body+=panel("slots-card",L+12,y,w,SH-160,text(L+28,y+34,`장착 슬롯 ${slotCount} / 3`,20)+
        [0,1,2].map(i=>rect(L+28+i*60,y+64,48,48,i<slotCount?p.ink:p.paper)).join(""))+
        panel("slot-cost",L+24+w,y,w,SH-160,text(L+40+w,y+34,full?"모두 열림":`종 파편 ${price}`,20)+text(L+40+w,y+72,full?"계정 공용":`${need}층 클리어 ${locked?"필요":"완료"}`,16))+
        button("slot-buy",R-232,B-64,220,48,full?"모두 열림":locked?`${need}층 클리어 필요`:"슬롯 늘리기",true,full||locked,"BR","slot-confirm");
      if(state==="confirm")body+=modal("슬롯을 늘릴까요?",`종 파편 ${price}개 사용\n슬롯 ${slotCount} → ${slotCount+1}`,"늘리기","취소","slot-buy");
    }else{
      body+=panel("exchange-offer",L+12,y,w,SH-160,text(L+28,y+34,`골드 ${m.exchangeGold}`,22)+text(L+28,y+72,`종 파편 ${m.exchangeFragments}`,18))+
        panel("exchange-balance",L+24+w,y,w,SH-160,text(L+40+w,y+34,`종 파편 ${fragments}`,18)+text(L+40+w,y+72,slotCount>=3?"슬롯 모두 열림":`다음 슬롯 ${m.slotCosts[slotCount-1]}개`,16))+
        button("exchange-buy",R-232,B-64,220,48,`종 파편 ${m.exchangeFragments}로 교환`,true,false,"BR","exchange-confirm");
      if(state==="confirm")body+=modal("교환할까요?",`종 파편 ${m.exchangeFragments} → 골드 ${m.exchangeGold}\n교환 후 ${Math.max(0,fragments-m.exchangeFragments)}개 · ${slotCount>=3?"슬롯 모두 열림":`다음 슬롯 ${m.slotCosts[slotCount-1]}개`}`,"교환","취소","exchange-buy");
    }
    if(state==="insufficient")body+=modal("종 파편이 부족해요","보유량과 비용을 확인해 주세요.","확인","닫기");
  }
  if(guides)body+=`<g pointer-events="none">`+rect(L,T,SW,SH,"none",p.line,'stroke-dasharray="6 4"')+
    items.filter(i=>i.hit).map(i=>rect(i.x+1,i.y+1,i.w-2,i.h-2,"none",p.line,'stroke-dasharray="3 3"')).join("")+
    regions.map(r=>rect(r.x,r.y,r.w,r.h,p.mid,"none",'fill-opacity=".18"')).join("")+"</g>";
  body=body.replace(/data-id="([^"]+)"([^>]*?)data-hit="(?:true|false)"/g,(_,id,a)=>`data-id="${id}"${a}data-hit="${items.find(i=>i.id===id).hit}"`);
  return {svg:`<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" role="img" aria-label="${kind} ${state}" font-family="'Malgun Gothic','Apple SD Gothic Neo',sans-serif">${body}</svg>`,items,regions,floorY:F};
}

const names={lobby:"캐릭터 로비",battle:"전투",tutorial:"첫 튜토리얼",progress:"체크포인트·엔딩",slots:"유물 슬롯",exchange:"재화 교환"};
const states={
  lobby:[["normal","기본 / 강화0"],["death9","9 사망 → 1"],["death19","19 사망 → 11"],["next11","10 완료 → 11"],["ngplus","다음 회차"],["insufficient","강화 재화 부족"],["settings","흔들림 설정"]],
  battle:[["normal","발밑 위험"],["upper","위 위험"],["rear","뒤 위험"],["front","앞 위험"],["boss5","중간 보스"],["boss10","10 보스"],["stunned","종 막대·별·기절"],["resist","기절 후 회복"],["hit","일반 피해·파편"],["critical","치명타·월드 흔들림"],["player-hit","플레이어 피격"],["paused","일시정지"]],
  tutorial:[["normal","1: 멈춤 → ▲"],["attack","2: 멈춤 → 공격"]],
  progress:[["normal","10 → 11"],["ending","50 → NG+ 1층"]],
  slots:[["normal","기본"],["confirm","슬롯 확인"],["insufficient","부족"]],
  exchange:[["normal","기본"],["confirm","교환 확인"],["insufficient","부족"]],
};
function check(){
  let views=0,targets=0;const failures=[],overlap=(a,b)=>a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y;
  for(const [width,height] of [[960,540],[1200,540],[640,360]])for(const safe of [
    {left:0,right:0,top:0,bottom:0},{left:24,right:24,top:12,bottom:12},{left:44,right:32,top:16,bottom:16}])
    for(const large of [false,true])for(const [kind,ss] of Object.entries(states))for(const [state] of ss){
      const r=renderV3(kind,{width,height,...safe,state,large},model),hit=r.items.filter(i=>i.hit);
      for(const i of r.items)if(!i.world&&(i.x<safe.left||i.y<safe.top||i.x+i.w>width-safe.right+.01||i.y+i.h>height-safe.bottom+.01))
        failures.push(`${width} ${kind}/${state}: outside safe ${i.id}`);
      for(const [n,a] of hit.entries()){targets++;if(a.w<48||a.h<48)failures.push(`${width} ${kind}/${state}: small ${a.id}`);
        for(const b of hit.slice(n+1))if(overlap(a,b))failures.push(`${width} ${kind}/${state}: overlap ${a.id}/${b.id}`);}
      for(const i of r.items.filter(i=>i.critical))for(const z of r.regions)if(overlap(i,z))failures.push(`${width} ${kind}/${state}: ${i.id} in ${z.id}`);
      views++;
    }
  return {views,targets,failures};
}
const dir=dirname(fileURLToPath(import.meta.url));
function sheet(){
  const kinds=Object.keys(names);return `<svg xmlns="http://www.w3.org/2000/svg" width="2048" height="2070" viewBox="0 0 2048 2070" font-family="'Malgun Gothic',sans-serif"><rect width="2048" height="2070" fill="#eee"/>${kinds.map((kind,i)=>{
    const x=32+i%2*1000,y=56+Math.floor(i/2)*650,r=renderV3(kind,{guides:true,state:kind==="battle"?"boss5":"normal"},model);
    const lines=r.items.filter(t=>t.hit||["bell-meter","boss-hp","player-hp"].includes(t.id)).map(t=>`${t.id} ${t.x},${t.y},${t.w},${t.h} ${t.anchor}`).join(" · ");
    return `<g transform="translate(${x},${y})"><text y="-18" font-size="22">${names[kind]}</text>${r.svg}<text y="566" font-size="12">${lines.slice(0,145)}</text><text y="588" font-size="12">${lines.slice(145,290)}</text></g>`;
  }).join("")}<text x="32" y="2020" font-size="18">V3 · 960×540 · L24/R24/T12/B12 · bounds=x,y,w,h · 상세/최소/120%와 상호작용은 HTML에서 확인</text><text x="32" y="2046" font-size="16">픽셀형 SVG 배치 설명이며 production 아트/실제 게임 아님. 통합 계약 반영 · 실제 기기 검증 전.</text></svg>`;
}
const html=`<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>마지막 종지기 · UI v3</title>
<style>*{box-sizing:border-box}body{margin:0;background:#eee;color:#222;font:14px "Malgun Gothic",sans-serif}header,footer{padding:20px;background:#fff}h1{font-size:24px;margin:0 0 8px}p{line-height:1.55;margin:6px 0}.controls{display:flex;flex-wrap:wrap;gap:12px;margin-top:14px}label{display:flex;align-items:center;gap:6px}select,button{font:inherit;background:#fff;color:#222;padding:8px;border:1px solid #888;min-height:40px}main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px;padding:20px}.card{min-width:0}h2{font-size:18px;margin:0 0 10px}.screen svg{display:block;width:100%;height:auto}.screen [data-hit=true][data-action]{cursor:pointer;touch-action:none}.screen [data-hit=true][data-action]:focus{outline:2px solid #c78b2d}details{background:white;padding:10px;margin-top:12px}table{border-collapse:collapse;width:100%;font-size:11px}td,th{padding:5px;border-bottom:1px solid #ddd;text-align:left}.note{line-height:1.6}input[type=checkbox]{width:18px;height:18px}@keyframes shake{0%,100%{transform:translate(0,0)}25%{transform:translate(-2px,1px)}50%{transform:translate(2px,-1px)}75%{transform:translate(-1px,0)}}.world-shake{animation:shake 80ms linear 1}@keyframes idle{0%,100%{transform:translateY(0)}50%{transform:translateY(-2px)}}.idle-preview{animation:idle 1200ms steps(2,end) infinite}@keyframes dodge{0%,100%{transform:translate(0,0)}45%,60%{transform:translate(var(--dodge-x),var(--dodge-y))}}.dodge-motion{animation:dodge 260ms ease-out both}@keyframes rise{from{transform:translateY(0);opacity:1}to{transform:translateY(-16px);opacity:0}}@keyframes fall{from{transform:translateY(-6px);opacity:1}to{transform:translateY(18px);opacity:0}}.damage-rise{animation:rise 450ms ease-out forwards}.fragment-fall{animation:fall 300ms ease-in forwards}@media(prefers-reduced-motion:reduce){.world-shake,.damage-rise,.fragment-fall,.idle-preview{animation:none}}@media(max-width:1100px){main{grid-template-columns:1fr}}a{color:#254e72}</style></head><body>
<header><h1>UI v3 · 종지기와 위험을 보는 화면</h1><p>왼쪽은 클릭 가능한 상태 시안, 오른쪽은 비교 화면입니다. 실제 전투 판정·경제·저장 기능이 아닙니다. 통합 계약의 비용과 능력치를 표시합니다. 움직이는 숫자·파편은 연출 표본이며 브라우저 축소 크기는 실제 dp가 아닙니다.</p>
<div class="controls"><label>크기<select id="size"><option value="960,540">960×540</option><option value="1200,540">1200×540</option><option value="640,360">640×360</option></select></label>
<label>안전영역<select id="safe"><option value="24,24,12,12">L24 R24 T12 B12</option><option value="44,32,16,16">L44 R32 T16 B16</option><option value="0,0,0,0">없음</option></select></label>
<label><input id="large" type="checkbox">한글120%</label><label><input id="guides" type="checkbox">bounds·손가림</label><label><input id="color" type="checkbox" checked>컬러 배치</label><label><input id="reduced" type="checkbox">흔들림 줄이기</label></div>
<div class="controls"><button data-scenario="tutorial">튜토리얼2행동</button><button data-scenario="lobby">로비 검토</button><button data-scenario="battle">전투 입력</button><button data-scenario="death9">9사망→1</button><button data-scenario="finish10">10완료→11</button><button data-scenario="ending">50완료→NG+</button><button data-scenario="level9">강화9→10→11</button><button data-scenario="hit">피해·파편</button><button data-scenario="critical">치명타</button><button data-scenario="funds">검토용 재화 채우기</button><button data-scenario="reset">초기화</button></div>
<p id="status" role="status">검토용 인터랙션 준비. 전투 입력은 방향키/WASD, 공격은 Space로도 확인할 수 있습니다.</p></header>
<main><section class="card"><h2>동작 시안</h2><div class="controls"><label>화면<select id="live-kind"></select></label><label>상태<select id="live-state"></select></label></div><div class="screen" id="live"></div><p class="note">튜토리얼은 ▲ → 검 순서입니다. 다른 버튼은 첫 행동을 통과시키지 않습니다. 이후 전투 흐름이 자동 재개되며 플레이어 자동공격은 하지 않습니다.</p><details id="live-details"><summary>bounds · anchor · hit area</summary><div id="inspector"></div></details></section>
<section class="card"><h2>비교 화면</h2><div class="controls"><label>화면<select id="compare-kind"></select></label><label>상태<select id="compare-state"></select></label></div><div class="screen" id="compare"></div><p class="note">위험은 공격 위치의 모양이며 정답 화살표가 아닙니다. 기절 글자·숫자와 상단 공격 안내는 게임 화면에서 제거했습니다. 피해 숫자는 픽셀 도형입니다.</p></section></main>
<footer><p>캐릭터 선택 → 네 능력치/강화 종 → 출전.9사망은1층,10완료는11층,50완료 뒤 해당 캐릭터NG+와 다음1층.</p><p><a href="../Reviews/ui-v3.md">UI 검토</a> · <a href="wireframes-v3.svg">전체 시트</a> · <a href="landscape-battle-v3.svg">전투 SVG</a> · <a href="lobby-v3.svg">로비 SVG</a></p></footer>
<script>
const model=${JSON.stringify(model)},names=${JSON.stringify(names)},states=${JSON.stringify(states)};
const upgradeCost=${upgradeCost.toString()},statValue=${statValue.toString()},renderV3=${renderV3.toString()};
const initial=()=>({kind:"tutorial",state:"normal",character:"on",gold:0,fragments:0,slotCount:1,highestClear:0,requiredGold:10,motion:"",damage:"",records:Object.fromEntries(model.characters.map(c=>[c.id,{stage:1,checkpoint:1,ng:0,levels:[0,0,0,0]}]))});
let app=initial(),comparison={kind:"battle",state:"boss5"},timer=null,anim=null,auto=false;
const rec=()=>app.records[app.character];
const status=s=>{document.getElementById("status").textContent=s;};
function options(){const [width,height]=document.getElementById("size").value.split(",").map(Number),[left,right,top,bottom]=document.getElementById("safe").value.split(",").map(Number);return {width,height,left,right,top,bottom,color:document.getElementById("color").checked,large:document.getElementById("large").checked,guides:document.getElementById("guides").checked,reduced:document.getElementById("reduced").checked};}
function setSelect(id,values,value){document.getElementById(id).innerHTML=values.map(([k,v])=>'<option value="'+k+'"'+(k===value?' selected':'')+'>'+v+'</option>').join("");}
function paint(){
 setSelect("live-kind",Object.entries(names),app.kind);setSelect("live-state",states[app.kind],app.state);
 setSelect("compare-kind",Object.entries(names),comparison.kind);setSelect("compare-state",states[comparison.kind],comparison.state);
 const o=options(),r=renderV3(app.kind,{...o,...app,...rec(),levels:rec().levels},model);
 document.getElementById("live").innerHTML=r.svg;
 const cp=comparison.state==="death9"?1:comparison.state==="death19"||comparison.state==="next11"?11:1;
 document.getElementById("compare").innerHTML=renderV3(comparison.kind,{...o,state:comparison.state,checkpoint:cp,ng:comparison.state==="ngplus"?1:0},model).svg;
 document.getElementById("inspector").innerHTML='<table><thead><tr><th>ID</th><th>x,y,w,h</th><th>anchor</th><th>입력</th></tr></thead><tbody>'+r.items.map(i=>'<tr><td>'+i.id+'</td><td>'+[i.x,i.y,i.w,i.h].join(",")+'</td><td>'+i.anchor+'</td><td>'+(i.hit?'활성':'—')+'</td></tr>').join("")+'</tbody></table>';
}
function stop(){clearInterval(timer);clearTimeout(anim);timer=null;auto=false;}
function startFlow(){stop();auto=true;let n=0;timer=setInterval(()=>{if(app.kind!=="battle"||app.state==="paused")return;app.state=["normal","upper","front","rear"][n++%4];app.motion="";app.damage="";paint();},1400);}
function action(a){
 if(a.startsWith("character-")){stop();app.character=a.slice(10);app.state="normal";status("선택한 종지기의 강화와 체크포인트를 표시합니다.");}
 else if(a.startsWith("upgrade-")){const i=Number(a.slice(8)),cost=upgradeCost(rec().levels[i]+1,model);app.requiredGold=cost;if(app.gold<cost){app.state="insufficient";}else if(rec().levels[i]<model.maxLevel){app.gold-=cost;rec().levels[i]++;status(model.statNames[i]+" Lv."+rec().levels[i]+" · "+cost+"G 차감 · 이 브라우저 안의 시안 값");}}
 else if(a.startsWith("dodge-")){
  const direction=a.slice(6);if(app.kind==="tutorial"&&app.state==="normal"){if(direction!=="up")return;app.motion="up";paint();status("회피 동작 표본. 이어서 공격 행동을 안내합니다.");anim=setTimeout(()=>{app.motion="";app.state="attack";paint();},400);return;}
  if(app.kind!=="battle"||app.state==="paused")return;app.motion=direction;status(direction+" 회피 동작 표본 · 성공/실패 판정 없음");clearTimeout(anim);anim=setTimeout(()=>{app.motion="";paint();},260);
 }
 else if(a==="attack"){
  if(app.kind==="tutorial"&&app.state==="attack"){app.damage="24";paint();status("두 행동 완료. 전투 흐름을 자동으로 재개합니다.");anim=setTimeout(()=>{app.kind="battle";app.state="normal";app.damage="";paint();startFlow();},450);return;}
  if(app.kind!=="battle"||app.state==="paused")return;app.damage="24";status("피해 숫자와 대상 사각 파편 표본");clearTimeout(anim);anim=setTimeout(()=>{app.damage="";paint();},450);
 }
 else if(a==="start"){stop();app.kind="battle";app.state="normal";rec().stage=rec().checkpoint;startFlow();}
 else if(a==="pause"){stop();app.state="paused";}
 else if(a==="resume"){app.state="normal";startFlow();}
 else if(a==="lobby"){stop();app.kind="lobby";app.state="normal";}
 else if(a==="close"){if(app.kind==="battle"){stop();app.kind="lobby";}app.state="normal";}
 else if(a==="slots"||a==="exchange"){stop();app.kind=a;app.state="normal";}
 else if(a==="slot-confirm"||a==="exchange-confirm"){app.state="confirm";}
 else if(a==="slot-buy"){const cost=model.slotCosts[app.slotCount-1],need=model.slotClearRequirements[app.slotCount-1];if(app.slotCount>=3||app.highestClear<need)return;if(app.fragments<cost)app.state="insufficient";else{app.fragments-=cost;app.slotCount++;app.state="normal";status("계정 공용 슬롯 "+app.slotCount+"개 · 실제 저장 없음");}}
 else if(a==="exchange-buy"){if(app.fragments<model.exchangeFragments)app.state="insufficient";else{app.fragments-=model.exchangeFragments;app.gold+=model.exchangeGold;app.state="normal";status("교환 잔액 시안 · 실제 거래 없음");}}
 else if(a==="finish10"||a==="return10"){if(app.kind!=="progress")return;stop();rec().checkpoint=11;rec().stage=11;app.highestClear=Math.max(app.highestClear,10);app.kind=a==="return10"?"lobby":"battle";app.state=a==="return10"?"next11":"normal";status(a==="return10"?"귀환 · 다음 출전은11층입니다.":"체크포인트 확정 → 11층 자동 진행 표본");if(a==="finish10")startFlow();}
 else if(a==="finish50"){if(app.kind!=="progress")return;stop();rec().ng++;rec().checkpoint=1;rec().stage=1;app.highestClear=50;app.kind="lobby";app.state="ngplus";status("기록 확정 표본 → 다음 회차1층. 다른 종지기의 진행은 유지됩니다.");}
 else if(a==="toggle-reduced"){document.getElementById("reduced").checked=!document.getElementById("reduced").checked;}
 paint();
}
document.getElementById("live").addEventListener("pointerdown",e=>{const t=e.target.closest("[data-action]");if(!t||t.dataset.hit!=="true")return;e.preventDefault();action(t.dataset.action);});
document.addEventListener("keydown",e=>{if(["SELECT","INPUT","BUTTON"].includes(document.activeElement.tagName))return;const a={ArrowUp:"dodge-up",w:"dodge-up",ArrowLeft:"dodge-back",a:"dodge-back",ArrowDown:"dodge-down",s:"dodge-down",ArrowRight:"dodge-front",d:"dodge-front"," ":"attack"}[e.key];if(a&&!e.repeat){e.preventDefault();action(a);}});
document.addEventListener("change",e=>{
 const id=e.target.id;if(id==="live-kind"){stop();app.kind=e.target.value;app.state="normal";}
 if(id==="live-state"){stop();app.state=e.target.value;if(app.kind==="lobby"){if(app.state==="death9")rec().checkpoint=1;if(app.state==="death19"||app.state==="next11")rec().checkpoint=11;}}
 if(id==="compare-kind"){comparison.kind=e.target.value;comparison.state="normal";}if(id==="compare-state")comparison.state=e.target.value;paint();
});
document.querySelectorAll("[data-scenario]").forEach(b=>b.onclick=()=>{
 stop();app.motion="";app.damage="";const s=b.dataset.scenario;
 if(s==="reset"){app=initial();status("검토 상태 초기화");}
 if(s==="tutorial"){app.kind="tutorial";app.state="normal";rec().stage=1;status("적 타격 직전 멈춤. ▲를 눌러 첫 행동을 확인하세요.");}
 if(s==="battle"){app.kind="battle";app.state="normal";startFlow();}
 if(s==="death9"){rec().stage=9;rec().checkpoint=1;app.kind="lobby";app.state="death9";status("9층 사망 → 로비 → 1층 출전 표본");}
 if(s==="lobby"){app.kind="lobby";app.state="normal";}
 if(s==="finish10"){app.kind="progress";app.state="normal";status("10보스 완료 → 잠시 뒤11층 자동 진행. 귀환으로 로비를 선택할 수 있습니다.");anim=setTimeout(()=>action("finish10"),1200);}
 if(s==="ending"){app.kind="progress";app.state="ending";anim=setTimeout(()=>action("finish50"),6000);}
 if(s==="level9"){app.kind="lobby";app.state="normal";rec().levels[0]=9;app.gold=200;status("공격 +를 두 번 눌러10개 채움과11의다음단계1개를 비교하세요.");}
 if(s==="hit"||s==="critical"){app.kind="battle";app.state=s;status(s==="critical"?"월드만 흔들리는 치명타 표본. 줄이기 옵션을 비교하세요.":"피해 숫자·사각 파편 표본");}
 if(s==="funds"){app.gold+=200;app.fragments+=50;status("문서 검토용 잔액을 추가했습니다. 게임 획득 규칙이 아닙니다.");}
 paint();
});
paint();
</script></body></html>`;
const framed=(kind,title)=>{const r=renderV3(kind,{color:true,state:kind==="battle"?"boss5":"normal"},model);return `<svg xmlns="http://www.w3.org/2000/svg" width="1008" height="636" viewBox="0 0 1008 636" font-family="'Malgun Gothic',sans-serif"><rect width="1008" height="636" fill="#eee"/><text x="24" y="28" font-size="18">${title} · production 아트가 아닌 배치 시안</text><g transform="translate(24,48)">${r.svg}</g><text x="24" y="616" font-size="14">960×540 · 발선${r.floorY} · 문구/좌표는 화면 밖 명세 참조 · 실제 기기 검증 전</text></svg>`;};
if(process.argv.includes("--check")){const r=check();console.log(JSON.stringify(r,null,2));if(r.failures.length)process.exitCode=1;}
else{writeFileSync(join(dir,"wireframes-v3.html"),html,"utf8");writeFileSync(join(dir,"wireframes-v3.svg"),sheet(),"utf8");writeFileSync(join(dir,"landscape-battle-v3.svg"),framed("battle","V3 · 전투"),"utf8");writeFileSync(join(dir,"lobby-v3.svg"),framed("lobby","V3 · 캐릭터 로비"),"utf8");console.log("Wrote V3 HTML and three SVG artifacts.");}
export {renderV3,model,states,check,upgradeCost,statValue};
