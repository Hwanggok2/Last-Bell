/* Browser-only art/sequence study. No combat logic or Unity assets are changed. */
const P=window.PROPOSAL,A=window.ATLAS,$=id=>document.getElementById(id);
const C={dark:'#0c172b',old:'#ac7833',gold:'#dca947',cream:'#fff0b4',line:'#30465f'};
const images={},paths={hero:'../../Client/Assets/Game/Art/Source/on-poses-v1.png',enemy:'../../Client/Assets/Game/Art/Source/gate-enemies-v1.png',bg:'../../Client/Assets/Game/Art/Source/gate-background-v1.png',lobby:'../Evidence/Unity/Smoke/09-lobby.png'};
const clamp=(x,a=0,b=1)=>Math.max(a,Math.min(b,x)),mix=(a,b,t)=>a+(b-a)*clamp(t),ease=t=>{t=clamp(t);return t*t*(3-2*t)},px=x=>Math.round(x/2)*2;
function clear(ctx){ctx.setTransform(1,0,0,1,0,0);ctx.clearRect(0,0,1280,720);ctx.imageSmoothingEnabled=false}
function rect(ctx,x,y,w,h,color){ctx.fillStyle=color;ctx.fillRect(px(x),px(y),Math.max(1,px(w)),Math.max(1,px(h)))}
function text(ctx,s,x,y,size=24,color=C.cream,align='left'){ctx.fillStyle=color;ctx.font=`${size}px Bell, sans-serif`;ctx.textAlign=align;ctx.fillText(s,x,y)}
function background(ctx,offset=0,wire=false){
 if(wire){rect(ctx,0,0,1280,720,'#111821');rect(ctx,0,600,1280,120,'#27313e');ctx.strokeStyle='#546273';ctx.lineWidth=1;for(let x=0;x<1280;x+=80){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,720);ctx.stroke()}for(let y=40;y<720;y+=80){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(1280,y);ctx.stroke()}return}
 ctx.drawImage(images.bg,-px(offset),0,1280,720);if(offset>0)ctx.drawImage(images.bg,1280-px(offset),0,1280,720);
 // The world art's ground is slightly above the conceptual foot baseline; a stone lip joins it.
 rect(ctx,0,596,1280,8,'#192b43');for(let x=-offset%48;x<1280;x+=48){rect(ctx,x,596,42,4,'#405a7a')}
}
function sprite(ctx,name,x,foot,height,alpha=1,vanish=0,wire=false){
 const a=A[name],idle=name.startsWith('gate_')?A['gate_'+String(Number(name.slice(5))%5).padStart(2,'0')]:A.on_00,h=height*116.13*a.h/idle.h,w=h*a.w/a.h,left=x-w*a.px,top=foot-h;
 ctx.save();ctx.globalAlpha=clamp(alpha);
 if(wire){rect(ctx,left,top,w,h,'#34465b');ctx.strokeStyle='#c5cdd6';ctx.lineWidth=2;ctx.strokeRect(left,top,w,h);ctx.restore();return {left,top,w,h}}
 if(vanish>0){ctx.beginPath();for(let row=0;row<Math.ceil(h/8);row++)for(let col=0;col<Math.ceil(w/8);col++){const rand=((row*73+col*31+row*col*13)%101)/101;if(rand>vanish)ctx.rect(left+col*8,top+row*8,8,8)}ctx.clip()}
 ctx.drawImage(images[a.sheet],a.x,a.y,a.w,a.h,px(left),px(top),px(w),px(h));ctx.restore();return {left,top,w,h};
}
function debris(ctx,box,p,reduced=false){if(p<=0||p>=1.5)return;for(let i=0;i<(reduced?18:36);i++){const u=((i*37)%101)/101,v=((i*61)%97)/97,delay=u*.55,k=clamp((p-delay)/.8);if(k<=0)continue;ctx.globalAlpha=1-k;const x=box.left+u*box.w+(u-.5)*k*65,y=box.top+v*box.h+100*k*k;rect(ctx,x,y,i%3===0?8:4,i%3===0?8:4,[C.cream,C.gold,'#23344c'][i%3])}ctx.globalAlpha=1}
function meter(ctx,x,y,w,h,p,fill=C.gold){rect(ctx,x,y,w,h,'#15283b');rect(ctx,x,y,w*p,h,fill)}
function bell(ctx,x,y,color=C.cream){rect(ctx,x-6,y-13,12,5,color);rect(ctx,x-10,y-8,20,19,color);rect(ctx,x-14,y+9,28,5,color);rect(ctx,x-3,y+16,6,4,color)}
function controls(ctx,alpha=1){ctx.save();ctx.globalAlpha=alpha;[[200,421,0],[93,528,1],[200,635,2],[307,528,3]].forEach(([x,y,d])=>{ctx.save();ctx.translate(x,y);ctx.rotate(d*Math.PI/2-Math.PI*(d===1?1:0)+Math.PI*(d===3?1:0));ctx.fillStyle='#f0d59d';ctx.beginPath();ctx.moveTo(0,-23);ctx.lineTo(24,23);ctx.lineTo(-24,23);ctx.closePath();ctx.fill();ctx.restore()});rect(ctx,1118,566,133,117,'#213b50');ctx.save();ctx.translate(1184,625);ctx.rotate(Math.PI/4);rect(ctx,-4,-38,8,65,'#f0d59d');rect(ctx,-18,20,36,6,'#f0d59d');rect(ctx,-4,25,8,17,'#f0d59d');ctx.restore();ctx.restore()}
function hud(ctx,{stage=3,boss=false,bossAlpha=1,active=true,showBell=true,enemyHp=true,playerHp=true,showStage=true}={}){if(showStage)text(ctx,'Stage '+stage,12,57,32);rect(ctx,1168,16,96,96,'#21394c');rect(ctx,1205,51,6,30,C.cream);rect(ctx,1221,51,6,30,C.cream);if(showBell){bell(ctx,292,95);meter(ctx,326,88,627,18,.35)}if(boss){ctx.save();ctx.globalAlpha=bossAlpha;meter(ctx,213,36,928,21,1,'#f0d59d');ctx.restore()}if(playerHp)meter(ctx,447,608,106,10,.8,'#f0d59d');if(enemyHp&&!boss)meter(ctx,847,608,106,10,.68);controls(ctx,active?1:.28)}
function world(ctx,{enemy='gate_02',enemyHeight=2.0,heroX=500,enemyX=900,heroAlpha=1,enemyAlpha=1,heroVanish=0,enemyVanish=0,run=0,offset=0,wire=false}={}){background(ctx,offset,wire);const hero=sprite(ctx,'on_00',heroX,590-Math.abs(Math.sin(run*22))*5,1.1,heroAlpha,heroVanish,wire),monster=sprite(ctx,enemy,enemyX,590,enemyHeight,enemyAlpha,enemyVanish,wire);return{hero,monster}}
