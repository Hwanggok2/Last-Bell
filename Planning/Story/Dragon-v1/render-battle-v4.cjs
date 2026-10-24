// Pixel UI studies over the unchanged v3 world art. Planning only.
const fs = require('fs'), path = require('path');
const {createCanvas, loadImage, GlobalFonts} = require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const BASE = __dirname, OUT = path.join(BASE, 'images'), ASSETS = path.join(OUT, 'ui-pixel-v4');
GlobalFonts.registerFromPath(path.resolve(BASE, '../../../Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf'), 'Pixel');
const C = {paper:'#FFF1D1', light:'#FFF9E9', ink:'#273D45', edge:'#71543F', gold:'#B77A35', goldLight:'#F4CD6C', wood:'#9E6942', success:'#24766D', successLight:'#72B99B', danger:'#B84232', dangerLight:'#F18C67', empty:'#596463', steel:'#A4CED1', muted:'#89918A', purple:'#75628C'};
const rect = (g,x,y,w,h,c) => {g.fillStyle=c;g.fillRect(x,y,w,h)};
const canvas = (w,h) => {const c=createCanvas(w,h),g=c.getContext('2d');g.imageSmoothingEnabled=false;return [c,g]};
function save(c,file){fs.writeFileSync(path.join(OUT,file),c.toBuffer('image/png'))}

// Native 16px glyphs, binary alpha, then integer enlargement: no soft text edges.
function text(g,t,x,y,scale=2,color=C.ink,align='left'){
  const [measure,m]=canvas(1,1);m.font='16px Pixel';
  const width=Math.ceil(m.measureText(t).width),[label,l]=canvas(width+2,24);
  l.font='16px Pixel';l.textBaseline='alphabetic';l.fillStyle=color;l.fillText(t,0,16);
  const data=l.getImageData(0,0,label.width,label.height);
  const rgb=parseInt(color.slice(1),16);
  for(let i=3;i<data.data.length;i+=4){data.data[i]=data.data[i]>=128?255:0;data.data[i-3]=rgb>>16;data.data[i-2]=(rgb>>8)&255;data.data[i-1]=rgb&255}
  l.putImageData(data,0,0);
  if(align==='center')x-=Math.floor(width*scale/2);else if(align==='right')x-=width*scale;
  g.drawImage(label,Math.round(x),Math.round(y),label.width*scale,label.height*scale);
}
function stepped(g,x,y,w,h,color,cut=8){rect(g,x+cut,y,w-cut*2,h,color);rect(g,x,y+cut,w,h-cut*2,color)}
function plate(g,x,y,w,h,state='normal'){
  const shift=state==='pressed'?4:0,active=state==='selected';
  stepped(g,x,y+4,w,h,C.edge);
  stepped(g,x,y+shift,w,h,C.ink);
  stepped(g,x+4,y+4+shift,w-8,h-8,active?C.gold:C.goldLight,4);
  stepped(g,x+8,y+8+shift,w-16,h-16,state==='disabled'?'#DDD8C3':C.paper,4);
  rect(g,x+12,y+8+shift,w-24,4,C.light);
}

