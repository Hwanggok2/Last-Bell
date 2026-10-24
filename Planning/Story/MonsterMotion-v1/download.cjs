const fs=require('node:fs/promises'),path=require('node:path');
(async()=>{
 const jobs=JSON.parse(await fs.readFile(path.join(__dirname,'jobs.json'),'utf8'));
 const tasks=jobs.filter(j=>j.status==='completed').flatMap(j=>Array.from({length:j.totalFrames},(_,i)=>({j,i})));
 let count=0,downloaded=0;
 async function worker(){while(tasks.length){const {j,i}=tasks.shift();const dir=path.join(__dirname,'Frames',j.id,j.motion);await fs.mkdir(dir,{recursive:true});const target=path.join(dir,String(i).padStart(2,'0')+'.png');try{await fs.access(target);count++;continue;}catch{}
  const r=await fetch(`${j.download}?index=${i}`);if(!r.ok)throw Error(`${j.id}/${j.motion}/${i}: ${r.status}`);const data=Buffer.from(await r.arrayBuffer());if(data.readUInt32BE(0)!==0x89504e47)throw Error('Not PNG');await fs.writeFile(target,data);count++;downloaded++;
 }}
 await Promise.all(Array.from({length:6},worker));console.log(JSON.stringify({framesAvailable:count,newDownloads:downloaded}));
})().catch(e=>{console.error(e);process.exitCode=1});
