"""Import two publisher-provided CC0 retro sounds with recorded, reproducible gain."""
from pathlib import Path
import hashlib, io, json, wave, zipfile
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Planning/Story/Dragon-v1/Audio-v1/Sources/retro'
OUT = ROOT / 'Client/Assets/Game/Audio/Dragon-v4'
REVIEW = ROOT / 'Planning/Story/Dragon-v1/Audio-v2'
URL = 'https://opengameart.org/content/512-sound-effects-8-bit-style'
LICENSE = 'https://creativecommons.org/publicdomain/zero/1.0/'

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    REVIEW.mkdir(parents=True, exist_ok=True)
    archive = next(SOURCE.glob('*.zip'))
    records = []
    with zipfile.ZipFile(archive) as z:
        for file, source_name, peak, trigger in [
            ('pixel-hit.wav', 'sfx_damage_hit3.wav', -6, 'Confirmed player hit, once at the Core contact event'),
            ('pixel-touch.wav', 'sfx_menu_move1.wav', -12, 'Attack button/key press while combat input is enabled; never a hit event'),
        ]:
            member = next(n for n in z.namelist() if n.endswith('/' + source_name))
            raw = z.read(member)
            with wave.open(io.BytesIO(raw)) as w:
                assert w.getsampwidth() == 2 and w.getframerate() == 44100
                samples = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(float).reshape(-1, w.getnchannels()).mean(axis=1) / 32768
            gain = 10 ** (peak / 20) / max(abs(samples).max(), 1e-9)
            pcm = np.clip(samples * gain * 32767, -32767, 32767).round().astype('<i2')
            target = OUT / file
            with wave.open(str(target), 'wb') as w:
                w.setparams((1, 2, 44100, 0, 'NONE', 'not compressed'))
                w.writeframes(pcm.tobytes())
            (REVIEW / file).write_bytes(target.read_bytes())
            records.append(dict(file=file, author='SubspaceAudio / Juhani Junkala', sourceUrl=URL,
                license='CC0-1.0', sourceArchive=archive.relative_to(ROOT).as_posix(), sourceMember=member,
                sourceSha256=hashlib.sha256(raw).hexdigest(), sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                frames=len(pcm), durationMs=round(len(pcm)/44.1, 3), sampleRate=44100, channels=1,
                peakDbfs=peak, gain=float(gain), trigger=trigger,
                edits='Mono PCM16; fixed gain only. No trim, pitch, time change or layered sound.'))
    manifest=dict(version='Dragon-v4 / 0.9.3', checkedAt='2026-10-24', status='assets-prepared-awaiting-saved-bindings',
        sourceEvidence=(SOURCE/'source.html').relative_to(ROOT).as_posix(), files=records,
        limitation='Chosen from author-described 8-bit library. No claim of auditory identity with Timing Hero.')
    payload=json.dumps(manifest,ensure_ascii=False,indent=2)+'\n'
    (OUT/'manifest.json').write_text(payload,encoding='utf-8')
    (REVIEW/'manifest.json').write_text(payload,encoding='utf-8')
    credits='Dragon v4 pixel attack sounds\nSubspaceAudio / Juhani Junkala\n'+URL+'\nCC0-1.0: '+LICENSE+'\n\n'
    for r in records:
        credits+=r['file']+' <- '+r['sourceMember']+'\n'+r['edits']+'\nSHA256: '+r['sha256']+'\n\n'
    (OUT/'CREDITS.txt').write_text(credits,encoding='utf-8')
    (REVIEW/'CREDITS.txt').write_text(credits,encoding='utf-8')
    distributed=ROOT/'Client/Assets/StreamingAssets/AudioLicenses/Dragon-v4-CC0.txt'
    distributed.write_text(credits,encoding='utf-8')
    previous=(ROOT/'Client/Assets/Game/Audio/Combat-v86/UI_CREDITS.txt').read_text(encoding='utf-8')
    (OUT/'UI_CREDITS.txt').write_text(previous+'\n\n'+credits,encoding='utf-8')
    cards=''.join('<article class="card"><h3>'+title+'</h3><audio controls preload="none" src="'+file+'"></audio><p>'+text+'</p></article>' for file,title,text in [
        ('pixel-hit.wav','적중 · 도트 타격음','114ms. 실제로 적을 맞힌 순간에 한 번 울립니다.'),
        ('pixel-touch.wav','입력 · 도트 터치음','39ms. 공격 버튼을 누른 순간 짧고 작게 울립니다.')])
    (REVIEW/'index.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>도트 공격음</title><link rel="stylesheet" href="../board.css"><style>audio{width:100%}main{padding-top:24px}</style><main><a href="../index.html#attack-audio">기획서로</a><h1>누르는 소리, 맞히는 소리.</h1><p>짧은 8비트 타격음과 터치음을 분리했습니다. 원작과 같은 음원이라는 뜻은 아닙니다.</p><div class="grid2">'+cards+'</div><p><a href="'+URL+'">원저작자 배포 페이지 · CC0</a> · <a href="CREDITS.txt">출처·편집 내역</a></p></main></html>',encoding='utf-8')
    print(json.dumps([{'file':r['file'],'durationMs':r['durationMs'],'sha256':r['sha256']} for r in records]))

if __name__=='__main__': main()
