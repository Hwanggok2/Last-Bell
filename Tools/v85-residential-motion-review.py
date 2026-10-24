"""Full-frame QA sheets, raw-pixel loop checks and native-frame previews."""
import pathlib,json,hashlib
from PIL import Image,ImageDraw,ImageChops
root=pathlib.Path(__file__).resolve().parents[1]
source=root/'Planning/Story/Monster-v85/residential'
jobs=json.loads((source/'jobs.json').read_text(encoding='utf-8'))['jobs']
report={}
for item in jobs:
    key=item['id'];report[key]={}
    for motion in ['idle','attack','stun']:
        clip=item.get('clips',{}).get(motion)
        if not clip:continue
        paths=sorted((source/key/clip['folder']).glob('*.png'))
        if not paths:continue
        rows=(len(paths)+4)//5;sheet=Image.new('RGB',(5*204,rows*190+30),'#182834');d=ImageDraw.Draw(sheet);d.text((8,8),key+' / '+motion,fill='white')
        frames=[];metrics=[];raw=[]
        for i,path in enumerate(paths):
            im=Image.open(path).convert('RGBA');bbox=im.getbbox();assert im.size==(192,160),(path,im.size)
            raw.append(im);x=i%5*204;y=30+i//5*190;d.text((x+6,y+3),str(i).zfill(2),fill='white');sheet.paste(im,(x+6,y+24),im)
            colors={c[:3] for c in im.getdata() if c[3]};alpha=set(im.getchannel('A').getdata())
            metrics.append({'frame':i,'bounds':bbox,'colors':len(colors),'alphaValues':sorted(alpha),'touchesEdge':bbox[0]==0 or bbox[1]==0 or bbox[2]==192 or bbox[3]==160,'pixelHash':hashlib.sha256(im.tobytes()).hexdigest()})
            display=Image.new('RGB',(576,480),'#182834');zoom=im.resize((576,480),Image.Resampling.NEAREST);display.paste(zoom,(0,0),zoom);frames.append(display)
        info={'frames':metrics,'pixelUniqueFrames':len({f['pixelHash'] for f in metrics}),'firstLastPixelDiff':sum(a!=b for a,b in zip(raw[0].getdata(),raw[-1].getdata()))}
        if motion=='idle':
            baseline=raw[0].getbbox()[3]
            roi=(0,baseline-6,192,baseline)
            info['bottom6pxChanges']=[sum(a!=b for a,b in zip(raw[0].crop(roi).getdata(),im.crop(roi).getdata())) for im in raw]
            info['bottomRows']=[im.getbbox()[3] for im in raw]
        report[key][motion]=info;sheet.save(source/f'{key}-{motion}-frames.png')
        holds=[180]*len(frames) if motion=='idle' else [40]*len(frames) if motion=='attack' else [110]*len(frames)
        if motion=='attack':holds[-1]+=900
        frames[0].save(source/f'{key}-{motion}.gif',save_all=True,append_images=frames[1:],duration=holds,loop=0)
(source/'frame-metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Full-frame sheets, previews and idle seam metrics saved')
