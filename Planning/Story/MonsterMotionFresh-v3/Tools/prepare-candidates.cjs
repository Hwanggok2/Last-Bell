const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const specs=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
const actors=specs.map(spec=>{
 const actor={id:spec.id,base:`Inputs/${spec.id}.png`,pivot:[78,114],referenceHeightPixels:92,clips:[],review:{passed:false,notes:'생성 후보입니다. 아직 채택되지 않았습니다. 타격 프레임·프레임 선택·재생 시간은 시각 검수 후 확정합니다.'}};
 for(const motion of ['Attack','Stun']){
  const folder=spec[motion];
  const frames=fs.readdirSync(path.join(root,folder)).filter(f=>/^\d{3}\.png$/.test(f)).sort().map(f=>`${folder}/${f}`);
  actor.clips.push({motion,frames,frameDurationsMs:frames.map(()=>motion==='Attack'?80:120),contactFrame:-1,loop:motion==='Stun'});
 }
 actor.clips.push({motion:'Hit',effect:'whiteFlash',holdMs:45,fadeMs:75});return actor;
});
fs.writeFileSync(path.join(root,'Review','candidate-manifest.json'),JSON.stringify({version:3,actors},null,2));
console.log(`Prepared ${actors.length} unapproved candidates.`);
