"""Collect publisher-provided CC0 comparison sources; no Unity asset changes."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urljoin, urlsplit, unquote
from urllib.request import Request, urlopen
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Planning/Story/Dragon-v1/Audio-v1'
SOURCES = [
    ('retro', 'SubspaceAudio / Juhani Junkala', 'https://opengameart.org/content/512-sound-effects-8-bit-style'),
    ('sword', 'StarNinjas', 'https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes'),
    ('swishes', 'artisticdude', 'https://opengameart.org/content/swishes-sound-pack'),
]

class Links(HTMLParser):
    def __init__(self, text):
        super().__init__(); self.hrefs = []; self.feed(text)
    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if tag == 'a' and 'href' in a:
            self.hrefs.append(a['href'])

def fetch(url, target):
    if not target.exists():
        data = urlopen(Request(url, headers={'User-Agent':'LastBellAudioResearch/1.0'}), timeout=60).read()
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
    return target.read_bytes()

def main():
    records = []
    for key, author, page in SOURCES:
        folder = OUT / 'Sources' / key
        source = fetch(page, folder / 'source.html').decode('utf-8')
        assert 'creativecommons.org/publicdomain/zero/1.0' in source, f'CC0 evidence missing: {page}'
        urls = list(dict.fromkeys(urljoin(page, x) for x in Links(source).hrefs if urlsplit(x).path.lower().endswith('.zip')))
        assert urls, page
        files = []
        for url in urls:
            name = Path(unquote(urlsplit(url).path)).name
            archive = folder / name
            data = fetch(url, archive)
            with zipfile.ZipFile(archive) as z:
                contents = [x.filename for x in z.infolist() if not x.is_dir()]
            files.append(dict(url=url, file=archive.relative_to(OUT).as_posix(), sha256=hashlib.sha256(data).hexdigest(), contents=contents))
        records.append(dict(id=key, author=author, sourceUrl=page, license='CC0-1.0', sourceEvidence=(folder/'source.html').relative_to(OUT).as_posix(), archives=files))
        print(key, [(Path(x['file']).name, len(x['contents'])) for x in files])
    (OUT/'sources.json').write_text(json.dumps(dict(checkedAt='2026-10-24', purpose='Planning audition candidates only; not imported into Unity', sources=records),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

if __name__ == '__main__':
    main()
