from pathlib import Path
import json
R=Path(__file__).resolve().parent
e=json.loads((R/'PixelLab/edits.json').read_text('utf-8'))
e['hero-v7-run-rig']['status']='rejected'
e['hero-v7-run-rig']['note']='반려: 검 소실, 약한 전경, 비율 변화와 긴 체공. 신규 후보로 재제작.'
(R/'PixelLab/edits.json').write_text(json.dumps(e,ensure_ascii=False,indent=2),encoding='utf-8')
s=(R/'render-v6-offline.mjs').read_text('utf-8').replace('Presentation-v6','Presentation-v7').replace('effects-v6.js','effects-v7.js').replace('motions-v6.js','motions-v7.js')
s=s.replace("'PixelLab/assets.js','effects-v7.js'","'PixelLab/assets.js','hit-flash-v7.js','stage-banner-v7.js','effects-v7.js'")
s=s.replace("const sources=", "ctx.canvasFactory=createCanvas;vm.runInContext('HitFlashV7.setCanvasFactory(canvasFactory)',ctx);\nconst sources=")
s=s.replace("Array.from({length:60},(_,i)=>i*8)","Array.from({length:50},(_,i)=>i*19.2)")
s=s.replace("[0,75,150,225,300,375]","[0,40,80,120,160,200]").replace('t*.4,false','t*.96,false')
s=s.replace("const missing=[];", """const transitionDir=path.join(out,'transition');fs.mkdirSync(transitionDir,{recursive:true});
for(let i=0;i<=90;i++){const c=render('sequenceFrame',['normal',i*25,false,false]);fs.writeFileSync(path.join(transitionDir,String(i).padStart(3,'0')+'.png'),c.toBuffer('image/png'))}
contact('1초 질주 → 새 적 → Stage 2', [550,1050,1550,1810,2030,2250].map(t=>({c:render('sequenceFrame',['normal',t]),label:t+'ms'})),'transition-contact');
const flashDir=path.join(out,'hit-flash');fs.mkdirSync(flashDir,{recursive:true});
for(let i=0;i<50;i++){const t=i<25?i*20:(i-25)*20,c=i<25?render('playerFrame',[t,false]):render('warningFrame',['high_sweep',500+t,650,false]);fs.writeFileSync(path.join(flashDir,String(i).padStart(3,'0')+'.png'),c.toBuffer('image/png'))}
const missing=[];""")
(R/'render-v7-offline.mjs').write_text(s,encoding='utf-8')
s=(R/'check-v6-controls.mjs').read_text('utf-8').replace('Presentation-v6','Presentation-v7').replace('effects-v6.js','effects-v7.js').replace('motions-v6.js','motions-v7.js')
s=s.replace("'PixelLab/assets.js','effects-v7.js'","'PixelLab/assets.js','hit-flash-v7.js','stage-banner-v7.js','effects-v7.js'")
s=s.replace('callback(2400)','callback(3000)').replace(',2200)',',2700)')
s=s.replace('callback(1200);assert.equal(+ids.travel.value,480)','callback(1000);assert.equal(+ids.travel.value,960)')
s=s.replace('l=>480*l.factor','l=>960*l.factor').replace('[9.6,38.4,105.6,230.39999999999998,480]','[19.2,76.8,211.2,460.79999999999995,960]')
s=s.replace("r.name==='gate_00'","r.name==='gate_01'").replace('1380-480*Math.min(1,(r.t-550)/1200)','1860-960*Math.min(1,(r.t-550)/1000)')
s=s.replace("const roots=", """assert.equal(vm.runInContext('duration("normal")',ctx),2250);
for(const d of ['up','down','front','back']){ctx.d=d;assert.equal(vm.runInContext('selectedDodge(d)',ctx),'hero-dodge-'+d)}
for(const [age,alpha]of [[-1,0],[0,1],[45,1],[82.5,.5],[120,0]]){ctx.age=age;assert.equal(vm.runInContext('HitFlashV7.opacity(age)',ctx),alpha)}
const roots=""")
(R/'check-v7-controls.mjs').write_text(s,encoding='utf-8')
s=(R/'encode-v6-previews.py').read_text('utf-8').replace('Presentation-v6','Presentation-v7')
s += "\nfor kind,ms in [('transition',25),('hit-flash',20)]:\n    frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/kind).glob('*.png'))]\n    save(frames,R/f'{kind}.gif',[ms]*(len(frames)-1)+[400])\n"
(R/'encode-v7-previews.py').write_text(s,encoding='utf-8')
print('Prepared v7 offline renderer and control checks.')
