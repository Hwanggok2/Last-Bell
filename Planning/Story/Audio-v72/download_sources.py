"""Fetch the publicly offered HQ previews and retain their exact license evidence."""
from pathlib import Path
import hashlib, json, re, urllib.request

HERE=Path(__file__).resolve().parent
OUT=HERE/'Sources'; OUT.mkdir(exist_ok=True)
sources=[
    dict(id='649361',role='attack',title='Sword_Fire01.wav',creator='SonoFxAudio',user='SonoFxAudio',license='CC-BY-4.0',licenseUrl='https://creativecommons.org/licenses/by/4.0/'),
    dict(id='60030',role='dodge',title='Swosh / Whoosh / Air Cut',creator='qubodup',user='qubodup',license='CC0-1.0',licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/'),
    dict(id='540752',role='hit',title='Impact: Smack Body',creator='Philipp Grzemba / Sheyvan',user='Sheyvan',license='CC0-1.0',licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/'),
]
def fetch(url,path):
    req=urllib.request.Request(url,headers={'User-Agent':'LastBell-Audio-License-Archive/1.0'})
    with urllib.request.urlopen(req,timeout=45) as response:path.write_bytes(response.read())
for item in sources:
    item['sourceUrl']=f"https://freesound.org/people/{item['user']}/sounds/{item['id']}/"
    page=OUT/(item['id']+'-source.html')
    if not page.exists():fetch(item['sourceUrl'],page)
    content=page.read_text(encoding='utf-8')
    assert item['licenseUrl'] in content.replace('http://creativecommons.org/','https://creativecommons.org/'), 'License mismatch: '+item['id']
    matches=re.findall(r'https://cdn\.freesound\.org/previews/[^\s\"<>]+-hq\.mp3',content)
    assert matches,'No publicly offered HQ preview: '+item['id']
    item['downloadUrl']=matches[0];item['downloadKind']='Public HQ MP3 preview, not the original WAV/FLAC'
    item['downloadedFile']=item['id']+'-hq.mp3';target=OUT/item['downloadedFile']
    if not target.exists():fetch(item['downloadUrl'],target)
    item['sha256']=hashlib.sha256(target.read_bytes()).hexdigest();item['checkedAt']='2026-10-23'
    print(item['role'],item['license'],target.stat().st_size)
for name,url in [('CC-BY-4.0.txt','https://creativecommons.org/licenses/by/4.0/legalcode.txt'),('CC0-1.0.txt','https://creativecommons.org/publicdomain/zero/1.0/legalcode.txt')]:
    fetch(url,OUT/name)
(HERE/'sources.json').write_text(json.dumps(sources,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
