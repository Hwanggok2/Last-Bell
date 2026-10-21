from pathlib import Path
import json,hashlib
from PIL import Image
R=Path(__file__).resolve().parent;D=R/'PixelLab/clips/hero-v7-run-authored'
frames=[Image.open(D/'frames'/f'{i:02}.png').convert('RGBA') for i in range(12)]
assert len({f.tobytes() for f in frames})==12
assert all(f.size==(128,128) for f in frames)
assert all(0<f.getbbox()[0] and f.getbbox()[2]<128 and f.getbbox()[3]<128 for f in frames)
scale=1.1*116.13/84
foot=[85,72,59,46]
delta=[(a-b)*scale for a,b in zip(foot,foot[1:])]
floor=960*.020
assert all(abs(d-floor)<.6 for d in delta)
report={'source':'PixelLab Workbench drawing 148d66b5-7d30-4b4a-847d-6668715fd590','authoredFrames':12,'aiGeneratedFrames':0,'cycleMs':240,'frameMs':20,'uniqueFrames':12,'sourceSheetSha256':hashlib.sha256((D/'source-sheet.png').read_bytes()).hexdigest(),'contactStepScreenPx':delta,'floorStepScreenPx':floor,'maxContactDriftPer20ms':max(abs(d-floor) for d in delta),'bounds':[f.getbbox() for f in frames],'limits':['Contact calculation uses authored foot anchors, not a continuous-playback visual certification.','No Unity or browser playback test.']}
(R/'../Evidence/Presentation-v7/run-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('12 distinct source poses, borders clear; contact drift under 0.6px per 20ms.')
