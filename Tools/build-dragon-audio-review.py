"""Build a local, click-to-play SFX comparison sheet. Never touches Unity."""
from pathlib import Path
import hashlib, html, json, subprocess, wave, zipfile
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
B=ROOT/'Planning/Story/Dragon-v1/Audio-v1'
FF='C:/Users/JW/AppData/Roaming/vrew/ffmpeg_gpl_vgx_v3/ffmpeg.exe'
RATE=44100
source_data=json.loads((B/'sources.json').read_text(encoding='utf-8'))
sources={x['id']:x for x in source_data['sources']}
specs=[
 ('current','현재 Unity 공격음','현재 비교 기준 · 칼날과 타격음을 합친 280ms 베기','Kenney',None,'sword-strike.wav'),
 ('retro-1','A1 · 8비트 검 1','레트로 검 효과 후보 · 먼저 비교할 계열','SubspaceAudio / Juhani Junkala','retro','sfx_wpn_sword1.wav'),
 ('retro-2','A2 · 8비트 검 2','레트로 검 효과 후보 · A1과 공격 인상 비교','SubspaceAudio / Juhani Junkala','retro','sfx_wpn_sword2.wav'),
 ('retro-3','A3 · 8비트 검 3','레트로 검 효과 후보 · A1·A2와 비교','SubspaceAudio / Juhani Junkala','retro','sfx_wpn_sword3.wav'),
 ('metal-1','B · 녹음한 칼날','칼날 질감을 더하는 보조 후보','StarNinjas','sword','sword.1.ogg'),
 ('clash-1','C · 검 충돌','쳐내기·치명타의 접촉감을 분리할 후보','StarNinjas','sword','sword_clash.1.ogg'),
 ('swish-9','D · 짧은 휘두름','베기 시작을 알리는 보조 후보','artisticdude','swishes','swish-9.wav'),
]

records=[]
for key,title,description,author,pack,basename in specs:
    if pack:
        matches=[]
        for item in sources[pack]['archives']:
            for member in item['contents']:
                if Path(member).name==basename and '__MACOSX' not in member:
                    matches.append((item,member))
        assert len(matches)==1,(basename,matches)
        archive,member=matches[0]
        with zipfile.ZipFile(B/archive['file']) as z: raw=z.read(member)
        original=B/'Originals'/pack/basename
        original.parent.mkdir(parents=True,exist_ok=True);original.write_bytes(raw)
        source_url=sources[pack]['sourceUrl']
        original_ref=dict(archive=archive['file'],member=member)
    else:
        original=ROOT/'Client/Assets/Game/Audio/Combat-v86/sword-strike.wav'
        raw=original.read_bytes();source_url='https://kenney.nl/assets/rpg-audio'
        original_ref=dict(file=original.relative_to(ROOT).as_posix())
    x=np.frombuffer(subprocess.check_output([FF,'-hide_banner','-loglevel','error','-i',str(original),'-ar',str(RATE),'-ac','1','-f','f32le','pipe:1']),dtype='<f4').copy()
    gain=10**(-6/20)/max(float(np.abs(x).max()),1e-9)
    x*=gain
    target=B/'Samples'/(key+'.wav');target.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(target),'wb') as w:
        w.setparams((1,2,RATE,0,'NONE','not compressed'))
        w.writeframes((np.clip(x,-1,1)*32767).round().astype('<i2').tobytes())
    records.append(dict(id=key,title=title,description=description,author=author,sourceUrl=source_url,license='CC0-1.0',original=original_ref,originalSha256=hashlib.sha256(raw).hexdigest(),file=target.relative_to(B).as_posix(),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),durationMs=round(len(x)/RATE*1000),peakDbfs=-6,edits=f'Mono 44.1kHz PCM16; fixed gain {gain:.6f} to peak -6dBFS; no trimming/EQ/time change. Comparison only.'))