function outline(grid){
  const h=grid.length,w=grid[0].length,out=Array.from({length:h+2},()=>Array(w+2).fill(null));
  for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(grid[y][x]){
    for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)out[y+dy+1][x+dx+1]=C.ink;
  }
  for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(grid[y][x])out[y+1][x+1]=grid[y][x];
  return out;
}
function matrix(rows,colors){return rows.map(row=>[...row].map(p=>colors[p]||null))}
const ARROW=['.......x.......','......xxx......','.....xxxxx.....','....xxxxxxx....','...xxxxxxxxx...','..xxxxxxxxxxx..','.xxxxxxxxxxxxx.','xxxxxxxxxxxxxxx','xxxxx.....xxxxx','xxx.........xxx','x.............x'];
function arrowMap(dir){
  let a=ARROW.map((r,y)=>[...r].map(p=>p==='x'?(y<5?C.successLight:C.success):null));
  if(dir==='left')a=Array.from({length:a[0].length},(_,y)=>a.map(row=>row[a[0].length-1-y]));
  if(dir==='right')a=Array.from({length:a[0].length},(_,y)=>a.map(row=>row[y]).reverse());
  if(dir==='down')a=a.slice().reverse().map(row=>row.slice().reverse());
  return outline(a);
}
function swordMap(){
  const a=Array.from({length:24},()=>Array(24).fill(null));
  for(let y=1;y<16;y++)for(let x=9;x<23;x++)if(x+y>=22&&x+y<=25&&x-y>=-4&&x-y<=20)a[y][x]=x+y<24?C.light:C.steel;
  for(let i=0;i<7;i++)for(let d=-1;d<=1;d++)a[15+i][8-i+d]=C.wood;
  for(let i=0;i<9;i++)for(let d=-1;d<=1;d++)a[10+i][5+i+d]=i<4?C.goldLight:C.gold;
  a[21][1]=a[21][2]=a[22][1]=a[22][2]=C.goldLight;
  return outline(a);
}
const maps={
  up:arrowMap('up'),left:arrowMap('left'),right:arrowMap('right'),down:arrowMap('down'),sword:swordMap(),
  shield:outline(matrix(['xxxxxxx','xllsssx','xllsssx','xllsssx','xllsssx','.xsssx.','..xsx..','...x...'],{x:C.edge,l:C.light,s:C.steel})),
  pause:outline(matrix(['xx..xx','xx..xx','xx..xx','xx..xx','xx..xx','xx..xx','xx..xx','xx..xx'],{x:C.paper})),
  coin:outline(matrix(['.xxxx.','xhhxxx','xhgxxx','xhgxxx','xhgxxx','.xxxx.'],{x:C.gold,h:C.goldLight,g:C.light})),
  gem:outline(matrix(['..xxx..','.xhllx.','xhhlllx','.xhlxx.','..xxx..','...x...'],{x:C.success,h:C.successLight,l:C.light})),
  heart:outline(matrix(['.xx.xx.','xxxxxxx','xxxxxxx','.xxxxx.','..xxx..','...x...'],{x:C.dangerLight})),
  star:outline(matrix(['...x...','...x...','..xxx..','xxxxxxx','.xxxxx.','..xxx..','.xx.xx.','x.....x'],{x:C.goldLight})),
  plus:outline(matrix(['..xx..','..xx..','xxxxxx','xxxxxx','..xx..','..xx..'],{x:C.paper})),
  bag:outline(matrix(['..xxxx..','.x....x.','xxxxxxxx','xxhxxxxx','xxhxxxxx','xxhxxxxx','xxxggxxx','xxxggxxx','xxxxxxxx'],{x:C.wood,h:C.goldLight,g:C.gold})),
  check:outline(matrix(['......xx','.....xxx','xx..xxx.','xxxxxx..','.xxxx...','..xx....'],{x:C.successLight})),
  cross:outline(matrix(['xx....xx','xxx..xxx','.xxxxxx.','..xxxx..','..xxxx..','.xxxxxx.','xxx..xxx','xx....xx'],{x:C.dangerLight}))
};
function icon(g,name,x,y,state='normal',size=4){
  const a=maps[name],w=a[0].length,h=a.length;
  const ox=Math.round(x-w*size/2),oy=Math.round(y-h*size/2)+(state==='pressed'?4:0);
  for(let yy=0;yy<h;yy++)for(let xx=0;xx<w;xx++)if(a[yy][xx]){
    let c=a[yy][xx];
    if(state==='disabled')c=c===C.ink?C.muted:C.paper;
    if(state==='pressed'&&c!==C.ink)c=c===C.light?C.goldLight:C.gold;
    rect(g,ox+xx*size,oy+yy*size,size,size,c);
  }
}
function bar(g,x,y,w,ratio,color,bright){
  // Same horizontal centers; 24px frame adds four pixels above/below the old bar.
  stepped(g,x,y,w,24,C.ink,4);
  rect(g,x+4,y+4,w-8,16,C.empty);
  const cells=(w-8)/4,filled=ratio<=0?0:Math.min(cells,Math.max(1,Math.ceil(cells*ratio)));
  rect(g,x+4,y+4,filled*4,16,color);
  if(filled){rect(g,x+4,y+4,filled*4,4,bright);rect(g,x+filled*4,y+8,4,8,bright)}
}
function ui(g,stage,name,boss,wire=false){
  rect(g,0,500,1280,220,C.paper);rect(g,0,500,1280,4,C.edge);rect(g,0,504,1280,4,C.goldLight);
  plate(g,16,16,244,104);text(g,`Stage ${stage}`,32,28);icon(g,'coin',46,92);text(g,stage===3?'120':'1,240',76,76);
  plate(g,1160,16,96,96);icon(g,'pause',1208,64);icon(g,'shield',640,90);
  bar(g,390,520,160,.8,C.success,C.successLight);bar(g,boss?710:718,520,boss?280:184,.7,C.danger,C.dangerLight);
  text(g,name,boss?850:810,556,2,C.ink,'center');
  icon(g,'up',144,507);icon(g,'left',71,613);icon(g,'down',190,641);icon(g,'sword',1192,636);
  if(wire){
    for(const [x,y,w,h] of [[92,455,104,104],[19,561,104,104],[138,589,104,104],[1120,564,144,144]]){
      for(let xx=x;xx<x+w;xx+=12){rect(g,xx,y,Math.min(8,x+w-xx),4,C.purple);rect(g,xx,y+h-4,Math.min(8,x+w-xx),4,C.purple)}
      for(let yy=y;yy<y+h;yy+=12){rect(g,x,yy,4,Math.min(8,y+h-yy),C.purple);rect(g,x+w-4,yy,4,Math.min(8,y+h-yy),C.purple)}
    }
  }
}
function telegraph(g){
  // Rasterize continuous timing arcs on a 4px grid; no change to timing semantics.
  for(let y=28;y<125;y++)for(let x=0;x<320;x++){
    const d=Math.min(Math.abs(Math.hypot(x+.5+40,y+.5-90)-160),Math.abs(Math.hypot(x+.5-360,y+.5-90)-160));
    if(d<=1.25)rect(g,x*4,y*4,4,4,d<=.5?C.successLight:C.ink);
  }
  for(const [x,y,sx,sy] of [[352,424,1,1],[800,424,-1,1],[352,460,1,-1],[800,460,-1,-1]]){
    rect(g,sx>0?x-4:x-32,y-4,36,12,C.ink);rect(g,x-4,sy>0?y-4:y-24,12,28,C.ink);
    rect(g,sx>0?x:x-28,y,28,4,C.dangerLight);rect(g,x,sy>0?y:y-20,4,20,C.dangerLight);
  }
}
function kit(){
  const [c,g]=canvas(1280,1120);rect(g,0,0,1280,1120,C.paper);
  text(g,'PIXEL UI / 4px',40,28,3);text(g,'조작 아이콘 · 기본 / 눌림 / 비활성',40,100);
  for(const [i,name] of ['up','left','down','sword'].entries()){
    const x=152+i*300;for(const [j,state] of ['normal','pressed','disabled'].entries())icon(g,name,x-72+j*72,210,state,name==='sword'?2:3);
    text(g,['점프','뒤로','쳐내기','공격'][i],x,272,2,C.ink,'center');
  }
  rect(g,40,328,1200,4,C.gold);
  text(g,'체력 · 피해 / 낮음 / 1HP / 0HP',40,356);
  bar(g,40,416,224,.75,C.success,C.successLight);bar(g,344,416,224,.15,C.danger,C.dangerLight);bar(g,648,416,224,.001,C.success,C.successLight);bar(g,952,416,224,0,C.success,C.successLight);
  text(g,'보스',40,472);bar(g,152,476,480,.7,C.danger,C.dangerLight);text(g,'128',836,464,2,C.danger);text(g,'256!',1024,452,3,C.gold);icon(g,'star',1180,478,'normal',3);
  rect(g,40,548,1200,4,C.gold);text(g,'공통 아이콘 · 메뉴 / 재화 / 성장 / 유물',40,580);
  for(const [i,name] of ['pause','shield','coin','gem','heart','star','plus','bag'].entries()){
    const x=110+i*152;icon(g,name,x,678);text(g,['일시정지','방어','골드','보석','체력','별점','강화','유물'][i],x,724,1,C.ink,'center');
  }
  rect(g,40,780,1200,4,C.gold);text(g,'선택 / 확인 / 취소 / 비활성',40,812);
  for(const [i,state] of ['selected','normal','normal','disabled'].entries()){
    const x=40+i*304;plate(g,x,872,272,104,state);if(i===0)icon(g,'check',x+32,900,'normal',2);
    text(g,['공격력','확인','취소','강화'][i],x+136,912,2,state==='disabled'?C.empty:C.ink,'center');
  }
  text(g,'선택은 모서리 + 체크 / 눌림은 1칸 이동 / 비활성은 빈 면',40,1020,2);
  save(c,'ui-components-v4.png');
}
(async()=>{
  fs.mkdirSync(ASSETS,{recursive:true});
  for(const name of Object.keys(maps)){
    const a=maps[name],w=a[0].length,h=a.length;
    for(const state of ['normal','pressed','disabled']){
      const [c,g]=canvas(w*4,h*4+4);icon(g,name,w*2,h*2,state);fs.writeFileSync(path.join(ASSETS,`${name}-${state}.png`),c.toBuffer('image/png'));
    }
  }
  for(const cfg of [{id:'normal',stage:3,name:'뿔토끼',boss:false},{id:'boss',stage:10,name:'큰엄니',boss:true}]){
    const scene=await loadImage(path.join(OUT,`battle-${cfg.id}-scene-v3.png`));
    for(const mode of ['clean','telegraph']){
      const [c,g]=canvas(1280,720);g.drawImage(scene,0,-16,1280,720);if(mode==='telegraph')telegraph(g);ui(g,cfg.stage,cfg.name,cfg.boss);
      save(c,`battle-${cfg.id}-${mode}-v4.png`);
    }
    if(!cfg.boss){const [c,g]=canvas(1280,720);g.drawImage(scene,0,-16,1280,720);ui(g,3,cfg.name,false,true);save(c,'battle-wireframe-v4.png')}
  }
  kit();
  console.log(`Rendered five battle studies, UI state sheet, and ${Object.keys(maps).length*3} transparent pixel UI assets.`);
})();
