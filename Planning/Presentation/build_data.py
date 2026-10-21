"""Build browser-only proposal data from the plan and existing sprite metadata; no Unity writes."""
from pathlib import Path
import json
import re
import struct

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
assets = {}
for sheet, file in [('hero', 'on-poses-v1.png'), ('enemy', 'gate-enemies-v1.png')]:
    source = ROOT / 'Client/Assets/Game/Art/Source' / file
    width, height = struct.unpack('>II', source.read_bytes()[16:24])
    text = source.with_suffix('.png.meta').read_text('utf-8')
    pattern = r'name: ((?:on|gate)_\d+)\s+rect:\s+serializedVersion: \d+\s+x: ([\d.]+)\s+y: ([\d.]+)\s+width: ([\d.]+)\s+height: ([\d.]+)\s+alignment: \d+\s+pivot: \{x: ([\d.]+), y: ([\d.]+)\}'
    for match in re.finditer(pattern, text):
        name, *numbers = match.groups()
        x,y,w,h,px,py = map(float, numbers)
        assets[name] = dict(sheet=sheet,x=x,y=height-y-h,w=w,h=h,px=px,py=py)
    assert len([v for v in assets.values() if v['sheet']==sheet]) == (8 if sheet=='hero' else 15)
plan = json.loads((HERE/'proposal.json').read_text('utf-8'))
(HERE/'data.js').write_text('window.PROPOSAL = '+json.dumps(plan,ensure_ascii=False)+';\nwindow.ATLAS = '+json.dumps(assets)+';\n',encoding='utf-8')
print('Browser proposal and 23 existing sprite rects prepared; Unity untouched.')

motions = json.loads((HERE/plan.get("motionCatalog", "motion-catalog-v3.json")).read_text("utf-8"))
with (HERE/"data.js").open("a", encoding="utf-8") as target:
    target.write("window.MOTIONS = "+json.dumps(motions,ensure_ascii=False)+";\n")
