const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const configs=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
const actors=configs.map(c=>({id:c.id,base:`Inputs/${c.id}.png`,pivot:[78,114],referenceHeightPixels:92,
 clips:['Attack','Stun'].map(m=>({motion:m,frames:c[m].selection.map(n=>n==='base'?`Inputs/${c.id}.png`:`${c[m].source}/${String(n).padStart(3,'0')}.png`),frameDurationsMs:c[m].durations,contactFrame:m==='Attack'?c[m].contactFrame:-1,loop:m==='Stun'})).concat({motion:'Hit',effect:'whiteFlash',holdMs:45,fadeMs:75}),
 review:c.review}));
fs.writeFileSync(path.join(root,'Review','candidate-manifest.json'),JSON.stringify({version:3,actors},null,2));
console.log(`Selected candidates: ${actors.length}`);
