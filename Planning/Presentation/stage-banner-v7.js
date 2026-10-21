/* A display-only Stage label assembled from deterministic square pixels. */
const STAGE_GLYPHS={
 S:['01111','10000','10000','01110','00001','00001','11110'],
 t:['0100','0100','1111','0100','0100','0100','0011'],
 a:['00000','00000','01110','00001','01111','10001','01111'],
 g:['00000','01111','10001','10001','01111','00001','01110'],
 e:['00000','00000','01110','10001','11111','10000','01111'],
 ' ':['000','000','000','000','000','000','000'],
 '0':['01110','10001','10011','10101','11001','10001','01110'],
 '1':['00100','01100','00100','00100','00100','00100','01110'],
 '2':['01110','10001','00001','00010','00100','01000','11111'],
 '3':['11110','00001','00001','01110','00001','00001','11110'],
 '4':['00010','00110','01010','10010','11111','00010','00010'],
 '5':['11111','10000','10000','11110','00001','00001','11110'],
 '6':['01110','10000','10000','11110','10001','10001','01110'],
 '7':['11111','00001','00010','00100','01000','01000','01000'],
 '8':['01110','10001','10001','01110','10001','10001','01110'],
 '9':['01110','10001','10001','01111','00001','00001','01110']
};
function stageBannerPixels(stage){let column=0;const dots=[];for(const ch of 'Stage '+stage){const glyph=STAGE_GLYPHS[ch];glyph.forEach((row,y)=>[...row].forEach((bit,x)=>{if(bit==='1')dots.push([column+x,y])}));column+=glyph[0].length+1}return{dots,columns:column-1}}
function drawStageBanner(ctx,stage,age,reduced=false){
 const cfg=P.stageBanner;if(age<0||age>=cfg.totalMs)return{visible:false};
 const {dots,columns}=stageBannerPixels(stage),unit=12,left=640-columns*unit/2,top=135;
 const fade=age<cfg.fadeStartMs?1:1-clamp((age-cfg.fadeStartMs)/(cfg.totalMs-cfg.fadeStartMs));
 ctx.save();ctx.globalAlpha=fade;
 for(let i=0;i<dots.length;i++){const [col,row]=dots[i],delay=(i*47%101),q=reduced?1:clamp((age-delay)/(cfg.assembleMs-100)),e=1-Math.pow(1-q,3);if(!reduced&&age<delay)continue;
  const dx=((i*59%17)-8)*18,dy=((i*31%11)-5)*14,x=Math.round(left+col*unit+dx*(1-e)),y=Math.round(top+row*unit+dy*(1-e));
  ctx.fillStyle='#07111f';ctx.fillRect(x-2,y-2,unit+3,unit+3);ctx.fillStyle='#fff0b4';ctx.fillRect(x,y,unit-1,unit-1);
 }
 ctx.restore();return{visible:true,stage,assembled:reduced||age>=cfg.assembleMs,pixels:dots.length};
}
