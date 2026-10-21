/* Actual PixelLab raster frames for planning. No procedural body parts or runtime changes. */
const PL=window.PIXELLAB_CLIPS.clips,PL_BY_ID=Object.fromEntries(PL.map(c=>[c.id,c]));
for(const c of PL)paths['pl_'+c.id]='PixelLab/'+c.sheet;
const PL_ACTOR={hero_on:'hero',gate_sentinel:'sentinel',chain_thrall:'chain',ash_wisp:'wisp',gate_warden:'warden',bell_knight:'knight'};
const PL_REF={hero:84,sentinel:86,chain:86,wisp:88,warden:92,knight:94};
const PL_DODGE={dodge_up:'up',dodge_down:'down',dodge_forward:'front',dodge_back:'back'};
function clipAvailable(id){return PL_BY_ID[id]&&images['pl_'+id]&&PL_BY_ID[id].status!=='rejected'}
function clipIndex(id,time,duration,loop=false){const c=PL_BY_ID[id];if(!c)return 0;const order=c.playbackFrames||Array.from({length:c.frameCount},(_,i)=>i);const p=loop?((time%duration)+duration)%duration/duration:clamp(time/duration);return order[Math.min(order.length-1,Math.floor(p*order.length))]}
function strikeIndex(id,time,contactTime=100,end=500){const c=PL_BY_ID[id];if(!c)return 0;const order=c.playbackFrames||Array.from({length:c.frameCount},(_,i)=>i),hit=Math.max(1,order.indexOf(c.contactSourceFrame??9));const at=time<contactTime?Math.floor(clamp(time/contactTime)*hit):hit+Math.floor(clamp((time-contactTime)/(end-contactTime))*(order.length-1-hit));return order[Math.min(order.length-1,at)]}
function drawClip(ctx,id,index,x=500,foot=590,height=1.1){const c=PL_BY_ID[id],sheet=images['pl_'+id];if(!c||!sheet)return null;const scale=height*116.13/PL_REF[c.actor],left=x-c.pivot[0]*scale,top=foot-c.pivot[1]*scale;ctx.imageSmoothingEnabled=false;ctx.drawImage(sheet,index%c.columns*c.frameWidth,Math.floor(index/c.columns)*c.frameHeight,c.frameWidth,c.frameHeight,Math.round(left),Math.round(top),Math.round(c.frameWidth*scale),Math.round(c.frameHeight*scale));return{left,top,w:c.frameWidth*scale,h:c.frameHeight*scale,index};}
let strikeVariantIndex=1;
function slashVariant(index=strikeVariantIndex){return P.playerAttack.variants[index]}
function rollSlashVariant(random=Math.random){const count=P.playerAttack.variants.length;strikeVariantIndex=(strikeVariantIndex+1+Math.floor(clamp(random(),0,.999999)*(count-1)))%count;return slashVariant()}
function selectedRun(){return 'hero-v6-run'}
function selectedAttack(index=strikeVariantIndex){return slashVariant(index).clip}
function selectedDodge(direction){return direction==='down'&&clipAvailable('hero-v5-dodge-down-retry')?'hero-v5-dodge-down-retry':'hero-v5-dodge-'+direction}
function drawRun(ctx,x=500,foot=590,time=0){const id=selectedRun();if(clipAvailable(id)){const index=clipIndex(id,time,P.parallax.runCycleMs,true);drawClip(ctx,id,index,x,foot,1.1);return{rootX:x,frame:index,cycleMs:P.parallax.runCycleMs,source:id}}sprite(ctx,'on_00',x,foot,1.1);return{rootX:x,frame:0,cycleMs:P.parallax.runCycleMs,source:'idle-reference'};}
function circleWave(ctx,x,y,r,alpha){ctx.save();ctx.globalAlpha=alpha;const pts=Array.from({length:65},(_,i)=>[x+Math.cos(i*Math.PI/32)*r,y+Math.sin(i*Math.PI/32)*r]);line(ctx,pts,C.dark,8);line(ctx,pts,C.gold,4);line(ctx,pts,C.cream,2);ctx.restore()}
function clipFace(id,index,key){const c=PL_BY_ID[id];if(!c||!clipAvailable(id))return P.bossRoar.faceAnchors[key].idle;const height=key==='mini'?3.2:4,scale=height*116.13/PL_REF[c.actor],points=c.faceByFrame||{},local=points[index]||c.faceAnchor||(key==='mini'?[70,46]:[75,43]);return[900+(local[0]-c.pivot[0])*scale,590+(local[1]-c.pivot[1])*scale]}
function drawBossRoar(ctx,key,t,cam){const holding=t>=cam.inEnd&&t<cam.holdEnd,p=clamp((t-cam.inEnd)/(cam.holdEnd-cam.inEnd)),id=key==='mini'?'warden-attack':clipAvailable('knight-attack-retry')?'knight-attack-retry':'knight-attack',height=key==='mini'?3.2:4;
 if(holding&&clipAvailable(id))drawClip(ctx,id,clipIndex(id,t-cam.inEnd,cam.holdEnd-cam.inEnd),900,590,height);else sprite(ctx,key==='mini'?'gate_03':'gate_04',900,590,height);
 const face=clipFace(id,clipIndex(id,t-cam.inEnd,cam.holdEnd-cam.inEnd),key);
 if(holding)for(const emit of [0,.24,.52]){const age=(p-emit)/P.bossRoar.pulseLifetimeFraction,origin=clipFace(id,clipIndex(id,emit,1),key);if(age>=0&&age<=1)circleWave(ctx,origin[0],origin[1],mix(24,108,age),1-age)}
 return{attackCycles:holding?1:0,damageEvents:0,face,phase:holding?'PixelLab-attack':'idle'};
}
function heroMotion(ctx,state,time,dur=600,reduced=false){if(state==='attack')return drawPlayerAttack(ctx,time);if(state==='run')return drawRun(ctx,500,590,time);const id=PL_DODGE[state]?selectedDodge(PL_DODGE[state]):'hero-v5-'+state;
 if(clipAvailable(id)){const index=clipIndex(id,time,state==='death'?300:dur,state==='idle'),vanish=state==='death'?clamp((time-200)/350):0;const box=drawHeroClip(ctx,id,index,500,590,1,vanish);if(state==='death')debris(ctx,box,vanish,reduced);return{rootX:500,index,source:id}}
 sprite(ctx,'on_00',500,590,1.1);return{rootX:500,source:'awaiting-v5'};
}
function motionClip(actorId,state){if(actorId==='hero_on')return state==='run'?selectedRun():state==='attack'?selectedAttack():PL_DODGE[state]?selectedDodge(PL_DODGE[state]):'hero-v5-'+state;if(state==='hit')return PL_ACTOR[actorId]+'-v6-hit-clean';if(state==='roar'||state.startsWith('attack_'))return actorId==='bell_knight'&&clipAvailable('knight-attack-retry')?'knight-attack-retry':PL_ACTOR[actorId]+'-attack';return null}
function motionFrame(canvas,actorId='hero_on',state='run',time=200,dur=600){if(actorId==='hero_on'&&state==='attack')return playerFrame(canvas,time,false);const ctx=canvas.getContext('2d');clear(ctx);background(ctx);const selected=P.actors.find(a=>a.id===actorId),id=motionClip(actorId,state);
 if(actorId==='hero_on'){heroMotion(ctx,state,time,dur);sprite(ctx,'gate_00',900,590,2)}else{sprite(ctx,'on_00',500,590,1.1);if(id&&clipAvailable(id))drawClip(ctx,id,clipIndex(id,time,dur),900,590,selected.proposed);else sprite(ctx,selected.sprite,900,590,selected.proposed)}
 hud(ctx,{stage:actorId==='bell_knight'?10:actorId==='gate_warden'?5:1,boss:['bell_knight','gate_warden'].includes(actorId)});return{actorId,state,time,durationMs:dur,source:id&&clipAvailable(id)?id:'idle-reference'};
}
function slashGeometry(targetId='gate_sentinel',variantIndex=strikeVariantIndex){
 const v=slashVariant(variantIndex),target=P.playerAttack.targets[targetId],clip=PL_BY_ID[v.clip],hand=clip?.effectOrigin||[85,v.direction==='up'?78:28],pivot=clip?.pivot||[60,114],scale=1.1*116.13/84;
 const start=[target.strikeX+(hand[0]-pivot[0])*scale,590+(hand[1]-pivot[1])*scale],aim=target[v.direction],angle=Math.atan2(aim[1]-start[1],aim[0]-start[0])+v.angleOffsetDeg*Math.PI/180;
 const distance=(aim[0]-start[0])/Math.cos(angle),hit=[aim[0],start[1]+Math.sin(angle)*distance],length=Math.min(distance+55,v.direction==='down'?(582-start[1])/Math.sin(angle):Infinity);
 return{start,end:[start[0]+Math.cos(angle)*length,start[1]+Math.sin(angle)*length],hit,angle,variant:v};
}
function drawDiagonalSlash(ctx,time,critical=false,targetId='gate_sentinel',variantIndex=strikeVariantIndex){
 const c=PL_BY_ID['diagonal-slash'];if(!c||!images['pl_diagonal-slash']||time<0||time>=250)return;
 const index=Math.min(c.frameCount-1,Math.floor(time/25)),g=slashGeometry(targetId,variantIndex),scale=Math.hypot(g.end[0]-g.start[0],g.end[1]-g.start[1])/Math.hypot(213,86);
 ctx.save();ctx.imageSmoothingEnabled=false;ctx.translate(...g.start);ctx.rotate(g.angle-Math.atan2(-86,213));ctx.scale(scale,scale);ctx.drawImage(images['pl_diagonal-slash'],index%c.columns*256,Math.floor(index/c.columns)*128,256,128,-26,-104,256,128);ctx.restore();
}
function attackPose(time,targetId='gate_sentinel',variantIndex=strikeVariantIndex){const a=P.playerAttack,t=clamp(time,0,a.nextAttackReadyMs),home=a.homeX,close=a.targets[targetId].strikeX;if(t<a.approachEndMs)return{x:mix(home,close,1-Math.pow(1-t/a.approachEndMs,2)),phase:'approach',clip:selectedRun()};if(t<a.returnStartMs)return{x:close,phase:t<a.windupMs?'windup':'strike',clip:selectedAttack(variantIndex)};if(t<a.returnEndMs)return{x:mix(close,home,ease((t-a.returnStartMs)/(a.returnEndMs-a.returnStartMs))),phase:'return',clip:'hero-v5-return'};return{x:home,phase:'idle',clip:'hero-v5-idle'};}
function drawPlayerAttack(ctx,time,targetId='gate_sentinel',variantIndex=strikeVariantIndex){const a=P.playerAttack,p=attackPose(time,targetId,variantIndex);let index=0;if(p.phase==='approach')index=clipIndex(p.clip,time,P.parallax.runCycleMs,true);else if(p.phase==='windup'||p.phase==='strike')index=strikeIndex(p.clip,Math.max(0,time-a.approachEndMs),a.windupMs-a.approachEndMs,a.returnStartMs-a.approachEndMs);else if(p.phase==='return')index=clipIndex(p.clip,time-a.returnStartMs,a.returnEndMs-a.returnStartMs);else index=clipIndex(p.clip,time,700,true);if(clipAvailable(p.clip))drawHeroClip(ctx,p.clip,index,p.x,590);else sprite(ctx,'on_00',p.x,590,1.1);return{rootX:p.x,phase:p.phase,source:p.clip,index};}
function drawMonsterHit(ctx,targetId,after){const a=actor(targetId),id=PL_ACTOR[targetId]+'-v6-hit-clean',active=after>=0&&after<P.playerAttack.enemyHitMs;if(clipAvailable(id))return drawClip(ctx,id,active?clipIndex(id,after,P.playerAttack.enemyHitMs):0,900,590,a.proposed);return sprite(ctx,a.sprite,900,590,a.proposed)}
function drawHeroClip(ctx,id,index,x,foot,alpha=1,vanish=0){const c=PL_BY_ID[id],scale=1.1*116.13/84,left=x-c.pivot[0]*scale,top=foot-c.pivot[1]*scale;ctx.save();ctx.globalAlpha=alpha;if(vanish>0){ctx.beginPath();for(let y=0;y<196;y+=8)for(let xx=0;xx<196;xx+=8)if(((y*73+xx*31+y*xx*13)%101)/101>vanish)ctx.rect(left+xx,top+y,8,8);ctx.clip()}const box=drawClip(ctx,id,index,x,foot,1.1);ctx.restore();const b=c.frameBounds?.[index];return b?{left:left+b[0]*scale,top:top+b[1]*scale,w:(b[2]-b[0])*scale,h:(b[3]-b[1])*scale}:box;}
// Static scenes use the newly generated idle. Old source art stays available only in the archive.
const legacySprite=sprite;
sprite=function(ctx,name,x,foot,height,alpha=1,vanish=0,wire=false){if(name==='on_00'&&clipAvailable('hero-v5-idle')&&!wire)return drawHeroClip(ctx,'hero-v5-idle',1,x,foot,alpha,vanish);return legacySprite(ctx,name,x,foot,height,alpha,vanish,wire)};
