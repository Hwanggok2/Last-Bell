import fs from 'node:fs';import path from 'node:path';import {createRequire}from'node:module';
const require=createRequire(import.meta.url);const{createCanvas,loadImage}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const here=process.cwd(),s=fs.readFileSync(path.join(here,'Planning/Presentation/data.js'),'utf8'),A=JSON.parse(s.split('window.ATLAS = ')[1].split(';')[0]);
const out=path.join(here,'Planning/Presentation/PixelLab/inputs');fs.mkdirSync(out,{recursive:true});
const hero=await loadImage(path.join(here,'Client/Assets/Game/Art/Source/on-poses-v1.png')),enemy=await loadImage(path.join(here,'Client/Assets/Game/Art/Source/gate-enemies-v1.png'));
const items=[['hero','on_00',84,60],['sentinel','gate_00',86,78],['chain','gate_01',86,78],['wisp','gate_02',88,76],['warden','gate_03',92,78],['knight','gate_04',94,78]],manifest=[];
for(const[id,key,h,x]of items){const a=A[key],c=createCanvas(128,128),g=c.getContext('2d');g.imageSmoothingEnabled=false;const scale=h/a.h,w=a.w*scale,left=Math.round(x-w*a.px),top=114-h;g.drawImage(a.sheet==='hero'?hero:enemy,a.x,a.y,a.w,a.h,left,top,Math.round(w),h);fs.writeFileSync(path.join(out,id+'.png'),c.toBuffer('image/png'));manifest.push({id,key,canvas:[128,128],foot:[x,114],height:h,sourceRect:a,method:'reference preparation only: crop existing idle, nearest sampling, transparent margins; no animation invented'});}
fs.writeFileSync(path.join(out,'reference-metadata.json'),JSON.stringify(manifest,null,2));console.log(manifest.map(x=>({id:x.id,height:x.height,foot:x.foot})));

