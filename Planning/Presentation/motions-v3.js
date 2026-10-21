/* Pose/timing study: existing sprite poses + procedural legs. Not finished animation assets. */
function runPose(time){return Math.floor((time%P.parallax.runCycleMs)/P.parallax.runCycleMs*6)}
function drawRun(ctx,x=500,foot=590,time=0){
 const frame=runPose(time),poses=[[[ -15,-24],[-30,-18],[20,-20],[40,0]],[[5,-30],[15,-25],[5,-20],[0,0]],[[22,-26],[40,-14],[-20,-16],[-40,0]],[[20,-20],[40,0],[-15,-24],[-30,-18]],[[5,-20],[0,0],[5,-30],[15,-25]],[[-20,-16],[-40,0],[22,-26],[40,-14]]],fraction=(time%100)/100,q=poses[frame].map((v,i)=>v.map((x,j)=>mix(x,poses[(frame+1)%6][i][j],fraction))),bob=[0,3,6,0,3,6][frame];
 for(let leg=0;leg<2;leg++){const knee=q[leg*2],toe=q[leg*2+1],hip=[x+(leg?7:-8),foot-35-bob];line(ctx,[hip,[x+knee[0],foot+knee[1]],[x+toe[0],foot+toe[1]-5]],C.dark,19);line(ctx,[hip,[x+knee[0],foot+knee[1]],[x+toe[0],foot+toe[1]-5]],leg?C.cream:C.old,10);rect(ctx,x+toe[0]-8,foot+toe[1]-7,24,7,C.dark);rect(ctx,x+toe[0]-6,foot+toe[1]-7,20,4,C.gold)}
 ctx.save();ctx.beginPath();ctx.rect(x-100,foot-160,220,125-bob);ctx.clip();sprite(ctx,frame%3===1?'on_01':'on_00',x,foot-bob,1.1);ctx.restore();
 if(frame===0||frame===3)for(let i=0;i<3;i++)rect(ctx,x-30-i*10,foot-2-i%2*4,4,4,'#72829a');
 return{rootX:x,frame,cycleMs:P.parallax.runCycleMs};
}
function roarPose(key,p){const n=key==='mini'?3:4,attack=p>=.18&&p<.62;return{sprite:'gate_'+String(n+(attack?5:0)).padStart(2,'0'),phase:p<.18?'windup':p<.62?'attack':'recover',face:P.bossRoar.faceAnchors[key][attack?'attack':'idle']};}
function circleWave(ctx,x,y,r,alpha){ctx.save();ctx.globalAlpha=alpha;const points=Array.from({length:65},(_,i)=>[x+Math.cos(i*Math.PI/32)*r,y+Math.sin(i*Math.PI/32)*r]);line(ctx,points,C.dark,8);line(ctx,points,C.gold,4);line(ctx,points,C.cream,2);ctx.restore()}
function drawBossRoar(ctx,key,t,cam){const holding=t>=cam.inEnd&&t<cam.holdEnd,p=clamp((t-cam.inEnd)/(cam.holdEnd-cam.inEnd)),pose=roarPose(key,holding?p:0);sprite(ctx,holding?pose.sprite:key==='mini'?'gate_03':'gate_04',900,590,key==='mini'?3.2:4);
 if(holding){for(const emit of [0,.24,.52]){const age=(p-emit)/P.bossRoar.pulseLifetimeFraction;if(age>=0&&age<=1){const center=roarPose(key,emit).face;circleWave(ctx,center[0],center[1],mix(24,108,age),1-age)}}}
 return{attackCycles:1,damageEvents:0,face:pose.face,phase:holding?pose.phase:'idle'};
}
function heroMotion(ctx,state,time,dur=600){const p=clamp(time/dur);let pose='on_00',x=500,foot=590,alpha=1,vanish=0;
 if(state==='run')return drawRun(ctx,x,foot,time);
 if(state==='idle')pose=Math.floor(time/300)%2?'on_01':'on_00';
 if(state==='attack'){pose=time<100?'on_02':time<280?'on_03':'on_00';x+=80*(time<100?ease(time/100):1-ease((time-140)/140))}
 const dodgeBlend=time<60?ease(time/60):time<180?1:1-ease((time-180)/Math.max(1,dur-180));
 if(state==='dodge_up'){pose='on_04';foot-=144*dodgeBlend}
 if(state==='dodge_down'){pose=p<.08||p>.95?'on_00':'on_05'}
 if(state==='dodge_back'){pose='on_06';x-=70*dodgeBlend}
 if(state==='dodge_forward'){pose='on_07';x+=100*dodgeBlend}
 if(state==='hit'){pose='on_06';x-=16*Math.sin(p*Math.PI);alpha=Math.floor(time/45)%2?.45:1}
 if(state==='death'){vanish=p;alpha=1}
 const box=sprite(ctx,pose,x,foot,1.1,alpha,vanish);if(state==='death')debris(ctx,box,p);return{rootX:x,pose};
}
function motionFrame(canvas,actorId='hero_on',state='run',time=200,dur=600){const ctx=canvas.getContext('2d');clear(ctx);background(ctx);const selected=P.actors.find(a=>a.id===actorId);if(actorId==='hero_on'){heroMotion(ctx,state,time,dur);sprite(ctx,'gate_00',900,590,2)}else{sprite(ctx,'on_00',500,590,1.1);const n=P.actors.findIndex(a=>a.id===actorId)-1,base='gate_'+String(n).padStart(2,'0'),attack='gate_'+String(n+5).padStart(2,'0'),stun='gate_'+String(n+10).padStart(2,'0'),p=clamp(time/dur);let pose=base,foot=590,alpha=1,vanish=0;
 if(state==='roar'){const key=actorId==='gate_warden'?'mini':'boss';drawBossRoar(ctx,key,time,{inEnd:0,holdEnd:dur});}
 else{if(state.startsWith('windup_'))pose=p>.18?attack:base;if(state.startsWith('attack_'))pose=attack;if(state==='recover')pose=p<.6?attack:base;if(state==='stun')pose=stun;if(state==='hit'){pose=stun;alpha=Math.floor(time/40)%2?.45:1}if(state==='death')vanish=p;if(state==='idle')foot-=Math.round(Math.sin(p*Math.PI*2)*3);if(state==='enter')foot-=Math.abs(Math.sin(p*Math.PI*4))*4;
 const box=sprite(ctx,pose,900,foot,selected.proposed,alpha,vanish);const direction={low:'low_sweep',high:'high_sweep',front:'thrust',rear:'outer_arc'}[state.split('_')[1]];if(state.startsWith('windup_'))dangerEffect(ctx,direction,Math.min(time,dur-1),dur);if(state.startsWith('attack_'))dangerEffect(ctx,direction,650+time,650);if(state==='death')debris(ctx,box,p);if(state==='stun'){for(let i=0;i<3;i++){const a=p*Math.PI*2+i*Math.PI*2/3;rect(ctx,900+Math.cos(a)*42,box.top-15+Math.sin(a)*8,6,6,C.gold)}}}
 }hud(ctx,{stage:actorId==='bell_knight'?10:actorId==='gate_warden'?5:1,boss:['bell_knight','gate_warden'].includes(actorId)});return{actorId,state,time,durationMs:dur};}
