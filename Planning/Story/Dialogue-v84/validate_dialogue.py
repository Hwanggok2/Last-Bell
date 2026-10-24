"""Text/schema/link checks and real-font measurement; no Unity mutation."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import unquote
import json
from PIL import ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
patch = json.loads((OUT / 'dialogue-patch.json').read_text(encoding='utf-8'))
before = json.loads((OUT / 'before.json').read_text(encoding='utf-8'))
assert len(patch['onboarding']['prologueParagraphs']) == 4
assert [(s['id'], s['stage'], s['bossId']) for s in patch['stories']] == [
    (s['id'], s['stage'], s['bossId']) for s in before['stories']]
assert all(len(s['lines']) == 3 for s in patch['stories'])
texts = [line['text'] for s in patch['stories'] for line in s['lines']]
texts += [r['text'] for s in patch['stories'] for line in s['lines'] for r in line['responses']]
assert all(len(t.split('\n')) == 2 for t in texts + patch['onboarding']['prologueParagraphs'])
assert max(len(line) for t in texts for line in t.split('\n')) <= 22
font_path = ROOT / 'Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf'
measurements = []
for width, height in [(960, 540), (640, 360)]:
    font = ImageFont.truetype(str(font_path), round(24 * width / 960))
    for kind, paragraphs, available in [('prologue', patch['onboarding']['prologueParagraphs'], 504 * width / 960), ('story', texts, .75 * width)]:
        widths = [font.getlength(line) for text in paragraphs for line in text.split('\n')]
        result = {'kind': kind, 'resolution': [width, height], 'fontSize': font.size,
                  'availableWidth': available, 'maxTextWidth': max(widths), 'fits': max(widths) <= available}
        assert result['fits'], result
        measurements.append(result)

class Links(HTMLParser):
    def __init__(self):
        super().__init__()
        self.paths = []
    def handle_starttag(self, tag, attrs):
        for key, value in attrs:
            if key in ('src', 'href') and value and not value.startswith(('#', 'http:', 'https:', 'data:')):
                self.paths.append(value)

parser = Links()
parser.feed((OUT / 'index.html').read_text(encoding='utf-8'))
for url in parser.paths:
    assert (OUT / unquote(url.split('#')[0])).resolve().exists(), url
review = patch['review']
review['glyphWidthMeasurement'] = {'font': font_path.name,
    'method': 'Pillow FreeType bounding measurement; conservative 24px at 960, 16px at 640; not a Unity TextGenerator result',
    'results': measurements}
review['checks']['all_html_local_links_resolve'] = True
(OUT / 'dialogue-patch.json').write_text(json.dumps(patch, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
(OUT / 'review.json').write_text(json.dumps(review, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'pass': True, 'localLinks': len(parser.paths), 'fontWidthChecks': measurements}))
