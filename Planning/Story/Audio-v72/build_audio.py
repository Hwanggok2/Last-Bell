"""Reproducible, attributed combat SFX edits. Float decode avoids MP3 overshoot clipping."""
from pathlib import Path
import array, hashlib, html, json, math, shutil, subprocess, wave

HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];RATE=44100
FFMPEG='C:/Users/JW/AppData/Roaming/vrew/ffmpeg_gpl_vgx_v3/ffmpeg.exe'
OUT=ROOT/'Client/Assets/Game/Audio/Combat-v72';OUT.mkdir(parents=True,exist_ok=True)
LICENSES=ROOT/'Client/Assets/StreamingAssets/AudioLicenses';LICENSES.mkdir(parents=True,exist_ok=True)
sources=json.loads((HERE/'sources.json').read_text(encoding='utf-8'))
settings=[
 ('attack','sword-fire.wav','공격 · 불꽃 검',.285,1.0625,'highpass=f=65',-22,-5,.04),
 ('dodge','dodge-whoosh.wav','일반 회피 · 짧은 바람',.045,.315,'highpass=f=200,lowpass=f=7000',-24,-8,.03),
 ('hit','player-body-hit.wav','피격 · 몸 충격',0,.34,'highpass=f=60,lowpass=f=4200,equalizer=f=170:t=q:w=1:g=3',-23,-5,.04),
]
records=[]
for role,name,label,start,end,filters,rms_target,peak_target,fade_out in settings:
 source=next(s for s in sources if s['role']==role)
 raw=subprocess.check_output([FFMPEG,'-hide_banner','-loglevel','error','-i',str(HERE/'Sources'/source['downloadedFile']),'-af',filters,'-ar',str(RATE),'-ac','2','-f','f32le','pipe:1'])
 x=array.array('f',raw)[round(start*RATE)*2:round(end*RATE)*2]
 frames=len(x)//2;fade_in=round(.002*RATE);release=round(fade_out*RATE)
 for i in range(frames):
  gain=min(1,i/fade_in,(frames-1-i)/release)
  x[2*i]*=gain;x[2*i+1]*=gain
 peak=max(abs(v) for v in x);rms=math.sqrt(sum(v*v for v in x)/len(x))
 gain=min(10**(peak_target/20)/peak,10**(rms_target/20)/rms)
 pcm=array.array('h',(round(v*gain*32767) for v in x))
 target=OUT/name
 with wave.open(str(target),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(pcm.tobytes())
 decoded=[v/32768 for v in pcm];peak=max(abs(v) for v in decoded)
 assert peak<.9 and all(math.isfinite(v) for v in decoded)
 edits=f'Public HQ MP3 preview decoded to stereo PCM WAV, 44.1kHz/16-bit; excerpt {start:.4f}–{end:.4f}s; {filters}; 2ms fade-in, {fade_out*1000:.0f}ms fade-out; fixed gain {gain:.6f}; no pitch shift.'
 records.append(dict(role=role,file=name,label=label,sourceId=source['id'],edits=edits,sha256=hashlib.sha256(target.read_bytes()).hexdigest(),durationSeconds=frames/RATE,peakDbfs=20*math.log10(peak),rmsDbfs=20*math.log10(math.sqrt(sum(v*v for v in decoded)/len(decoded))),peakAtSeconds=max(range(len(decoded)),key=lambda i:abs(decoded[i]))/2/RATE))
manifest=dict(version='combat-audio-v7.2',date='2026-10-23',sources=sources,files=records,qualityNote='Public HQ previews, not original lossless downloads. Signal and runtime playback checked; subjective timbre remains user-reviewable.')
text=['마지막 종지기 · 음원 출처 / Audio credits','2026-10-23 · Combat audio v7.2','']
for r in records:
 s=next(s for s in sources if s['id']==r['sourceId'])
 text += [r['label'],s['title']+' — '+s['creator'],('Copyright protected. © SonoFxAudio.' if s['role']=='attack' else 'CC0 public-domain dedication.'),s['sourceUrl'],s['license']+' · '+s['licenseUrl'],'Changes: '+r['edits'],'']
text += ['CC BY 4.0 permits commercial use subject to attribution. No endorsement by the creators is implied. No additional restrictions are imposed on these CC-licensed audio assets. The CC BY sound and our edits are available under CC BY 4.0. License texts are bundled in StreamingAssets/AudioLicenses.','', '기존 배경 음악·발소리·종소리 등 / Retained audio sources (CC0)','']
old=json.loads((ROOT/'Client/Assets/Game/Audio/Reviewed-v4/sources.json').read_text(encoding='utf-8'))
for pack,url in [('RPG Audio','https://kenney.nl/assets/rpg-audio'),('Impact Sounds','https://kenney.nl/assets/impact-sounds')]:
 old.append(dict(id='kenney-'+pack.lower().replace(' ','-'),title=pack,creator='Kenney',sourceUrl=url,license='CC0-1.0',licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/'))
manifest['retainedSources']=old
for s in old:
 text += [s['title']+' — '+s['creator'],s['sourceUrl'],s['license']+' · '+s['licenseUrl'],'']
text += ['Legacy CC0 edits: pitch/EQ/tail processing for SFX; endpoint trim, loop crossfade, gain and Vorbis conversion for music. Lantern sound: original synthesis for Last Bell.','']
credits='\n'.join(text)
ui=[]
for r in records:
 s=next(s for s in sources if s['id']==r['sourceId'])
 ui += [r['label'],s['title']+' — '+s['creator']]
 if s['role']=='attack':ui += ['© SonoFxAudio. Copyright protected.']
 ui += [s['license'],s['sourceUrl'],s['licenseUrl'],'편집: 사용 구간 선택, EQ·음량·페이드 조정, WAV 변환.','']
ui += ['공격음 저작권: © SonoFxAudio.','CC BY 4.0에 따라 사용하며, 원작자의 후원을 의미하지 않습니다. 해당 음원과 편집본에 추가 제한을 부과하지 않습니다.','', '기존 음악·효과음 (CC0)','']
for s in old:
 ui += [s['title']+' — '+s['creator'],s['sourceUrl'],'']
ui += ['CC0 1.0: https://creativecommons.org/publicdomain/zero/1.0/','기존 음원 편집: 음높이·EQ·구간·음량·반복 연결 조정.','등불 점화음: Last Bell 자체 합성.']
for dest in [HERE,OUT,LICENSES]:
 (dest/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 (dest/'CREDITS.txt').write_text(credits,encoding='utf-8')
 (dest/'UI_CREDITS.txt').write_text('\n'.join(ui)+'\n',encoding='utf-8')
for name in ['CC-BY-4.0.txt','CC0-1.0.txt']:shutil.copy2(HERE/'Sources'/name,LICENSES/name)
for name in ['rpg-LICENSE.txt','impact-LICENSE.txt']:shutil.copy2(ROOT/'Client/Assets/Game/Audio/Reviewed-v4'/name,LICENSES/name)
cards=[]
for r in records:
 s=next(s for s in sources if s['id']==r['sourceId'])
 cards.append('<section><h2>'+r['label']+'</h2><audio controls preload="none" src="../../../Client/Assets/Game/Audio/Combat-v72/'+r['file']+'"></audio><p>'+html.escape(s['title']+' — '+s['creator'])+'</p><p><a href="'+s['sourceUrl']+'">원본 페이지</a> · <a href="'+s['licenseUrl']+'">'+s['license']+'</a></p><small>'+html.escape(r['edits'])+'</small></section>')
(HERE/'preview.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><title>전투 효과음 v7.2</title><style>body{margin:40px auto;max-width:850px;padding:24px;background:#101d29;color:#f1dfb9;font-family:Malgun Gothic,sans-serif;line-height:1.7}section{padding:20px;border:1px solid #65747b;margin:20px 0}a{color:#eabd63}audio{width:100%}small{color:#b2c2c8}</style><h1>공격 · 회피 · 피격</h1><p>사용자가 고른 불꽃 검에 짧은 바람과 몸 충격을 조합했습니다. 공개 HQ 미리듣기 파일을 사용했으며 무손실 원본은 아닙니다. 완벽 회피의 종소리는 유지합니다.</p>'+''.join(cards)+'<p><a href="CREDITS.txt">전체 음원 출처·편집 내역</a></p></html>',encoding='utf-8')
print(json.dumps([{k:r[k] for k in ['role','file','durationSeconds','peakDbfs','rmsDbfs','peakAtSeconds']} for r in records],indent=2))