manifest=dict(checkedAt='2026-10-24',status='audition-candidates-not-unity-applied',reference='용사는 타이밍 / Timing Hero',referenceListening='No new side-by-side auditory evaluation or waveform matching was performed; likeness is not confirmed.',referenceFootage='Planning/Reference/TimingHero-20261023/clips/reference-normal.mp4',firstDirection='Compare A1-A3 retro sword effects first. Choose final clip only after hearing with gameplay.',files=records)
(B/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
credits=['공격음 비교 후보 · 2026-10-24','기획서 청취용. Unity와 EXE 오디오는 변경하지 않았음.','모든 신규 후보의 작성자 배포 페이지에서 CC0 확인.','CC0: https://creativecommons.org/publicdomain/zero/1.0/','']
cards=[]
e=html.escape
for r in records:
    credits += [r['title'],r['author'],r['sourceUrl'],r['original'].get('member',r['original'].get('file','')),r['edits'],'']
    cards.append(f'<article class="card"><h3>{e(r["title"])}</h3><p>{e(r["description"])}</p><audio controls preload="none" aria-label="{e(r["title"])}"><source src="Audio-v1/{r["file"]}" type="audio/wav"></audio><p class="small">{r["durationMs"]}ms · {e(r["author"])} · <a href="{e(r["sourceUrl"])}">출처·CC0</a></p></article>')
(B/'CREDITS.txt').write_text('\n'.join(credits)+'\n',encoding='utf-8')
section='''<section id="attack-audio"><p class="eyebrow">ATTACK SOUND · AUDITION</p><h2>한 번의 베기, 짧고 분명한 타격음.</h2><p class="lead">용사는 타이밍과 어울리는 방향으로, 8비트 검 효과음을 먼저 비교합니다. 현재 공격음과 새 후보를 아래에서 직접 들어볼 수 있습니다.</p><p class="note"><b>음원 후보 조사 · Unity 미적용.</b> 원작과의 신규 청취 대조는 아직 하지 않았으므로 동일하거나 가장 비슷하다고 확정하지 않습니다. 작성자 배포 페이지에서 CC0를 확인했고, 비교 파일은 피크−6dBFS로 맞췄습니다. 체감 음량까지 동일하다는 뜻은 아닙니다.</p><div class="grid3">'''+''.join(cards)+'''</div><div class="grid2" style="margin-top:24px"><article class="card"><h3>먼저 비교할 방향</h3><p>A1~A3 중 짧고 공격 시작이 분명한 레트로 검음을 기본으로 검토합니다. 길게 울리는 잔향은 줄이고, 실제 명중 시점과 소리가 맞아야 합니다. B·D는 칼날이나 휘두름이 부족할 때만 약하게 더하는 후보입니다.</p></article><article class="card"><h3>일반타·치명타·막타 구분</h3><p>기획: 일반타는 짧은 베기, 치명타는 같은 베기에 강한 접촉음, 막타는 낮은 충격을 짧게 더합니다. 볼륨만 크게 올리지 않으며 공격 예고음이 묻히지 않도록 합니다. 현재 실행판은 같은 공격음 한 번을 재생합니다.</p></article></div><p><a href="Audio-v1/CREDITS.txt">출처·편집 내역</a> · <a href="Audio-v1/manifest.json">후보 파일·검증 범위</a> · <a href="Audio-v1/reference-audio-review.md">담당 조사</a></p></section>'''
(B.parent/'attack-audio-section.html').write_text(section,encoding='utf-8')
standalone=section.replace('src="Audio-v1/','src="').replace('href="Audio-v1/','href="')
(B/'index.html').write_text('<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>공격음 후보 비교</title><link rel="stylesheet" href="../board.css"><style>audio{max-width:100%;width:100%}main{padding-top:24px}</style></head><body><main><a href="../index.html#attack-audio">기획서로 돌아가기</a>'+standalone+'</main></body></html>',encoding='utf-8')
print(json.dumps([dict(id=r['id'],durationMs=r['durationMs'],file=r['file']) for r in records]))
