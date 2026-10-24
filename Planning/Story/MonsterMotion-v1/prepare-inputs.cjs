// Deterministic atlas slicing and target-cell export only; source drawings are generated assets.
const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const root=__dirname;
const groups=[['gate',['gate_sentry','bell_hound','cracked_blade','gate_lock','bell_knight']],['housing',['chain_puppet','hook_hound','chain_dancer','pendulum_warden','chain_jailer']]];
(async()=>{const manifest=[];
for(const [group,ids] of groups){const source=path.join(root,'Source',group+'-bases.png');const img=await loadImage(source);const c=createCanvas(img.width,img.height),ctx=c.getContext('2d');ctx.drawImage(img,0,0);const raw=ctx.getImageData(0,0,img.width,img.height).data;
const cols=[];for(let x=0;x<img.width;x++){let count=0;for(let y=0;y<img.height;y++)if(raw[(y*img.width+x)*4+3]>128)count++;cols.push(count);}
const runs=[];let start=-1,last=-1;for(let x=0;x<img.width;x++){if(cols[x]>4){if(start<0)start=x;last=x;}else if(start>=0&&x-last>14){runs.push([start,last]);start=-1;}}if(start>=0)runs.push([start,last]);
if(runs.length!==5)throw Error(group+' expected five silhouettes: '+JSON.stringify(runs));
for(let i=0;i<5;i++){const[x0,x1]=runs[i];let minY=img.height,maxY=-1;for(let x=x0;x<=x1;x++)for(let y=0;y<img.height;y++)if(raw[(y*img.width+x)*4+3]>128){minY=Math.min(minY,y);maxY=Math.max(maxY,y);}
const w=x1-x0+1,h=maxY-minY+1,scale=Math.min(92/h,88/w),tw=Math.round(w*scale),th=Math.round(h*scale);const out=createCanvas(128,128),o=out.getContext('2d');o.imageSmoothingEnabled=false;const left=Math.round(78-tw/2),top=114-th;o.drawImage(img,x0,minY,w,h,left,top,tw,th);const file='Inputs/'+ids[i]+'.png';fs.writeFileSync(path.join(root,file),out.toBuffer('image/png'));manifest.push({id:ids[i],source:'Source/'+group+'-bases.png',sourceRect:[x0,minY,w,h],input:file,cell:[128,128],pivot:[78,114],referenceHeightPixels:92,opaqueHeight:th});}
}
fs.writeFileSync(path.join(root,'inputs.json'),JSON.stringify(manifest,null,2)+'\n');console.log(manifest.map(m=>({id:m.id,rect:m.sourceRect,height:m.opaqueHeight})));
const board=createCanvas(640,256),b=board.getContext('2d');for(let i=0;i<manifest.length;i++){const img=await loadImage(path.join(root,manifest[i].input));b.drawImage(img,(i%5)*128,Math.floor(i/5)*128);}fs.writeFileSync(path.join(root,'Source/input-contact-sheet.png'),board.toBuffer('image/png'));
})();
