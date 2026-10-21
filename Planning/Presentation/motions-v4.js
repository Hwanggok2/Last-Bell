/* Actual PixelLab raster frames for planning. No procedural body parts or runtime changes. */
const PL=window.PIXELLAB_CLIPS.clips,PL_BY_ID=Object.fromEntries(PL.map(c=>[c.id,c]));
for(const c of PL)paths['pl_'+c.id]='PixelLab/'+c.sheet;
const PL_ACTOR={hero_on:'hero',gate_sentinel:'sentinel',chain_thrall:'chain',ash_wisp:'wisp',gate_warden:'warden',bell_knight:'knight'};
const PL_REF={hero:84,sentinel:86,chain:86,wisp:88,warden:92,knight:94};
const PL_DODGE={dodge_up:'up',dodge_down:'down',dodge_forward:'front',dodge_back:'back'};
function clipAvailable(id){return PL_BY_ID[id]&&images['pl_'+id]&&PL_BY_ID[id].status!=='rejected'}
function clipIndex(id,time,duration,loop=false){const c=PL_BY_ID[id];if(!c)return 0;const order=c.playbackFrames||Array.from({length:c.frameCount},(_,i)=>i);const p=loop?((time%duration)+duration)%duration/duration:clamp(time/duration);return order[Math.min(order.length-1,Math.floor(p*order.length))]}
function strikeIndex(id,time,contactTime=100,end=500){const c=PL_BY_ID[id],order=c.playbackFrames||Array.from({length:c.frameCount},(_,i)=>i),hit=Math.max(1,order.indexOf(c.contactSourceFrame??9));const at=time<contactTime?Math.floor(clamp(time/contactTime)*hit):hit+Math.floor(clamp((time-contactTime)/(end-contactTime))*(order.length-1-hit));return order[Math.min(order.length-1,at)]}
function drawClip(ctx,id,index,x=500,foot=590,height=1.1){const c=PL_BY_ID[id],sheet=images['pl_'+id];if(!c||!sheet)return null;const scale=height*116.13/PL_REF[c.actor],left=x-c.pivot[0]*scale,top=foot-c.pivot[1]*scale;ctx.imageSmoothingEnabled=false;ctx.drawImage(sheet,index%c.columns*c.frameWidth,Math.floor(index/c.columns)*c.frameHeight,c.frameWidth,c.frameHeight,Math.round(left),Math.round(top),Math.round(c.frameWidth*scale),Math.round(c.frameHeight*scale));return{left,top,w:c.frameWidth*scale,h:c.frameHeight*scale,index};}
function selectedRun(){return clipAvailable('hero-run-retry')?'hero-run-retry':'hero-run'}
function selectedAttack(){return clipAvailable('hero-attack-retry')?'hero-attack-retry':null}
function drawRun(ctx,x=500,foot=590,time=0){const id=selectedRun();if(clipAvailable(id)){const index=clipIndex(id,time,P.parallax.runCycleMs,true);drawClip(ctx,id,index,x,foot,1.1);return{rootX:x,frame:index,cycleMs:P.parallax.runCycleMs,source:id}}sprite(ctx,'on_00',x,foot,1.1);return{rootX:x,frame:0,cycleMs:P.parallax.runCycleMs,source:'idle-reference'};}
function circleWave(ctx,x,y,r,alpha){ctx.save();ctx.globalAlpha=alpha;const pts=Array.from({length:65},(_,i)=>[x+Math.cos(i*Math.PI/32)*r,y+Math.sin(i*Math.PI/32)*r]);line(ctx,pts,C.dark,8);line(ctx,pts,C.gold,4);line(ctx,pts,C.cream,2);ctx.restore()}
function clipFace(id,index,key){const c=PL_BY_ID[id];if(!c||!clipAvailable(id))return P.bossRoar.faceAnchors[key].idle;const height=key==='mini'?3.2:4,scale=height*116.13/PL_REF[c.actor],points=c.faceByFrame||{},local=points[index]||c.faceAnchor||(key==='mini'?[70,46]:[75,43]);return[900+(local[0]-c.pivot[0])*scale,590+(local[1]-c.pivot[1])*scale]}
function drawBossRoar(ctx,key,t,cam){const holding=t>=cam.inEnd&&t<cam.holdEnd,p=clamp((t-cam.inEnd)/(cam.holdEnd-cam.inEnd)),id=key==='mini'?'warden-attack':clipAvailable('knight-attack-retry')?'knight-attack-retry':'knight-attack',height=key==='mini'?3.2:4;
 if(holding&&clipAvailable(id))drawClip(ctx,id,clipIndex(id,t-cam.inEnd,cam.holdEnd-cam.inEnd),900,590,height);else sprite(ctx,key==='mini'?'gate_03':'gate_04',900,590,height);
 const face=clipFace(id,clipIndex(id,t-cam.inEnd,cam.holdEnd-cam.inEnd),key);
 if(holding)for(const emit of [0,.24,.52]){const age=(p-emit)/P.bossRoar.pulseLifetimeFraction,origin=clipFace(id,clipIndex(id,emit,1),key);if(age>=0&&age<=1)circleWave(ctx,origin[0],origin[1],mix(24,108,age),1-age)}
 return{attackCycles:holding?1:0,damageEvents:0,face,phase:holding?'PixelLab-attack':'idle'};
}
function heroMotion(ctx,state,time,dur=600){if(state==='run')return drawRun(ctx,500,590,time);const id=state==='attack'?selectedAttack():PL_DODGE[state]?'hero-dodge-'+PL_DODGE[state]:null;
 if(id&&clipAvailable(id)){const index=clipIndex(id,time,dur);drawClip(ctx,id,index,500,590,1.1);return{rootX:500,index,source:id}}
 const box=sprite(ctx,'on_00',500,590,1.1,1,state==='death'?clamp(time/dur):0);if(state==='death')debris(ctx,box,clamp(time/dur));return{rootX:500,source:'idle-reference'};
}
function motionClip(actorId,state){if(actorId==='hero_on')return state==='run'?selectedRun():state==='attack'?selectedAttack():PL_DODGE[state]?'hero-dodge-'+PL_DODGE[state]:null;if(state==='roar'||state.startsWith('attack_'))return actorId==='bell_knight'&&clipAvailable('knight-attack-retry')?'knight-attack-retry':PL_ACTOR[actorId]+'-attack';return null}
function motionFrame(canvas,actorId='hero_on',state='run',time=200,dur=600){const ctx=canvas.getContext('2d');clear(ctx);background(ctx);const selected=P.actors.find(a=>a.id===actorId),id=motionClip(actorId,state);
 if(actorId==='hero_on'){heroMotion(ctx,state,time,dur);sprite(ctx,'gate_00',900,590,2)}else{sprite(ctx,'on_00',500,590,1.1);if(id&&clipAvailable(id))drawClip(ctx,id,clipIndex(id,time,dur),900,590,selected.proposed);else sprite(ctx,selected.sprite,900,590,selected.proposed)}
 hud(ctx,{stage:actorId==='bell_knight'?10:actorId==='gate_warden'?5:1,boss:['bell_knight','gate_warden'].includes(actorId)});return{actorId,state,time,durationMs:dur,source:id&&clipAvailable(id)?id:'idle-reference'};
}
function drawDiagonalSlash(ctx,time,critical=false){const c=PL_BY_ID['diagonal-slash'];if(!c||!images['pl_diagonal-slash']||time<0||time>=250)return;const index=Math.min(c.frameCount-1,Math.floor(time/25));ctx.save();ctx.imageSmoothingEnabled=false;ctx.drawImage(images['pl_diagonal-slash'],index%c.columns*256,Math.floor(index/c.columns)*128,256,128,487,383,475,184);ctx.restore();}
