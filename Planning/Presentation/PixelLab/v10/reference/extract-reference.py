"""Read-only Unity sprite trace and lossless PNG reference extraction."""
from pathlib import Path
import hashlib
import json
import math
import re
from PIL import Image, ImageDraw, ImageFont, ImageSequence

SOURCE = Path('C:/Github/SimpleGame')
OUT = Path(__file__).resolve().parent
PATHS = {
    'texture': 'Assets/SourceAssets/PixelLabCharacters/BladeMage/Character.png',
    'textureMeta': 'Assets/SourceAssets/PixelLabCharacters/BladeMage/Character.png.meta',
    'clip': 'Assets/Game/Characters/Animations/BladeMage/BladeMage_Move.anim',
    'clipMeta': 'Assets/Game/Characters/Animations/BladeMage/BladeMage_Move.anim.meta',
    'overrideController': 'Assets/Game/Characters/Animators/BladeMage.overrideController',
    'overrideMeta': 'Assets/Game/Characters/Animators/BladeMage.overrideController.meta',
    'baseController': 'Assets/Game/Characters/Animators/Player.controller',
    'baseControllerMeta': 'Assets/Game/Characters/Animators/Player.controller.meta',
}
raw = {key: (SOURCE / value).read_bytes() for key, value in PATHS.items()}
text = {key: value.decode('utf-8-sig') for key, value in raw.items() if key != 'texture'}
sha = lambda value: hashlib.sha256(value).hexdigest()
guid = lambda value: re.search(r'^guid: ([0-9a-f]+)$', value, re.M).group(1)
clip_guid = guid(text['clipMeta'])
texture_guid = guid(text['textureMeta'])
controller_guid = guid(text['baseControllerMeta'])

state = next(block for block in text['baseController'].split('--- !u!')
             if block.startswith('1102 &') and re.search(r'^  m_Name: Move$', block, re.M))
state_file_id = int(re.match(r'1102 &(-?\d+)', state).group(1))
state_speed = float(re.search(r'^  m_Speed: ([\d.]+)$', state, re.M).group(1))
speed_parameter_active = int(re.search(r'^  m_SpeedParameterActive: (\d+)$', state, re.M).group(1))
original_guid = re.search(r'm_Motion: \{fileID: 7400000, guid: ([0-9a-f]+)', state).group(1)
override_pairs = re.findall(r'm_OriginalClip: \{fileID: 7400000, guid: ([0-9a-f]+), type: 2\}\s+m_OverrideClip: \{fileID: 7400000, guid: ([0-9a-f]+), type: 2\}', text['overrideController'])
assert dict(override_pairs)[original_guid] == clip_guid
assert re.search(r'm_Controller: \{fileID: 9100000, guid: ([0-9a-f]+)', text['overrideController']).group(1) == controller_guid
assert state_speed == 1 and speed_parameter_active == 0

curve = text['clip'].split('  m_PPtrCurves:', 1)[1].split('    attribute:', 1)[0]
keys = [(float(t), int(fid), g) for t, fid, g in re.findall(r'- time: ([\d.]+)\s+value: \{fileID: (-?\d+), guid: ([0-9a-f]+), type: 3\}', curve)]
fps = float(re.search(r'm_SampleRate: ([\d.]+)', text['clip']).group(1))
start = float(re.search(r'm_StartTime: ([\d.]+)', text['clip']).group(1))
stop = float(re.search(r'm_StopTime: ([\d.]+)', text['clip']).group(1))
loop = bool(int(re.search(r'm_LoopTime: (\d+)', text['clip']).group(1)))
assert len(keys) == 8 and fps == 16 and stop - start == 0.5 and loop
assert all(t == i / fps and g == texture_guid for i, (t, fid, g) in enumerate(keys))

sprites = {}
area = re.split(r'(?m)^    outline: \[\]', text['textureMeta'].split('    sprites:\n', 1)[1], maxsplit=1)[0]
for block in re.split(r'(?m)^    - serializedVersion: \d+\n', area):
    name = re.search(r'^      name: (.+)$', block, re.M)
    if not name:
        continue
    fid = int(re.search(r'^      internalID: (-?\d+)$', block, re.M).group(1))
    rect = {k: int(re.search(r'^        ' + k + r': (\d+)$', block, re.M).group(1)) for k in ['x', 'y', 'width', 'height']}
    pivot = [float(v) for v in re.search(r'pivot: \{x: ([\d.]+), y: ([\d.]+)\}', block).groups()]
    sprites[fid] = {'name': name.group(1), 'rect': rect, 'pivotNormalizedBottomLeft': pivot}

