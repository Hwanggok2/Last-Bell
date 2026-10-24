from pathlib import Path
import json, subprocess
import numpy as np
here=Path(__file__).resolve().parent
root=here.parents[2]
ffmpeg='C:/Users/JW/AppData/Roaming/vrew/ffmpeg_gpl_vgx_v3/ffmpeg.exe'
def read(p):
    return np.frombuffer(subprocess.check_output([ffmpeg,'-v','error','-i',str(p),'-f','f32le','-ac','1','-ar','44100','pipe:1']),dtype='<f4')
def spectrum(p):
    a=read(p); power=abs(np.fft.rfft(a*np.hanning(len(a))))**2;hz=np.fft.rfftfreq(len(a),1/44100)
    power[hz<40]=0
    return {'seconds':round(len(a)/44100,4),'powerCentroidHz':round(float(sum(power*hz)/sum(power)),1),'dominantHz':round(float(hz[np.argmax(power)]),1)}
pairs={
    'swordClash':(root/'Client/Assets/Game/Audio/Presentation/knifeSlice.ogg',here/'Audio/sword-clash.wav'),
    'bell':(root/'Client/Assets/Game/Audio/Presentation/critical-bell.wav',here/'Audio/bell-low-long.wav')}
result={k:{'before':spectrum(a),'after':spectrum(b)} for k,(a,b) in pairs.items()}
result['attackSource']='Sabre Norse Sword Blade on Blade.wav — Ben Jaszczak and Brian Nelson; single 0.65s blade collision, original pitch and metallic decay. Timbre suitability requires user audition.'
result['bellLowerAndLonger']=result['bell']['after']['dominantHz']<result['bell']['before']['dominantHz'] and result['bell']['after']['seconds']>result['bell']['before']['seconds']
(here/'comparison.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result))
