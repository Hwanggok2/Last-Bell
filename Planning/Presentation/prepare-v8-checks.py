from pathlib import Path
R=Path(__file__).resolve().parent
for name in ['render','check-controls','encode','verify']:
    src={'render':'render-v7-offline.mjs','check-controls':'check-v7-controls.mjs','encode':'encode-v7-previews.py','verify':'verify-v7-previews.py'}[name]
    dst=src.replace('v7','v8')
    s=(R/src).read_text(encoding='utf-8').replace('Presentation-v7','Presentation-v8').replace('effects-v7.js','effects-v8.js').replace('motions-v7.js','motions-v8.js')
    if name=='render':
        s=s.replace("Array.from({length:50},(_,i)=>i*19.2)","Array.from({length:100},(_,i)=>i*9.6)")
        s=s.replace('[0,40,80,120,160,200]','[0,80,160,240,320,400]').replace('t*.96','t*.48').replace('i<=90','i<=130').replace('1초 질주','2초 이동').replace('[550,1050,1550,1810,2030,2250]','[550,1550,2550,2810,3030,3250]')
    elif name=='check-controls':
        s=s.replace('callback(1000);assert.equal(+ids.travel.value,960)','callback(2000);assert.equal(+ids.travel.value,960)').replace('(r.t-550)/1000','(r.t-550)/2000').replace("duration(\"normal\")',ctx),2250","duration(\"normal\")',ctx),3250")
    elif name=='verify':s=s.replace("('run',1000)","('run',2000)").replace("('transition',2650)","('transition',3650)")
    (R/dst).write_text(s,encoding='utf-8')
print('Prepared v8 render and timing verification.')
