"""Reproducible CC0 soundtrack and short combat SFX, with distributed provenance."""
from pathlib import Path
import hashlib, json, shutil, subprocess, wave
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
HERE=ROOT/'Planning/Story/Audio-v86'
SRC=HERE/'Sources'
OUT=ROOT/'Client/Assets/Game/Audio/Combat-v86'
LICENSE=ROOT/'Client/Assets/StreamingAssets/AudioLicenses'
FF='C:/Users/JW/AppData/Roaming/vrew/ffmpeg_gpl_vgx_v3/ffmpeg.exe'
RATE=44100
OUT.mkdir(parents=True,exist_ok=True)
def decode(p,filters=None):
    args=[FF,'-hide_banner','-loglevel','error','-i',str(p)]
    if filters:args+=['-af',filters]
    return np.frombuffer(subprocess.check_output(args+['-ar',str(RATE),'-ac','2','-f','f32le','pipe:1']),dtype='<f4').reshape(-1,2).copy()
def stats(x):
    return dict(durationSeconds=len(x)/RATE,peakDbfs=float(20*np.log10(max(np.abs(x).max(),1e-9))),rmsDbfs=float(20*np.log10(max(np.sqrt(np.mean(x*x)),1e-9))),loopJump=float(np.max(np.abs(x[-1]-x[0]))))
def normalize(x,rms=-19,peak=-3):
    gain=min(10**(rms/20)/np.sqrt(np.mean(x*x)),10**(peak/20)/np.abs(x).max())
    return x*gain,float(gain)
def write(name,x):
    temp=OUT/(Path(name).stem+'.wav')
    with wave.open(str(temp),'wb') as w:
        w.setparams((2,2,RATE,0,'NONE','not compressed'));w.writeframes((np.clip(x,-1,1)*32767).round().astype('<i2').tobytes())
    target=OUT/name
    if name.endswith('.ogg'):
        subprocess.run([FF,'-hide_banner','-loglevel','error','-y','-i',str(temp),'-c:a','libvorbis','-q:a','6',str(target)],check=True)
        temp.unlink()
    return target
def hash(p):return hashlib.sha256(p.read_bytes()).hexdigest()
records=[]
music=list((SRC/'chiptune-adventures').glob('*.ogg'))
for role,index,label in [('lobby',4,'로비 · Stage Select'),('road',1,'성 밖 · Stage 1'),('quarter',2,'주거구 · Stage 2'),('boss',3,'보스 · Boss Fight')]:
    source=next(p for p in music if f'] {index}.' in p.name)
    x,gain=normalize(decode(source),-19,-4)
    target=write('bgm-'+role+'.ogg',x)
    records.append(dict(role=role,label=label,file=target.name,creator='Juhani Junkala / SubspaceAudio',title=source.stem,sourceUrl='https://opengameart.org/content/4-chiptunes-adventure',license='CC0-1.0',originalFile=source.name,originalSha256=hash(source),sha256=hash(target),edits=f'Fixed gain {gain:.6f}; stereo 44.1kHz Vorbis; original full loop and tempo preserved, no added bell layer.',**stats(decode(target))))

def short(p,seconds,filters):
    x=decode(p,filters)
    # Remove leading silence only, align audible attack with the confirmed hit.
    active=np.flatnonzero(np.max(np.abs(x),axis=1)>.012)
    start=max(0,int(active[0]) - 44) if len(active) else 0
    x=x[start:start+round(seconds*RATE)]
    x[:88]*=np.linspace(0,1,88)[:,None]
    n=min(len(x),round(.035*RATE));x[-n:]*=np.linspace(1,0,n)[:,None]
    return x,start/RATE
