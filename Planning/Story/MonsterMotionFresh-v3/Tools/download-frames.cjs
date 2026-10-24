const fs=require('node:fs'),path=require('node:path');
const [actor,motion,attempt,job,countText,kind='image']=process.argv.slice(2);
if(!/^[a-z_]+$/.test(actor)||!['Attack','Stun'].includes(motion)||!/^\d+$/.test(attempt)||!/^[-a-f0-9]+$/.test(job))throw Error('Invalid arguments');
const count=Number(countText),root=path.resolve(__dirname,'..');
if(!['image','pixel'].includes(kind)||(kind==='pixel'&&count!==1))throw Error('Invalid download kind');
const folder=path.join(root,'Sources',actor,`${motion}-a${attempt}`);
fs.mkdirSync(folder,{recursive:true});
(async()=>{
 for(let start=0;start<count;start+=4){
  await Promise.all(Array.from({length:Math.min(4,count-start)},async(_,offset)=>{
   const index=start+offset;
   const url=kind==='pixel'?`https://api.pixellab.ai/mcp/pixel-tools/${job}/image.png`:`https://api.pixellab.ai/mcp/images/${job}/download?index=${index}`;
   const response=await fetch(url);
   if(!response.ok)throw Error(`${index}: ${response.status}`);
   const bytes=Buffer.from(await response.arrayBuffer());
   if(bytes.toString('hex',0,8)!=='89504e470d0a1a0a')throw Error('Expected PNG');
   fs.writeFileSync(path.join(folder,`${String(index).padStart(3,'0')}.png`),bytes);
  }));
 }
 fs.writeFileSync(path.join(folder,'result.json'),JSON.stringify({actor,motion,attempt:Number(attempt),jobId:job,frameCount:count,kind},null,2));
 console.log(`${actor} ${motion}: downloaded ${count} frames into ${folder}`);
})().catch(error=>{console.error(error);process.exitCode=1});
