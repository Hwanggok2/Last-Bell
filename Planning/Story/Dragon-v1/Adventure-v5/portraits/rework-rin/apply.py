"""Replace only Miel. Preserve all other eight native24px portrait files."""
from pathlib import Path
import hashlib,json,shutil,runpy
from PIL import Image,ImageDraw
B=Path(__file__).resolve().parent;P=B.parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
others={p.name:sha(p) for p in P.glob('*.png') if p.stem in ['on','dam','tori','rina','barun','doto','gib','unknown']}
archive=B/'archive';archive.mkdir(exist_ok=True)
for source,name in [(P/'rin.png','rin-before.png'),(P/'raw/rin.png','raw-before.png'),(P/'manifest.json','manifest-before.json')]:
 if not (archive/name).exists():shutil.copy2(source,archive/name)
im=Image.open(B/'canonical.png').convert('RGBA');colors={p[:3] for p in im.getdata() if p[3]}
assert im.size==(24,24) and len(colors)<=8 and {p[3] for p in im.getdata()}=={0,255}
shutil.copy2(B/'canonical.png',P/'raw/rin.png');shutil.copy2(B/'canonical.png',P/'rin.png')
runpy.run_path(str(P/'package.py'),run_name='__main__')
assert all(sha(P/name)==h for name,h in others.items())
(B/'validation.json').write_text(json.dumps({'status':'passed','onlyPortraitReplaced':'rin','nativeSize':[24,24],'colorCount':len(colors),'binaryAlpha':True,'otherEightUnchangedHashes':others,'generationJob':'a1d9ff79-9145-4dce-8c70-2e1379d08a32','review':'wide clean teal hat; symmetric visible eyes; blonde locks; teal cloak; no frame'},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
sheet=Image.new('RGB',(720,280),'#DBCFAE');d=ImageDraw.Draw(sheet)
for i,(title,path) in enumerate([('Approved identity 48px',P.parents[1]/'Production/Portraits-v1/rin.png'),('Before 24px',archive/'rin-before.png'),('Reworked native24px',P/'rin.png')]):
 source=Image.open(path).convert('RGBA').resize((192,192),Image.Resampling.NEAREST);sheet.paste(source,(i*240+24,40),source);d.text((i*240+12,14),title,fill='#202D38')
sheet.save(B/'comparison.png')
print('Miel reworked; eight other portraits unchanged.')
