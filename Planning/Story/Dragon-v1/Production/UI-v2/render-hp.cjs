// Code-native UI study. No Unity, scene, prefab or gameplay state access.
const fs=require('fs'),path=require('path');
const {createCanvas,GlobalFonts}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
GlobalFonts.registerFromPath(path.resolve(__dirname,'../../../../../Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf'),'Neo');
const C={paper:'#FFF1D1',ink:'#273D45',empty:'#596463',cap:'#89918A',green:'#24766D',greenLight:'#72B99B',red:'#B84232',redLight:'#F18C67'};
const cases=[['0%',0],['tiny > 0',.000001],['25%',.25],['50%',.5],['100%',1]];
const types=[['PLAYER',160,false],['ENEMY',184,true],['BOSS',280,true]];
const count=(value,n)=>value<=0?0:Math.min(n,Math.max(1,Math.ceil(Math.min(1,value)*n)));
function bar(width,value,enemy,version){
 const ops=[],rect=(x,y,w,h,c)=>{if(w>0&&h>0)ops.push([x,y,w,h,c]);};
 const fill=enemy?C.red:C.green,light=enemy?C.redLight:C.greenLight;
 if(version===1){
  rect(4,0,width-8,24,C.ink);rect(0,4,width,16,C.ink);rect(4,4,width-8,16,C.empty);
  const f=count(value,(width-8)/4)*4;
  if(f){rect(4,4,f,16,fill);rect(4,4,f,4,light);rect(f,8,4,8,light);}
  return ops;
 }
 const columns=width/4,inner=columns-8,capacity=inner-Math.floor(inner/5),filled=count(value,capacity);
 // Nine native rows, two-step shoulders, fixed caps outside the HP channel.
 for(let y=0;y<9;y++){const inset=y===0||y===8?3:y===1||y===7?1:0;rect(inset*4,y*4,width-inset*8,4,C.ink);}
 rect(16,4,width-32,4,C.cap);
 for(const side of [0,1]){
  const x=side?columns-2:1;rect(x*4,12,4,12,C.cap);
  rect((side?columns-3:2)*4,16,4,4,C.empty);
 }
 let index=0;
 for(let x=0;x<inner;x++){
  if(x%5===4)continue; // 4 native columns per block, then one ink separator.
  const active=index++<filled;rect((x+4)*4,8,4,20,active?fill:C.empty);
  if(active)rect((x+4)*4,8,4,4,light);
 }
 return ops;
}
function sheet(scale){
 const width=1040,height=900,canvas=createCanvas(width,height),g=canvas.getContext('2d'),svg=[];
 g.imageSmoothingEnabled=false;
 const rect=(x,y,w,h,c)=>{g.fillStyle=c;g.fillRect(x,y,w,h);svg.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${c}"/>`);};
 const text=(s,x,y,size=18)=>{g.font=`${size}px Neo`;g.fillStyle=C.ink;g.fillText(s,x,y);svg.push(`<text x="${x}" y="${y}" fill="${C.ink}" font-family="monospace" font-size="${size}">${s.replaceAll('>','&gt;')}</text>`);};
 rect(0,0,width,height,C.paper);text(`HP PIXEL STUDY / ${scale===1?'1280 x 720':'640 x 360'} actual bar scale`,32,40,24);
 text('OLD: 6 native rows / NEW: 9 rows, 4px cells at 720p, 4+1 block rhythm',32,72,18);
 const xpos=[200,440,720];types.forEach((t,i)=>text(t[0],xpos[i],112));
 cases.forEach(([label,value],row)=>{
  const top=140+row*148;text(label,32,top+45);text('OLD',110,top+22,14);text('NEW',110,top+77,14);
  types.forEach(([name,w,enemy],i)=>{
   for(const [v,y] of [[1,top],[2,top+52]])for(const [x,yy,ww,hh,c] of bar(w,value,enemy,v))rect(xpos[i]+x*scale,y+yy*scale,ww*scale,hh*scale,c);
  });
 });
 const suffix=scale===1?'1280':'640';fs.writeFileSync(path.join(__dirname,`compare-${suffix}.png`),canvas.toBuffer('image/png'));
 fs.writeFileSync(path.join(__dirname,`compare-${suffix}.svg`),`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" shape-rendering="crispEdges">${svg.join('')}</svg>`);
}
sheet(1);sheet(.5);
const spec={status:'READY: visually reviewed; runtime code prepared; Unity integration pending',nativeHeight:9,cellAt720:4,cellAt360:2,
 sizes720:{player:[160,36],enemy:[184,36],boss:[280,36]},sizes360:{player:[80,18],enemy:[92,18],boss:[140,18]},
 unchanged:'All centers/home projection, width, HP data, input and gameplay rules. Only Dragon HP mesh and bar height.',
 outline:'1-native-pixel ink perimeter, 3/1/0 inset shoulders across 9 rows. Two fixed gray caps outside health channel.',
 fill:'4 native columns per block + 1 ink separator; 5 rows of fill with 1-row highlight. Five colors per active bar, binary alpha.',
 visibility:'ceil(value * drawableColumns); exact zero empty, positive values at least one full-height column, full HP all drawable columns. Separator columns do not count as HP.',
 palette:{ink:C.ink,empty:C.empty,cap:C.cap,player:[C.green,C.greenLight],enemy:[C.red,C.redLight]},
 cases:cases.map(([label,value])=>({label,value,bars:types.map(([name,width])=>{let n=width/4-8,capacity=n-Math.floor(n/5);return{name,capacity,filled:count(value,capacity)};})})),
 previews:'PNG and SVG comparisons contain labels for inspection only; no numbers or extra symbols are added to game HP bars.'};
fs.writeFileSync(path.join(__dirname,'spec.json'),JSON.stringify(spec,null,2)+'\n');
fs.writeFileSync(path.join(__dirname,'index.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><title>Dragon HP UI v2</title><style>body{margin:32px;background:${C.paper};color:${C.ink};font:18px/1.65 system-ui}main{max-width:1080px;margin:auto}div.comparison{overflow-x:auto}img{width:1040px;max-width:none;display:block;image-rendering:pixelated;border:1px solid ${C.cap}}code{background:#fff9e9;padding:3px}a{color:${C.green}}</style><main><h1>HP 도트 강화 · UI v2</h1><p>체력바 중심과 너비는 유지하고 높이를 24 → 36px로 조정합니다. 9칸 높이의 계단형 외곽선, 회색 끝부분, 4칸 채움 + 1칸 구분으로 도트 덩어리를 분명하게 만듭니다. 인게임 숫자나 별도 장식은 추가하지 않습니다.</p><p>1280×720: 플레이어 <code>160×36</code>, 적 <code>184×36</code>, 보스 <code>280×36</code>, 1칸 <code>4px</code>. 640×360: 각각 <code>80×18 / 92×18 / 140×18</code>, 1칸 <code>2px</code>. 막대당 5색, 그라데이션 없음.</p><p>실제 HP 값은 그대로 표시합니다. 0은 빈 칸, 0보다 크면 최소 한 열, 100%는 모든 채움 칸을 표시합니다. 회색 끝부분과 검은 구분선은 체력에 포함하지 않습니다.</p><h2>1280 기준 실제 막대 크기</h2><div class="comparison"><img src="compare-1280.png" alt="기존과 개선안: 0, 극소량, 25, 50, 100 퍼센트 HP"></div><h2>640 기준 실제 막대 크기</h2><div class="comparison"><img src="compare-640.png" alt="640 화면에서 동일 상태 비교"></div><p>시안은 코드로 그린 비교 자료입니다. Unity 실화면 검증은 통합 단계에서 진행합니다. <a href="spec.json">치수·색상·표시 규칙</a></p></main>`);
console.log('Created HP comparison HTML, SVG, PNG and size/rule specification.');
