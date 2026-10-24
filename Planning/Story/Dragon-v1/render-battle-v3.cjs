// Native UI layout over GPT-generated scene studies. Does not edit actor art.
const fs=require('fs'),path=require('path');
const {createCanvas,loadImage,GlobalFonts}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const BASE=__dirname,OUT=path.join(BASE,'images');
GlobalFonts.registerFromPath(path.resolve(BASE,'../../../Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf'),'Pixel');
const C={paper:'#FFF1D1',ink:'#273D45',edge:'#71543F',success:'#24766D',danger:'#B84232',muted:'#68705B',gold:'#B77A35'};
const rect=(g,x,y,w,h,c)=>{g.fillStyle=c;g.fillRect(x,y,w,h)};
function text(g,t,x,y,size=28,c=C.ink,align='left'){g.font=`${size}px Pixel`;g.textAlign=align;g.textBaseline='top';g.fillStyle=c;g.fillText(t,x,y);}
function plate(g,x,y,w,h){rect(g,x,y,w,h,C.edge);rect(g,x+4,y+4,w-8,h-8,C.paper);}
function bar(g,x,y,w,ratio,color){rect(g,x,y,w,16,C.ink);rect(g,x+3,y+3,w-6,10,C.paper);rect(g,x+3,y+3,Math.round((w-6)*ratio),10,color);}
function arrow(g,x,y,d){g.save();g.translate(x,y);g.rotate(d==='left'?-Math.PI/2:d==='right'?Math.PI/2:0);g.beginPath();g.moveTo(0,-24);g.lineTo(28,22);g.lineTo(0,10);g.lineTo(-28,22);g.closePath();g.fillStyle=C.success;g.fill();g.strokeStyle=C.ink;g.lineWidth=4;g.stroke();g.restore();}
function sword(g){g.save();g.translate(1192,636);g.rotate(Math.PI/4);rect(g,-8,-48,16,60,C.ink);rect(g,-4,-44,8,52,C.paper);rect(g,-24,12,48,10,C.gold);rect(g,-7,22,14,28,C.ink);g.restore();}
function shield(g){g.fillStyle=C.ink;g.beginPath();g.moveTo(622,72);g.lineTo(658,72);g.lineTo(658,96);g.lineTo(640,110);g.lineTo(622,96);g.closePath();g.fill();rect(g,636,78,8,20,C.paper);}
function ui(g,stage,name,boss,wire){
  // Existing battle layout: upper field, actor-follow HP, thumb controls below.
  rect(g,0,500,1280,220,C.paper);rect(g,0,500,1280,4,C.edge);
  plate(g,16,16,244,104);text(g,`Stage ${stage}`,30,24,36);text(g,stage===3?'골드 120':'골드 1,240',30,80,26);
  plate(g,1160,16,96,96);rect(g,1193,44,10,40,C.ink);rect(g,1213,44,10,40,C.ink);shield(g);
  bar(g,390,524,160,.8,C.success);bar(g,boss?710:718,524,boss?280:184,.7,C.danger);
  text(g,name,boss?850:810,554,28,C.ink,'center');arrow(g,148,526,'up');arrow(g,64,652,'left');arrow(g,268,652,'right');sword(g);
  if(wire){g.save();g.strokeStyle='#514798';g.lineWidth=3;g.setLineDash([8,6]);for(const b of [[96,476,104,104],[12,600,104,104],[216,600,104,104],[1120,564,144,144]])g.strokeRect(...b);g.restore();}
}
function telegraph(g){
  // A readable near-impact study, not a timing animation or damage calculation.
  g.save();g.beginPath();g.rect(0,112,1280,388);g.clip();
  for(const x of [-160,1440]){g.beginPath();g.arc(x,360,640,0,Math.PI*2);g.strokeStyle=C.ink;g.lineWidth=6;g.stroke();g.strokeStyle=C.success;g.lineWidth=2;g.stroke();}
  for(const [x,y,sx,sy] of [[350,424,1,1],[800,424,-1,1],[350,460,1,-1],[800,460,-1,-1]]){
    g.strokeStyle=C.ink;g.lineWidth=10;g.beginPath();g.moveTo(x+sx*28,y);g.lineTo(x,y);g.lineTo(x,y+sy*20);g.stroke();g.strokeStyle=C.danger;g.lineWidth=5;g.stroke();
  }g.restore();
}
(async()=>{
  for(const cfg of [{id:'normal',stage:3,name:'뿔토끼',boss:false},{id:'boss',stage:10,name:'큰엄니',boss:true}]){
    const scene=await loadImage(path.join(OUT,`battle-${cfg.id}-scene-v3.png`));
    for(const mode of ['clean','telegraph']){
      const c=createCanvas(1280,720),g=c.getContext('2d');g.imageSmoothingEnabled=false;g.drawImage(scene,0,-16,1280,720);
      if(mode==='telegraph')telegraph(g);ui(g,cfg.stage,cfg.name,cfg.boss,false);
      fs.writeFileSync(path.join(OUT,`battle-${cfg.id}-${mode}-v3.png`),c.toBuffer('image/png'));
    }
  }
  const scene=await loadImage(path.join(OUT,'battle-normal-scene-v3.png'));const c=createCanvas(1280,720),g=c.getContext('2d');g.imageSmoothingEnabled=false;g.drawImage(scene,0,-16,1280,720);ui(g,3,'뿔토끼',false,true);fs.writeFileSync(path.join(OUT,'battle-wireframe-v3.png'),c.toBuffer('image/png'));
  console.log('Rendered normal/boss clean and telegraph views plus touch-area study.');
})();
