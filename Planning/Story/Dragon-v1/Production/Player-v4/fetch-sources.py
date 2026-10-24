"""Download only observed PixelLab output URLs; no artist edits or palette changes."""
from pathlib import Path
import json, urllib.request, hashlib, zipfile, io
from PIL import Image
BASE=Path(__file__).resolve().parent
def main():
    records=[]
    items=json.loads((BASE/'requests/source-queue.json').read_text(encoding='utf-8'))
    items+=json.loads((BASE.parent/'Portraits-v1/requests/final-portraits.json').read_text(encoding='utf-8'))
    items+=json.loads((BASE.parent/'Portraits-v1/requests/final-corrections.json').read_text(encoding='utf-8'))
    items+=json.loads((BASE.parent/'Portraits-v1/requests/gib-final-url.json').read_text(encoding='utf-8'))
    for item in items:
        dest=(BASE/item['id']/'canonical.png') if item['kind']=='canonical' else (BASE.parent/'Portraits-v1'/'raw'/(item['id']+'.png'))
        dest.parent.mkdir(parents=True,exist_ok=True)
        if not dest.exists():
            with urllib.request.urlopen(item['url'],timeout=90) as response: data=response.read()
            assert data[:8]==b'\x89PNG\r\n\x1a\n', (item,data[:40])
            dest.write_bytes(data)
        with Image.open(dest) as im:
            rgba=im.convert('RGBA');pixels=list(rgba.getdata())
            record=dict(item,path=str(dest),size=list(im.size),colors=len({p[:3] for p in pixels if p[3]}),alpha=sorted({p[3] for p in pixels}),sha256=hashlib.sha256(dest.read_bytes()).hexdigest())
            records.append(record)
    (BASE/'requests/downloaded.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
    print([(r['id'],r['size'],r['colors'],r['alpha']) for r in records])
if __name__=='__main__':main()
