// Install the reviewed source bytes; Unity's explicit migration imports and binds them.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../../..');
const read = p => JSON.parse(fs.readFileSync(path.join(root, p), 'utf8'));
const write = (p, value) => fs.writeFileSync(path.join(root, p), JSON.stringify(value, null, 2) + '\n');
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const delivery = read('Planning/Story/MonsterMotionFresh-v3/delivery.json');
const reviews = read('Planning/Story/MonsterMotion-v1/fresh-review.json');
const gallery = read('Planning/Story/MonsterMotion-v1/motions.json');
const manifestPath = 'Client/Assets/Game/Art/Presentation/manifest.json';
const manifest = read(manifestPath), data = read('GameData/mvp-catalog.json');
const plan = read('Planning/Story/route-onboarding-v4.json');
const artRoot = 'Assets/Game/Art/Presentation/Clips/monster-fresh-v3/';
const newActors = [], textures = [];
const copies = [];
for (const actor of delivery.actors) {
  if (!reviews.some(r => r.id === actor.id && r.passed && r.sourceSha256 === sha(JSON.stringify(actor))))
    throw new Error('Reviewed source changed: ' + actor.id);
  const selected = gallery.actors.find(a => a.group === 'candidate' && a.sourceId === actor.id);
  if (!selected) throw new Error('Missing gallery actor: ' + actor.id);
  const copy = source => {
    const original = 'Planning/Story/MonsterMotionFresh-v3/' + source;
    const dest = artRoot + source;
    const bytes = fs.readFileSync(path.join(root, original));
    textures.push({path:dest,source:original,sha256:sha(bytes),width:bytes.readUInt32BE(16),height:bytes.readUInt32BE(20),pivot:{x:actor.pivot[0],y:actor.pivot[1]}});
    copies.push({dest:'Client/' + dest,bytes});return dest;
  };
  const base = copy(actor.base);
  const clips = [{motion:'Idle',frames:[base],sourceFrames:[0],fps:1,loop:true,contactFrame:-1,staticFallback:true,sourceId:actor.id+'-fresh-v3-idle'}];
  for (const clip of actor.clips.filter(c => c.frames)) {
    if (clip.frames.length !== clip.frameDurationsMs.length || clip.frameDurationsMs.some(ms => ms <= 0)) throw new Error('Invalid timing');
    clips.push({...clip,frames:clip.frames.map(copy),sourceFrames:clip.frames.map((_,i)=>i),
      sourceId:actor.id+'-fresh-v3-'+clip.motion.toLowerCase(),
      fps:clip.frames.length*1000/clip.frameDurationsMs.reduce((a,b)=>a+b,0),staticFallback:false});
  }
  newActors.push({id:selected.id,facesRight:false,height:selected.height,referenceHeightPixels:actor.referenceHeightPixels,
    clips:clips.map(c=>({lowPoseFrames:[],recoverFrames:[],faceAnchors:[],effectOrigin:{x:0,y:0},legacyStun:false,referenceHeightPixels:0,...c}))});
  const enemy = data.enemies.find(e => e.id === actor.id);
  if (enemy) enemy.artId=selected.id;
  else data.enemies.push({id:actor.id,displayName:selected.name,artId:selected.id});
}
const audio = read('Planning/Story/Audio-v4/delivery.json');
for (const file of audio.files) {
  const bytes = fs.readFileSync(path.join(root,'Planning/Story/Audio-v4',file.file));
  if (sha(bytes) !== file.sha256) throw new Error('Audio source changed: ' + file.key);
  copies.push({dest:'Client/Assets/Game/Audio/Reviewed-v4/'+path.basename(file.file),bytes});
}
for (const name of ['CREDITS.txt','sources.json','delivery.json','impact-LICENSE.txt','rpg-LICENSE.txt'])
  copies.push({dest:'Client/Assets/Game/Audio/Reviewed-v4/'+name,bytes:fs.readFileSync(path.join(root,'Planning/Story/Audio-v4',name))});
// No edits until all reviewed sources have passed their integrity checks.
for (const {dest,bytes} of copies) {fs.mkdirSync(path.dirname(path.join(root,dest)),{recursive:true});fs.writeFileSync(path.join(root,dest),bytes);}
manifest.textures=manifest.textures.filter(t=>!t.path.startsWith(artRoot)).concat(textures);
manifest.actors=manifest.actors.filter(a=>!newActors.some(n=>n.id===a.id)).concat(newActors);
manifest.scope='Reviewed fresh-v3 monsters and approved hero run. Original five retained. Current gameplay ends at Stage 10; expansion actors are available for integration review.';
for (const stage of data.stages.filter(s=>s.number<=10)) stage.enemyId=plan.stages.find(s=>s.stage===stage.number).candidateEnemyId;
write(manifestPath,manifest);write('GameData/mvp-catalog.json',data);
console.log(JSON.stringify({reviewedActors:newActors.length,preservedActors:manifest.actors.length-newActors.length,textures:textures.length,audio:audio.files.length,stages:data.stages.filter(s=>s.number<=10).map(s=>({stage:s.number,enemyId:s.enemyId}))}));
