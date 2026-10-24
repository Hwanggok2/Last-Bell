const fs=require('node:fs'),path=require('node:path');
const {createCanvas,loadImage}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
(async()=>{const examples=[['gate_sentry','AttackThrust'],['gate_sentry','Death'],['bell_hound','AttackLow'],['cracked_blade','AttackHigh'],['gate_lock','AttackLow'],['bell_knight','Hit']];
const c=createCanvas(128*9,160*examples.length),ctx=c.getContext('2d');ctx.fillStyle='#263b4f';ctx.fillRect(0,0,c.width,c.height);ctx.font='18px sans-serif';
for(const [row,[id,motion]] of examples.entries()){ctx.fillStyle='#f4d79b';ctx.fillText(id+' / '+motion,10,row*160+20);for(let i=0;i<9;i++){const p=path.join(__dirname,'Frames',id,motion,String(i).padStart(2,'0')+'.png');if(fs.existsSync(p))ctx.drawImage(await loadImage(p),i*128,row*160+26);}}
fs.writeFileSync(path.join(__dirname,'Source/motion-review-1.png'),c.toBuffer('image/png'));})();
