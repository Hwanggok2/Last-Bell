"""Preview audio only. Source recordings, exact edits and measurements are recorded; Client is never written."""
from pathlib import Path
import hashlib, json, shutil, subprocess, wave
import numpy as np

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
FFMPEG=Path('C:/Users/JW/AppData/Roaming/vrew/ffmpeg_gpl_vgx_v3/ffmpeg.exe')
RATE=44100
OUT=HERE/'Audio'; OUT.mkdir(exist_ok=True)
KENNEY=ROOT/'Temp/PresentationIntegration/AudioSources'
sources=json.loads((HERE/'sources.json').read_text(encoding='utf-8'))
records=[]

def decode(file,filters=None,channels=1):
    cmd=[str(FFMPEG),'-hide_banner','-loglevel','error','-i',str(file)]
    if filters: cmd+=['-af',filters]
    raw=subprocess.check_output(cmd+['-ar',str(RATE),'-ac',str(channels),'-f','f32le','pipe:1'])
    return np.frombuffer(raw,dtype='<f4').copy().reshape(-1,channels)

def fade(x,attack=.002,release=.018):
    a=min(len(x),round(attack*RATE));r=min(len(x),round(release*RATE))
    if a:x[:a]*=np.linspace(0,1,a)[:,None]
    if r:x[-r:]*=np.linspace(1,0,r)[:,None]
    return x

def trim(x):
    # Keep 4ms before the first audible onset and discard only near-silent endpoints.
    active=np.flatnonzero(np.max(np.abs(x),axis=1)>np.max(np.abs(x))*.008)
    return x[max(0,active[0]-176):min(len(x),active[-1]+882)] if len(active) else x

