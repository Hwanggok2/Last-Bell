// Original five runtime monsters stay untouched; this also assembles the review gallery.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const previewOnly=process.argv.includes('--preview-only');
const root=path.resolve(__dirname,'../../..'),manifestPath=path.join(root,'Client/Assets/Game/Art/Presentation/manifest.json');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8')),write=(p,o)=>fs.writeFileSync(p,JSON.stringify(o,null,2)+'\n');
const original=read(path.join(__dirname,'original-actors.json')),manifest=read(manifestPath),plan=read(path.join(root,'Planning/Story/route-onboarding-v4.json'));
const freshRoot=path.join(root,'Planning/Story/MonsterMotionFresh-v3'),inputs=read(path.join(freshRoot,'inputs.json'));
const deliveryPath=path.join(freshRoot,'delivery.json'),reviewPath=path.join(__dirname,'fresh-review.json');
const delivery=fs.existsSync(deliveryPath)?read(deliveryPath):{actors:[]},reviews=fs.existsSync(reviewPath)?read(reviewPath):[];
manifest.actors=[manifest.actors.find(a=>a.id==='hero_on'),...original.actors.map(a=>({...a,clips:a.clips.filter(c=>!['Hit','Death'].includes(c.motion))}))];
manifest.textures=manifest.textures.filter(t=>!t.path.includes('/monster-v4/'));
const gallery={version:'monster-motion-v3-three-states',generatedAt:new Date().toISOString(),hitFlash:{holdMs:45,fadeMs:75,previewCycleMs:900},actors:[]};
const atlasPath='Assets/Game/Art/Source/gate-enemies-v1.png',atlas=fs.readFileSync(path.join(root,'Client',atlasPath)),atlasHeight=atlas.readUInt32BE(20),meta=fs.readFileSync(path.join(root,'Client',atlasPath+'.meta'),'utf8');
function region(index){const b=meta.split('name: gate_'+String(index).padStart(2,'0')+'\n')[1].split('border:')[0];const nums=[...b.matchAll(/^        (x|y|width|height): ([\d.]+)/gm)].map(m=>Number(m[2]));const p=b.match(/pivot: \{x: ([\d.]+), y: ([\d.]+)\}/);return {crop:[nums[0],atlasHeight-nums[1]-nums[3],nums[2],nums[3]],pivot:[+p[1]*nums[2],(1-+p[2])*nums[3]]};}
function hit(a){return {...a.baseClip,motion:'Hit',effect:'whiteFlash',durationMs:900,fps:1,loop:false,contactFrame:-1};}
for(const [index,a] of manifest.actors.filter(a=>a.id!=='hero_on').entries()){
 const clips=a.clips.map(c=>{if(c.legacyStun){const r=region(index+10);return {...c,...r,referenceHeightPixels:region(index).crop[3],frames:['../../Client/'+atlasPath]};}const t=manifest.textures.find(t=>t.path===c.frames[0]);return {...c,pivot:[t.pivot.x,t.pivot.y],frames:c.frames.map(p=>'../../Client/'+p)};});
 const preview={...a,name:original.enemies.find(e=>e.id===a.id).displayName,group:'original',status:'preserved',baseClip:clips.find(c=>c.motion==='Idle'),clips:clips.filter(c=>c.motion!=='Idle')};preview.clips=[preview.clips.find(c=>c.motion==='Attack'),hit(preview),preview.clips.find(c=>c.motion==='Stun')];gallery.actors.push(preview);
}
function spritePath(p){const resolved=path.resolve(freshRoot,p);if(!resolved.startsWith(freshRoot+path.sep)||!fs.existsSync(resolved))throw Error('Invalid fresh sprite path: '+p);return 'MonsterMotionFresh-v3/'+path.relative(freshRoot,resolved).replaceAll('\\','/');}
for(const input of inputs){const id=input.id,stage=plan.stages.find(s=>(s.candidateEnemyId||s.enemyId)===id),brief=plan.monsters.find(a=>a.id===id);
 const actor={id:id==='bell_knight'?'bell_knight_concept':id,sourceId:id,name:brief.name,group:'candidate',status:'pending',height:stage.kind==='boss'?4:stage.kind==='miniboss'?3.2:2,referenceHeightPixels:input.referenceHeightPixels,pivot:input.pivot,clips:[]};
 actor.baseClip={motion:'Idle',frames:[spritePath(input.file)],fps:1,loop:true,contactFrame:-1,staticFallback:true};
 const source=delivery.actors.find(a=>a.id===id),hash=source?crypto.createHash('sha256').update(JSON.stringify(source)).digest('hex'):null;
 const approved=source&&reviews.some(r=>r.id===id&&r.passed&&r.sourceSha256===hash);
 if(approved){for(const type of ['Attack','Stun']){const c=source.clips.find(c=>c.motion===type);if(!c||!c.frames.length||c.frames.length!==c.frameDurationsMs?.length||c.frameDurationsMs.some(t=>!Number.isFinite(t)||t<=0)||c.contactFrame>=c.frames.length||(type==='Attack'&&c.contactFrame<0))throw Error('Invalid fresh clip: '+id+'/'+type);
 actor.clips.push({...c,frames:c.frames.map(spritePath),fps:1000*c.frames.length/c.frameDurationsMs.reduce((a,b)=>a+b,0),staticFallback:false});}actor.status='reviewed';actor.review=reviews.find(r=>r.id===id&&r.sourceSha256===hash);}
 else for(const type of ['Attack','Stun'])actor.clips.push({...actor.baseClip,motion:type,loop:false,pending:true});
 actor.clips=[actor.clips[0],hit(actor),actor.clips[1]];gallery.actors.push(actor);
}
if(!previewOnly)write(manifestPath,manifest);write(path.join(__dirname,'motions.json'),gallery);fs.writeFileSync(path.join(__dirname,'motions-data.js'),'window.MONSTER_MOTIONS='+JSON.stringify(gallery)+';\n');console.log(JSON.stringify({previewOnly,runtimeEnemies:5,candidates:10,reviewed:gallery.actors.filter(a=>a.status==='reviewed').length,selectableStates:['Attack','Hit','Stun'],idleAnimations:0}));