knife=SRC/'kenney-rpg/Audio/knifeSlice2.ogg'
chop=SRC/'kenney-rpg/Audio/chop.ogg'
blade,a=short(knife,.24,'highpass=f=180,lowpass=f=8500')
impact,b=short(chop,.19,'highpass=f=90,lowpass=f=6000')
x=np.zeros((round(.28*RATE),2),dtype=np.float32)
x[:len(blade)]+=blade*.85
x[:len(impact)]+=impact*.65
x,gain=normalize(x,-18,-3)
target=write('sword-strike.wav',x)
records.append(dict(role='attack',label='공격 · 짧은 베기',file=target.name,creator='Kenney',title='RPG Audio: knifeSlice2 + chop',sourceUrl='https://kenney.nl/assets/rpg-audio',license='CC0-1.0',originalFiles=[dict(file=p.name,sha256=hash(p)) for p in [knife,chop]],sha256=hash(target),edits=f'Onset trim {a:.6f}/{b:.6f}s, EQ, layer blade .85 + chop .65, 280ms, 2ms attack/35ms release, fixed gain {gain:.6f}.',**stats(x)))
source=ROOT/'Planning/Story/Audio-v72/Sources/60030-hq.mp3'
x=decode(source,'highpass=f=240,lowpass=f=8500')[round(.045*RATE):round(.315*RATE)]
x[:88]*=np.linspace(0,1,88)[:,None];x[-1323:]*=np.linspace(1,0,1323)[:,None]
x,gain=normalize(x,-19,-4)
target=write('dodge-air.wav',x)
records.append(dict(role='dodge',label='일반·완벽 회피 · 바람',file=target.name,creator='qubodup',title='Swosh / Whoosh / Air Cut',sourceUrl='https://freesound.org/people/qubodup/sounds/60030/',license='CC0-1.0',originalFile=source.name,originalSha256=hash(source),sha256=hash(target),edits=f'Public HQ MP3 preview; 45–315ms excerpt, highpass240/lowpass8500, 2ms/30ms fades, fixed gain {gain:.6f}. Accepted input plays once; perfect result does not add a bell or duplicate.',**stats(x)))
for r in records:
    assert r['peakDbfs'] < -2 and r['durationSeconds']>0
evidence=[dict(file=str(p.relative_to(ROOT)).replace('\\','/'),sha256=hash(p)) for p in [SRC/'chiptune-adventures.zip',SRC/'chiptune-adventures/INFO.txt',SRC/'kenney-rpg.zip',SRC/'kenney-rpg/License.txt',SRC/'music-source.html',SRC/'rpg-source.html',SRC/'impact-source.html',ROOT/'Planning/Story/Audio-v72/Sources/60030-source.html']]
manifest=dict(version='combat-audio-v8.6',checkedAt='2026-10-23',licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/',files=records,sourceEvidence=evidence,removedFromPlayback=['bell-low-long','dark shrine','dark theme','dungeon ambience','fire sword'],qualityNote='Signal/clipping and loop-boundary measurements recorded. Runtime verification is in Planning/Evidence/Feedback-v86. Subjective timbre is reviewable in the audio preview.')
credits=['마지막 종지기 · v8.6 현재 사용 음원','새 배경음·공격음·회피음: CC0-1.0','']
for r in records:credits += [r['label'],r['title']+' — '+r['creator'],r['sourceUrl'],r['license'],r['edits'],'']
credits+=['유지 효과음: 피격 Impact: Smack Body — Philipp Grzemba / Sheyvan (CC0)', 'https://freesound.org/people/Sheyvan/sounds/540752/', '적 무기 충돌: Medieval sound effects — Weapon impacts — Ben Jaszczak & Brian Nelson / Still North Media (CC0)','https://opengameart.org/content/medieval-sound-effects-weapon-impacts','발소리: Kenney RPG Audio (CC0), 충격: Kenney Impact Sounds (CC0)','https://kenney.nl/assets/impact-sounds','등불 점화·등장·골드 획득 효과: Last Bell 자체 합성.','종 효과음과 기존 어두운 BGM은 재생하지 않음.','CC0 1.0: https://creativecommons.org/publicdomain/zero/1.0/','이전 v7.2 사용 이력과 CC BY 원문은 historical-v72-* 파일로 보존.']
for p in [HERE,OUT]:
    (p/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (p/'CREDITS.txt').write_text('\n'.join(credits)+'\n',encoding='utf-8')
for name in ['CREDITS.txt','manifest.json','UI_CREDITS.txt']:
    previous=LICENSE/name
    archive=LICENSE/('historical-v72-'+name)
    if previous.exists() and not archive.exists():shutil.copy2(previous,archive)
shutil.copy2(OUT/'CREDITS.txt',OUT/'UI_CREDITS.txt')
for name in ['CREDITS.txt','manifest.json','UI_CREDITS.txt']:shutil.copy2(OUT/name,LICENSE/name)
shutil.copy2(SRC/'chiptune-adventures/INFO.txt',LICENSE/'Juhani-Junkala-CC0.txt')
shutil.copy2(SRC/'kenney-rpg/License.txt',LICENSE/'Kenney-RPG-CC0.txt')
print(json.dumps([{k:r[k] for k in ['file','durationSeconds','peakDbfs','rmsDbfs','loopJump']} for r in records],indent=2))
