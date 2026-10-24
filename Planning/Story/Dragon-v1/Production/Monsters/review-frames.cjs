const fs = require('fs');
const path = require('path');
const {createCanvas,loadImage}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const root=__dirname;
async function review(actor){
 const clips=['idle','attack','stun','attack-retry','stun-retry'].filter(m=>fs.existsSync(path.join(root,actor,m,'raw')));
 const cv=createCanvas(1512,clips.length*226);const cx=cv.getContext('2d');cx.imageSmoothingEnabled=false;cx.fillStyle='#FFF2D8';cx.fillRect(0,0,cv.width,cv.height);
 const metrics={};
 for(let row=0;row<clips.length;row++){
  const clip=clips[row]; metrics[clip]=[];
  for(let i=0;i<9;i++){
   const p=path.join(root,actor,clip,'raw',String(i).padStart(2,'0')+'.png'); if(!fs.existsSync(p))continue;
   const im=await loadImage(p);const test=createCanvas(im.width,im.height);const tx=test.getContext('2d');tx.drawImage(im,0,0);const d=tx.getImageData(0,0,im.width,im.height).data;
   const colors=new Set(),alphas=new Set();let x0=im.width,y0=im.height,x1=-1,y1=-1;
   for(let y=0;y<im.height;y++)for(let x=0;x<im.width;x++){const k=(y*im.width+x)*4;alphas.add(d[k+3]);if(!d[k+3])continue;colors.add(Array.from(d.slice(k,k+3)).join(','));x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
   metrics[clip].push({index:i,width:im.width,height:im.height,bbox:[x0,y0,x1,y1],colors:colors.size,alpha:[...alphas].sort((a,b)=>a-b)});
   const s=Math.min(4,152/im.width),x=i*168+(168-im.width*s)/2,y=row*226+38;
   cx.fillStyle='#20383C';cx.font='14px sans-serif';cx.fillText(clip+' '+i,i*168+8,row*226+21);cx.fillStyle='#D4DED1';cx.fillRect(i*168+6,row*226+204,156,1);cx.drawImage(im,x,y,im.width*s,im.height*s);
  }
 }
 fs.writeFileSync(path.join(root,actor,'contact.png'),cv.toBuffer('image/png'));fs.writeFileSync(path.join(root,actor,'metrics.json'),JSON.stringify(metrics,null,2));console.log(actor,JSON.stringify(metrics));
}
(async()=>{for(const a of process.argv.slice(2))await review(a)})();
