"""Deterministic original bell/ambient compositions plus documented CC0 source copies."""
from pathlib import Path
import hashlib, json, shutil, wave
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Client/Assets/Game/Audio/Presentation'
SOURCE=ROOT/'Temp/PresentationIntegration/AudioSources'
OUT.mkdir(parents=True,exist_ok=True)
RATE=22050
rng=np.random.default_rng(41821)

def wav(name,samples):
    samples=np.asarray(samples)
    peak=float(np.max(np.abs(samples)))
    if peak>.88:samples=samples*.88/peak
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(RATE)
        f.writeframes((samples*32767).astype('<i2').tobytes())

def tone(hz,seconds,kind='bell'):
    t=np.arange(int(seconds*RATE))/RATE
    if kind=='bell':
        a=sum(np.sin(2*np.pi*hz*k*t)*np.exp(-t*d)*v for k,d,v in [(1,2.8,.55),(2.76,6,.25),(5.4,10,.08)])
    elif kind=='bass':a=(np.sin(2*np.pi*hz*t)+.15*np.sin(2*np.pi*hz*3*t))*np.exp(-t*3)*.5
    else:a=(np.sin(2*np.pi*hz*t)+.2*np.sin(2*np.pi*hz*2*t))*.14*np.sin(np.pi*np.minimum(t/seconds,1))**2
    return a*np.minimum(t/.008,1)

def music(name,bpm,bright=False,boss=False):
    # Original eight-bar A-minor sequence; no recorded music or borrowed melody.
    beat=60/bpm;length=beat*32;mix=np.zeros(round(length*RATE))
    roots=[110,87.307,130.813,97.999,110,146.832,130.813,82.407]
    def add(sound,start,gain):
        indices=(np.arange(len(sound))+int(start*RATE))%len(mix)
        np.add.at(mix,indices,sound*gain)
    for bar,root in enumerate(roots):
        add(tone(root,beat*4,'pad'),bar*4*beat,.8)
        add(tone(root*1.5,beat*4,'pad'),bar*4*beat,.55)
        for n in range(4 if not bright else 2):
            add(tone(root/2,beat*.85,'bass'),(bar*4+n*(2 if bright else 1))*beat,.28 if bright else .5)
        motif=[2,3,2.5,1.5] if bar%2==0 else [2.5,2,1.5,1]
        for n,ratio in enumerate(motif):
            add(tone(root*ratio*(2 if bright else 1),beat*2.2), (bar*4+n*.75+.25)*beat,.2 if bright else .15)
        if boss:
            for n in range(8):
                t=np.arange(int(.08*RATE))/RATE
                add(rng.normal(0,.2,len(t))*np.exp(-t*65),(bar*4+n*.5)*beat,.3)
    wav(name,mix)

music('battle-bells',96)
music('boss-bells',120,boss=True)
music('lobby-bells',72,bright=True)
wav('critical-bell',tone(659.255,.5))
wav('lantern-ignite',tone(880,.65)*.5+tone(440,.65)*.25)
wav('perfect-dodge',tone(1174.659,.25)*.5)
for name,seconds,base in [('mini-roar',.75,62),('boss-roar',1.0,46)]:
    t=np.arange(int(seconds*RATE))/RATE
    envelope=np.sin(np.pi*t/seconds)**.7
    rough=np.sin(2*np.pi*base*t+3*np.sin(2*np.pi*18*t))*.5
    hiss=np.convolve(rng.normal(0,.2,len(t)),np.ones(12)/12,mode='same')
    ring=tone(base*3.1,seconds)*.25
    wav(name,(rough+hiss+ring)*envelope)

copies={'knifeSlice.ogg':('rpg','knifeSlice.ogg'),'footstep03.ogg':('rpg','footstep03.ogg'),
        'hit.ogg':('impact','impactMetal_medium_001.ogg'),'spike.ogg':('impact','impactWood_heavy_001.ogg')}
sources=[]
for dest,(pack,filename) in copies.items():
    matches=list((SOURCE/pack).rglob(filename))
    if len(matches)!=1:raise RuntimeError((pack,filename,matches))
    shutil.copy2(matches[0],OUT/dest)
    sources.append({'file':dest,'originalFile':filename,'source':'https://kenney.nl/assets/'+('rpg-audio' if pack=='rpg' else 'impact-sounds'),'license':'CC0-1.0'})
for pack in ['rpg','impact']:
    shutil.copy2(SOURCE/pack/'License.txt',OUT/(pack+'-LICENSE.txt'))
for f in sorted(OUT.glob('*.wav')):
    sources.append({'file':f.name,'source':'Client/Tools/build_presentation_audio.py','license':'Original procedural audio produced for Last Bell; no third-party samples or borrowed melody'})
for s in sources:s['sha256']=hashlib.sha256((OUT/s['file']).read_bytes()).hexdigest()
(OUT/'audio-provenance.json').write_text(json.dumps({'date':'2026-10-21','sampleRate':RATE,'files':sources},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(OUT/'CREDITS.txt').write_text('Sound effects: RPG Audio and Impact Sounds by Kenney (kenney.nl), CC0 1.0.\nMusic, bell tones and synthesized creature roars: original procedural compositions for Last Bell.\n',encoding='utf-8')
print(f'Prepared {len(sources)} audio files with source records.')