source = Image.open(SOURCE / PATHS['texture']).convert('RGBA')
assert source.size == (384, 512)
(OUT / 'frames').mkdir(parents=True, exist_ok=True)
frames, records = [], []
for i, (t, fid, g) in enumerate(keys):
    sprite = sprites[fid]
    assert sprite['name'] == f'BladeMage_{i + 8:02d}'
    r = sprite['rect']
    assert (r['width'], r['height']) == (64, 64)
    box = [r['x'], source.height - r['y'] - r['height'], r['x'] + r['width'], source.height - r['y']]
    frame = source.crop(box)
    target = OUT / 'frames' / f'{i:02d}.png'
    frame.save(target)
    assert Image.open(target).convert('RGBA').tobytes() == source.crop(box).tobytes()
    frames.append(frame)
    records.append({'order': i, 'spriteName': sprite['name'], 'spriteFileId': str(fid), 'textureGuid': g,
                    'keyTimeSeconds': t, 'keyTimeMs': t * 1000, 'durationMs': 1000 / fps,
                    'unityRectBottomLeft': r, 'pngCropBoxTopLeft': box,
                    'pivotNormalizedBottomLeft': sprite['pivotNormalizedBottomLeft'],
                    'pivotPixelsTopLeft': [64 * sprite['pivotNormalizedBottomLeft'][0], 64 * (1 - sprite['pivotNormalizedBottomLeft'][1])],
                    'file': f'frames/{i:02d}.png', 'sha256': sha(target.read_bytes()),
                    'rgbaSha256': sha(frame.tobytes()), 'rgbaExactSourceCrop': True})

sheet = Image.new('RGBA', (512, 64))
for i, frame in enumerate(frames):
    sheet.paste(frame, (i * 64, 0))
sheet.save(OUT / 'sheet.png')

opaque_colors = sorted({pixel[:3] for frame in frames for pixel in frame.get_flattened_data() if pixel[3]})
assert len(opaque_colors) <= 255, 'Use an explicit quantization note if the source exceeds GIF palette capacity.'
assert {pixel[3] for frame in frames for pixel in frame.get_flattened_data()} <= {0, 255}
palette = [0, 0, 0] + [channel for color in opaque_colors for channel in color]
palette += [0] * (768 - len(palette))
indices = {color: i + 1 for i, color in enumerate(opaque_colors)}
indexed = []
for frame in frames:
    p = Image.new('P', frame.size)
    p.putpalette(palette)
    p.putdata([indices[pixel[:3]] if pixel[3] else 0 for pixel in frame.get_flattened_data()])
    indexed.append(p)
# GIF units are 10ms. This nearest schedule has at most5ms boundary error and totals500ms.
gif_durations = [60, 60, 70, 60, 60, 60, 70, 60]
indexed[0].save(OUT / 'reference.gif', save_all=True, append_images=indexed[1:], duration=gif_durations,
                loop=0, transparency=0, disposal=2, optimize=False)
gif = Image.open(OUT / 'reference.gif')
decoded = [f.copy().convert('RGBA') for f in ImageSequence.Iterator(gif)]
read_durations = [f.info['duration'] for f in ImageSequence.Iterator(gif)]
assert gif.n_frames == 8 and read_durations == gif_durations and sum(read_durations) == 500
for a, b in zip(frames, decoded):
    assert list(a.getchannel('A').get_flattened_data()) == list(b.getchannel('A').get_flattened_data())
    assert all(x[:3] == y[:3] for x, y in zip(a.get_flattened_data(), b.get_flattened_data()) if x[3])

