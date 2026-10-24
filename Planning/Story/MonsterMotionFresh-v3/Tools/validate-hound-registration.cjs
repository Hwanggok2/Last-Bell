// Verify rigid translation against the unmodified selected PixelLab frames.
const fs=require('node:fs'),path=require('node:path');
const {createCanvas,loadImage}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const root=path.resolve(__dirname,'..'),offsets=[8,10,23,21,18,18,18,2];
async function pixels(file){const im=await loadImage(file),c=createCanvas(128,128),ctx=c.getContext('2d');if(im.width!==128||im.height!==128)throw Error('Unexpected dimensions');ctx.drawImage(im,0,0);return ctx.getImageData(0,0,128,128).data;}
(async()=>{const frames=[];
 for(const [i,dy]of offsets.entries()){
  const name=String(i).padStart(3,'0')+'.png';
  const [before,after]=await Promise.all(['Attack-selected','Attack-grounded'].map(dir=>pixels(path.join(root,'Sources/hook_hound',dir,name))));
  const counts=[0,0],bounds=[[128,128,0,0],[128,128,0,0]];
  for(let k=0;k<16384;k++){
   for(const [j,p]of [before,after].entries())if(p[k*4+3]){counts[j]++;const x=k%128,y=k>>7;bounds[j]=[Math.min(bounds[j][0],x),Math.min(bounds[j][1],y),Math.max(bounds[j][2],x),Math.max(bounds[j][3],y)];}
   if(before[k*4+3])for(let c=0;c<4;c++)if(before[k*4+c]!==after[(k+128*dy)*4+c])throw Error('Visible pixel changed: frame '+(i+1));
  }
  if(counts[0]!==counts[1]||bounds[1][3]!==113)throw Error('Ground/alpha mismatch: frame '+(i+1));
  frames.push({frame:i+1,offset:[0,dy],beforeBounds:bounds[0],afterBounds:bounds[1],opaquePixels:counts[0],allVisiblePixelsPreserved:true});
 }
 fs.writeFileSync(path.join(root,'Review/hook_hound-registration.json'),JSON.stringify({passed:true,pivot:[78,114],supportingFootY:113,frames},null,2));
 console.log('PASS: 8 rigid translations; all visible RGBA pixels preserved; supporting feet at y113.');
})().catch(e=>{console.error(e);process.exitCode=1});
