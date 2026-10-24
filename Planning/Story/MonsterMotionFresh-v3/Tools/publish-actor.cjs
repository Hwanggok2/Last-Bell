const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const config=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
if(config.review?.passed!==true)throw Error('Visual approval is required before publishing');
const actor={id:config.id,base:`Inputs/${config.id}.png`,pivot:[78,114],referenceHeightPixels:92,clips:[],review:config.review};
for(const motion of ['Attack','Stun']){
 const spec=config[motion];
 if(spec.selection.length!==spec.durations.length)throw Error('Frame/timing count mismatch');
 const target=path.join(root,'Actors',config.id,motion);fs.mkdirSync(target,{recursive:true});
 const frames=spec.selection.map((index,i)=>{
  const input=index==='base'?actor.base:`${spec.source}/${String(index).padStart(3,'0')}.png`;
  const output=`Actors/${config.id}/${motion}/frame-${String(i).padStart(3,'0')}.png`;
  fs.copyFileSync(path.join(root,input),path.join(root,output));return output;
 });
 actor.clips.push({motion,frames,frameDurationsMs:spec.durations,contactFrame:motion==='Attack'?spec.contactFrame:-1,loop:motion==='Stun'});
}
actor.clips.push({motion:'Hit',effect:'whiteFlash',holdMs:45,fadeMs:75});
const file=path.join(root,'delivery.json');
const delivery=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
const previous=delivery.actors.findIndex(a=>a.id===actor.id);
if(previous<0)delivery.actors.push(actor);else delivery.actors[previous]=actor;
fs.writeFileSync(file+'.tmp',JSON.stringify(delivery,null,2));fs.renameSync(file+'.tmp',file);
fs.writeFileSync(path.join(root,'progress.json'),JSON.stringify({phase:delivery.actors.length===10?'completed':'production',completedActors:delivery.actors.length,targetActors:10,lastCompletedActor:actor.id},null,2));
console.log(`Published ${actor.id}: ${delivery.actors.length}/10.`);