font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 19)
title = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 26)
contact = Image.new('RGB', (1280, 780), '#101c2c')
draw = ImageDraw.Draw(contact)
draw.text((40, 18), 'SimpleGame 검술사 · 실제 Move 8프레임', font=title, fill='#f4d29a')
draw.text((40, 56), '원본 64×64 · 16fps · 500ms / 방향·크기·프레임 순서 원본 유지', font=font, fill='#c2cad5')
for i, (frame, record) in enumerate(zip(frames, records)):
    x, y = 48 + (i % 4) * 296, 100 + (i // 4) * 320
    enlarged = frame.resize((256, 256), Image.Resampling.NEAREST)
    contact.paste(enlarged, (x, y), enlarged)
    draw.text((x, y + 256), f'{i:02d} · {record["spriteName"]}', font=font, fill='#f4d29a')
    draw.text((x, y + 283), f'{record["keyTimeMs"]:g}–{record["keyTimeMs"]+62.5:g}ms', font=font, fill='#c2cad5')
draw.text((40, 752), '표시만4배 확대. 원본 픽셀은 frames/00.png~07.png에 그대로 보존.', font=font, fill='#99a9bd')
contact.save(OUT / 'contact.png')

for key, value in raw.items():
    assert (SOURCE / PATHS[key]).read_bytes() == value, f'Source changed: {key}'
manifest = {
    'schemaVersion': 1, 'id': 'simplegame-blademage-move-reference-v10', 'sourceProject': str(SOURCE),
    'purpose': '사용자가 지정한 SimpleGame 프로젝트 검술사의 이동 동작 참고. LastBell 완성 자산으로 표시하지 않음.',
    'rights': '사용자 프로젝트의 동작 참고 자료로 추출했다. 라이선스 종류·재배포 허용 여부를 추정하거나 새로 부여하지 않는다.',
    'sourceReadOnly': True, 'originalsUnchangedAfterExtraction': True,
    'sources': [{'role': key, 'path': (SOURCE / path).as_posix(), 'sha256': sha(raw[key]), 'bytes': len(raw[key])} for key, path in PATHS.items()],
    'trace': {'baseControllerGuid': controller_guid, 'stateName': 'Move', 'stateFileId': str(state_file_id),
              'baseMotionGuid': original_guid, 'overrideControllerGuid': guid(text['overrideMeta']),
              'overrideMotionGuid': clip_guid, 'spriteCurvePath': 'Visual/Sprite', 'textureGuid': texture_guid,
              'chain': 'Player.controller Move.m_Motion → BladeMage.overrideController → BladeMage_Move.anim sprite fileID → Character.png.meta internalID/rect → Character.png'},
    'timing': {'clipFps': fps, 'clipStartSeconds': start, 'clipStopSeconds': stop, 'clipLoop': loop,
               'frameCount': 8, 'frameDurationMs': 62.5, 'loopDurationMs': 500,
               'controllerStateSpeed': state_speed, 'controllerSpeedParameterActive': bool(speed_parameter_active),
               'loopAtSerializedControllerSpeedMs': (stop-start)*1000/state_speed,
               'scope': '클립과 직렬화된 Move 상태 속도 근거. 실시간 Animator.speed·Time.timeScale·실행 중 전환 측정은 포함하지 않음.',
               'gifDurationsMs': gif_durations, 'gifLoopDurationMs': 500, 'gifAverageFps': 16,
               'gifTimingNote': 'GIF10ms 단위에 맞춘60/70ms 분배. 각 프레임을 정확62.5ms로 표시할 수 없어 경계 오차 최대5ms, 한 주기500ms 보존.'},
    'image': {'sourceSize': list(source.size), 'frameSize': [64,64], 'framePixelPolicy': '원형 그대로: resize/flip/trim/color edit 없음',
              'facing': 'left in the source drawings; not mirrored', 'sheet': 'sheet.png', 'columns': 8,
              'contact': 'contact.png', 'contactScale': 4, 'gif': 'reference.gif', 'gifSize': [64,64],
              'opaquePaletteColors': len(opaque_colors), 'gifVisibleColorsLossless': True, 'alphaValues': [0,255]},
    'frames': records,
    'qa': {'mappedFrameIds': 8, 'sourceRectMatches': 8, 'rgbaExactSourceCrops': 8, 'decodedGifFrames': 8,
           'decodedGifDurationMs': 500, 'gifVisibleColorsAndAlphaExact': True, 'sourceHashesUnchanged': True,
           'unityRuntimePlaybackTested': False},
    'outputs': [{'path': name, 'sha256': sha((OUT/name).read_bytes())} for name in ['sheet.png','contact.png','reference.gif']],
}
(OUT / 'source-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'frames': len(frames), 'sourceNames': [r['spriteName'] for r in records], 'gifMs': sum(read_durations),
                  'opaqueColors': len(opaque_colors), 'sourceUnchanged': True}))