def save(key,x,title,source,edits,kind='sfx',gain=1):
    x=x-np.mean(x,axis=0)
    if kind=='music':
        x*=min(.105/max(float(np.sqrt(np.mean(x*x))),1e-6),.70/max(float(np.max(np.abs(x))),1e-6))
    else: x*=.78/max(float(np.max(np.abs(x))),1e-6)
    ext='ogg' if kind=='music' else 'wav';name=key+'.'+ext;target=OUT/name
    pcm=np.clip(x,-1,1)
    if ext=='wav':
        with wave.open(str(target),'wb') as w:
            w.setnchannels(x.shape[1]);w.setsampwidth(2);w.setframerate(RATE);w.writeframes((pcm*32767).astype('<i2').tobytes())
    else:
        subprocess.run([str(FFMPEG),'-hide_banner','-loglevel','error','-y','-f','f32le','-ar',str(RATE),'-ac',str(x.shape[1]),'-i','pipe:0','-c:a','libvorbis','-q:a','5',str(target)],input=pcm.astype('<f4').tobytes(),check=True)
    decoded=decode(target,channels=x.shape[1]);peak=float(np.max(np.abs(decoded)))
    if not np.isfinite(decoded).all() or peak>=.99:raise ValueError('Invalid/clipped output '+key)
    rec={'key':key,'title':title,'file':'Audio/'+name,'kind':kind,'gain':gain,'durationSeconds':round(len(decoded)/RATE,4),'sampleRate':RATE,'channels':x.shape[1],'peakDbfs':round(20*np.log10(max(peak,1e-9)),2),'rmsDbfs':round(20*np.log10(max(float(np.sqrt(np.mean(decoded*decoded))),1e-9)),2),'source':source,'edits':edits,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
    if kind=='music':rec['loopBoundaryStep']=round(float(np.max(np.abs(decoded[0]-decoded[-1]))),6)
    records.append(rec)

def kenney(pack,name):return KENNEY/pack/'Audio'/name
clash_source=next(s for s in sources if s['id']=='weapon-impacts')
clash_folder=HERE/'Sources/weapon-impacts';clash_folder.mkdir(exist_ok=True)
original=clash_folder/clash_source['selectedFile']
if not original.exists():
    subprocess.run(['tar','-xf',str(HERE/'Sources'/clash_source['file']),'-C',str(clash_folder),clash_source['selectedFile']],check=True)
clash=decode(original)
start=round(clash_source['excerptStartSeconds']*RATE)
clash=clash[start:start+round(clash_source['excerptDurationSeconds']*RATE)]
save('sword-clash',fade(clash,attack=.0005,release=.06),'검 충돌 · 챙',
     {'id':'weapon-impacts','file':clash_source['selectedFile'],'startSeconds':clash_source['excerptStartSeconds']},
     'One sabre/Norse sword blade-on-blade strike; 12.4575–13.1075s excerpt; original pitch, EQ and metallic decay; 0.5ms onset / 60ms end fade',gain=.75)
hit=trim(decode(kenney('impact','impactMetal_light_002.ogg'),'highpass=f=350,equalizer=f=2700:t=q:w=1:g=5'))[:round(.32*RATE)]
save('hit-sharp',fade(hit,release=.04),'금속 명중',{'pack':'Kenney Impact Sounds','file':'impactMetal_light_002.ogg'},'350Hz high-pass; +5dB at 2.7kHz; tail trimmed to 320ms',gain=.60)
bell=trim(decode(HERE/'Sources/bell-3446.mp3','highpass=f=65,asetrate=28224,aresample=44100,lowpass=f=2800'))
bell=fade(bell,attack=.005,release=.8)
save('bell-low-long',bell,'낮고 긴 종',{'id':'bell'},'Recorded church bell, playback rate 0.64 (-7.73 semitones); 65Hz high-pass / 2.8kHz low-pass; full stretched decay; 800ms end fade',gain=.65)
cue=bell[:round(1.15*RATE)].copy();cue*=np.exp(-np.arange(len(cue))/RATE*2.3)[:,None]
save('cue-low',fade(cue,release=.25),'낮은 전투 예고 종',{'id':'bell'},'Same lowered bell; 1.15s decaying tail for repeated combat cues',gain=.22)
dodge=trim(decode(kenney('rpg','knifeSlice.ogg'),'asetrate=48510,aresample=44100,highpass=f=1600,lowpass=f=6500'))[:round(.18*RATE)]
save('dodge-air',fade(dodge),'회피 바람',{'pack':'Kenney RPG Audio','file':'knifeSlice.ogg'},'Short airy high-pass whoosh, 180ms',gain=.30)
perfect=trim(decode(kenney('impact','impactMetal_light_001.ogg'),'asetrate=50715,aresample=44100,highpass=f=1200'))[:round(.36*RATE)]
save('perfect-parry',fade(perfect,release=.10),'완벽 대응',{'pack':'Kenney Impact Sounds','file':'impactMetal_light_001.ogg'},'Bright short metal confirmation, 360ms maximum',gain=.42)
step=trim(decode(kenney('rpg','footstep03.ogg'),'highpass=f=100,lowpass=f=2600'))[:round(.15*RATE)]
save('footstep-stone',fade(step),'돌길 발소리',{'pack':'Kenney RPG Audio','file':'footstep03.ogg'},'Trimmed 150ms stone/foley transient; 2.6kHz low-pass',gain=.22)
t=np.arange(round(1.3*RATE))/RATE;rng=np.random.default_rng(221026)
lantern=(np.sin(2*np.pi*220*t)*np.exp(-t*3.8)*.30+np.sin(2*np.pi*440*t)*np.exp(-t*6)*.08+rng.normal(0,.025,len(t))*np.exp(-t*12))[:,None]
save('lantern-warm',fade(lantern,attack=.015,release=.18),'등불 점화',{'original':'Last Bell procedural sound, no external sample'},'Warm 220/440Hz body and brief seeded noise, no high bell melody',gain=.38)
for key,source,title,overlap in [
 ('bgm-road','dark-theme','성문 밖 · 긴장',.35),
 ('bgm-sanctuary','shrine','등불 아래 · 휴식',.60),
 ('bgm-boss','boss','성문 가디언 · 보스전',.008),
 ('bgm-quarter','dungeon','성문 안 · 폐허',.50)]:
    s=next(s for s in sources if s['id']==source);x=decode(HERE/'Sources'/s['file'],channels=2)
    x=trim(x);n=round(overlap*RATE);p=np.linspace(0,1,n)[:,None]
    # Wrap tail into head; the end continues directly into the next loop's first sample.
    x=np.concatenate([x[n:-n],x[-n:]*(1-p)+x[:n]*p])
    save(key,x,title,{'id':source},f'Near-silent endpoints trimmed; circular crossfade {overlap}s; fixed gain for RMS -19.6dBFS / peak <=-3.1dBFS; Vorbis q5',kind='music',gain=.34 if source!='boss' else .28)

for pack in ['rpg','impact']:
    shutil.copy2(KENNEY/pack/'License.txt',HERE/(pack+'-LICENSE.txt'))
manifest={'version':'last-bell-audio-v4-clash1','scope':'preview-first; Unity application pending review','date':'2026-10-22','listeningReview':'Playback and signal checks only; final timbre/music suitability requires user audition.','files':records,'sources':sources}
(HERE/'delivery.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
credits=['Last Bell audio preview v4','All external recordings below are CC0 1.0: https://creativecommons.org/publicdomain/zero/1.0/','Edits: pitch/EQ/tail processing for SFX; endpoint trim, loop crossfade, fixed gain and Vorbis conversion for music.','']
credits += [s['title']+' — '+s['creator']+'\n'+s['sourceUrl'] for s in sources]
credits += ['RPG Audio / Impact Sounds — Kenney\nhttps://kenney.nl/assets/rpg-audio\nhttps://kenney.nl/assets/impact-sounds','Lantern sound: original deterministic synthesis for Last Bell.']
(HERE/'CREDITS.txt').write_text('\n\n'.join(credits)+'\n',encoding='utf-8')
(HERE/'validation.json').write_text(json.dumps({'passed':True,'files':len(records),'decodedAll':True,'clippedFiles':0,'unityModified':False,'filesMeasured':[{k:r[k] for k in ['key','durationSeconds','peakDbfs','rmsDbfs']} for r in records]},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'files':len(records),'bellSeconds':next(r['durationSeconds'] for r in records if r['key']=='bell-low-long'),'noClipping':True}))
