'use strict';
(() => {
 const data=window.MONSTER_MOTIONS,images=new Map(),whiteImages=new Map();
 const motionNames={Idle:'대기',Attack:'공격',Hit:'피격 · 흰색 점멸',Stun:'기절',AttackLow:'하단 공격',AttackHigh:'상단 공격',AttackThrust:'찌르기',AttackRear:'후방 휘두르기'};
 const getActor=id=>data.actors.find(a=>a.id===id),getClip=(a,m)=>m==='Idle'?a?.baseClip:a?.clips.find(c=>c.motion===m)||(m.startsWith('Attack')?a?.clips.find(c=>c.motion==='Attack'):null)||a?.baseClip;
 function getImage(src){if(!images.has(src)){const im=new Image();im.src=src;images.set(src,im);}return images.get(src);}
 for(const a of data.actors)for(const c of [a.baseClip,...a.clips])for(const src of c.frames)getImage(src);
 const authored=c=>c.frameDurationsMs?.length===c.frames.length&&c.frameDurationsMs.every(n=>n>0);
 const duration=c=>c.durationMs||(authored(c)?c.frameDurationsMs.reduce((a,b)=>a+b,0):c.frames.length/c.fps*1000);
 const frameStart=(c,i)=>authored(c)?c.frameDurationsMs.slice(0,i).reduce((a,b)=>a+b,0):i/c.fps*1000;
 function frameIndex(c,ms,repeat=c.loop){const d=duration(c),t=repeat?((ms%d)+d)%d:Math.max(0,ms);if(authored(c)){let end=0;for(let i=0;i<c.frames.length;i++){end+=c.frameDurationsMs[i];if(t<end)return i;}return c.frames.length-1;}return Math.min(c.frames.length-1,Math.floor(t/d*c.frames.length));}
 function flashAmount(ms){return ms<0?0:ms<=45?1:Math.max(0,1-(ms-45)/75);}
 function draw(ctx,id,motion,ms,x,y,height,index,hitAge=-1){const a=getActor(id),c=getClip(a,motion);if(!c)return false;const f=index??frameIndex(c,ms),src=c.frames[Math.max(0,Math.min(c.frames.length-1,f))],im=getImage(src);if(!im.complete||!im.naturalWidth)return false;
  const crop=c.crop||[0,0,im.naturalWidth,im.naturalHeight],pivot=c.pivot||a.pivot,scale=height/(c.referenceHeightPixels||a.referenceHeightPixels);
  const dest=[Math.round(x-pivot[0]*scale),Math.round(y-pivot[1]*scale),Math.round(crop[2]*scale),Math.round(crop[3]*scale)];ctx.imageSmoothingEnabled=false;
  const white=flashAmount(hitAge>=0?hitAge:c.effect==='whiteFlash'?ms:-1);
  if(white>0){if(!whiteImages.has(src)){const mask=document.createElement('canvas');mask.width=im.naturalWidth;mask.height=im.naturalHeight;whiteImages.set(src,mask);}const mask=whiteImages.get(src),m=mask.getContext('2d');m.globalAlpha=1;m.globalCompositeOperation='source-over';m.clearRect(0,0,mask.width,mask.height);m.drawImage(im,0,0);m.globalCompositeOperation='source-atop';m.globalAlpha=white;m.fillStyle='#fff';m.fillRect(0,0,mask.width,mask.height);ctx.drawImage(mask,...crop,...dest);}else ctx.drawImage(im,...crop,...dest);return true;
 }
 window.MonsterSprites={getActor,getClip,frameIndex,frameStart,duration,flashAmount,draw};
 const $=id=>document.getElementById(id),canvas=$('monster-canvas');if(!canvas)return;
 const ctx=canvas.getContext('2d');let actor=data.actors[0],clip=actor.clips[0],time=0,last=performance.now(),playing=true,hitAge=-1,soundCycle=-1;
 $('monster-select').innerHTML=['original','candidate'].map(group=>`<optgroup label="${group==='original'?'기존 몬스터 · 인게임 사용':'신규 후보 · 인게임 미적용'}">${data.actors.filter(a=>a.group===group).map(a=>`<option value="${a.id}">${a.name}</option>`).join('')}</optgroup>`).join('');
 function sync(){ $('motion-select').innerHTML=actor.clips.map(c=>`<option value="${c.motion}">${motionNames[c.motion]}${c.pending?' · 새로 제작 중':c.effect?' · 120ms':c.staticFallback?' · 기존 고정 자세':' · '+c.frames.length+'프레임'}</option>`).join('');clip=actor.clips[0];reset();}
 function reset(){time=0;hitAge=-1;soundCycle=-1;window.LastBellAudio?.stopSfx();if(clip.effect)window.LastBellAudio?.play('hit-sharp');last=performance.now();$('motion-frame').max=clip.effect?53:clip.frames.length-1;$('motion-frame').value=0;playing=true;$('motion-play').textContent='일시정지';}
 $('monster-select').addEventListener('change',()=>{actor=getActor($('monster-select').value);sync();});
 $('motion-select').addEventListener('change',()=>{clip=getClip(actor,$('motion-select').value);reset();});
 $('motion-play').addEventListener('click',()=>{playing=!playing;if(!playing)window.LastBellAudio?.stopSfx();last=performance.now();$('motion-play').textContent=playing?'일시정지':'재생';});
 $('motion-restart').addEventListener('click',reset);$('motion-hit').addEventListener('click',()=>{hitAge=0;window.LastBellAudio?.play('hit-sharp');});
 $('motion-frame').addEventListener('input',()=>{playing=false;time=(clip.effect?Number($('motion-frame').value)/60*1000:frameStart(clip,Number($('motion-frame').value)))+.01;hitAge=-1;$('motion-play').textContent='재생';});
 function tick(now){const dt=(now-last)*Number($('motion-speed').value);if(playing)time+=dt;if(hitAge>=0)hitAge+=dt;last=now;const d=duration(clip),repeat=$('motion-repeat').checked,t=repeat?time%(d+(clip.motion==='Attack'?400:0)):time,f=frameIndex(clip,t,false),rest=repeat&&clip.motion==='Attack'&&t>=d;$('motion-frame').value=clip.effect?Math.min(53,Math.floor(t/1000*60)):f;
  const cycle=repeat?Math.floor(time/(d+400)):0;
  const visible=canvas.getBoundingClientRect();
  if(playing&&clip.motion==='Attack'&&!rest&&t<d&&t>=frameStart(clip,clip.contactFrame)&&soundCycle!==cycle){soundCycle=cycle;if(visible.bottom>0&&visible.top<innerHeight)window.LastBellAudio?.play('sword-clash');}
  ctx.fillStyle='#213b59';ctx.fillRect(0,0,960,540);ctx.fillStyle='#587083';ctx.fillRect(0,442,960,14);ctx.fillStyle='#0b1928';ctx.fillRect(0,456,960,84);ctx.strokeStyle='#94b5b077';ctx.setLineDash([5,6]);ctx.beginPath();ctx.moveTo(640,100);ctx.lineTo(640,458);ctx.stroke();ctx.setLineDash([]);
  draw(ctx,actor.id,rest?'Idle':clip.motion,t,640,442,actor.height/6.2*540,rest?0:f,hitAge);
  ctx.fillStyle='#efdbb7';ctx.textAlign='left';ctx.font='26px Nanum, sans-serif';ctx.fillText(actor.name,32,48);ctx.font='18px Nanum, sans-serif';ctx.fillText(motionNames[clip.motion]+' · '+(clip.effect?'흰색 → 원래 색':(f+1)+' / '+clip.frames.length),32,82);
  ctx.fillStyle='#a6ded1';ctx.fillText(actor.group==='original'?'기존 원본 · 인게임 사용':actor.status==='pending'?'신규 모션 제작·검수 중 · 기본 이미지':'신규 후보 · 인게임 미적용',32,116);ctx.font='16px Nanum, sans-serif';ctx.fillText('피격 확인: 현재 동작을 유지한 채 흰색으로 점멸합니다.',32,500);
  $('motion-info').textContent=clip.pending?'기존 생성 모션은 제외했습니다. 새 공격·기절 모션 검수가 끝나면 연결합니다.':`${actor.id} / ${clip.motion} · ${clip.effect?'45ms 흰색 유지 + 75ms 복귀':Math.round(d)+'ms'} · ${rest?'다음 재생 대기':clip.staticFallback&&!clip.effect?'기존 고정 자세':clip.loop?'순환 동작':'단발 동작'}${!rest&&f===clip.contactFrame?' · 명중 프레임':''}`;
  requestAnimationFrame(tick);
 }
 sync();requestAnimationFrame(tick);
})();
