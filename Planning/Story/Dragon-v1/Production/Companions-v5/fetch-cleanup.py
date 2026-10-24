from pathlib import Path
import urllib.request,json
from PIL import Image
B=Path(__file__).resolve().parent
rows=[('tori',3,'2b45d372-481c-4ba6-b9f4-22bc84aa60ef'),('rina',2,'e75a8f35-8dee-4220-b583-6c6f1d1391ae'),('rina',3,'c88faa56-e1c3-4193-89e1-60418d43a5c6'),('momo',3,'01c629ef-8e0c-4592-a52c-e4adea3a8ddc')]
for id,n,tool in rows:
 p=B/id/'frames/Attack'/f'{n:02d}.png';archive=B/id/'raw/cleanup-before';archive.mkdir(parents=True,exist_ok=True)
 if not (archive/p.name).exists():(archive/p.name).write_bytes(p.read_bytes())
 url=f'https://api.pixellab.ai/mcp/pixel-tools/{tool}/image.png'
 with urllib.request.urlopen(url,timeout=60) as r:data=r.read()
 p.write_bytes(data);assert Image.open(p).size==(32,32)
(B/'requests/cleanup.json').write_text(json.dumps([dict(id=id,attackFrame=n,toolImage=tool,reason='Remove only1–2detached projectile pixels; runtimeownsprojectiles') for id,n,tool in rows],indent=2)+'\n',encoding='utf-8')
print('Four frames cleaned with PixelLab guarded pixel patches.')
