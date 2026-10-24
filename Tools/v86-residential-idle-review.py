import pathlib,json,hashlib
from PIL import Image,ImageDraw
P=pathlib.Path(__file__).resolve().parents[1]/'Planning/Story/Monster-v86/residential'
jobs=json.loads((P/'jobs.json').read_text(encoding='utf-8'))['jobs'];report={}
for j in jobs:
    paths=sorted((P/j['id']/j.get('folder','idle')).glob('*.png'))
    if not paths:continue
    raw=[Image.open(p).convert('RGBA') for p in paths];base=Image.open(P/j['id']/'base.png').convert('RGBA');floor=base.getbbox()[3]-1
    sheet=Image.new('RGB',(5*272,((len(paths)+4)//5)*294+30),'#192c36');d=ImageDraw.Draw(sheet);d.text((8,8),j['id']+' native Idle candidates x2',fill='white')
    metrics=[]
    for i,im in enumerate(raw):
        x=i%5*272;y=i//5*294+30;d.text((x+8,y),str(i),fill='#f9d296');up=im.resize((256,256),Image.Resampling.NEAREST);sheet.paste(up,(x+8,y+20),up)
        alpha=im.getchannel('A');a0=base.getchannel('A');metrics.append({'sourceFrame':i,'bounds':im.getbbox(),'edgeClipped':not(0<im.getbbox()[0] and 0<im.getbbox()[1] and im.getbbox()[2]<128 and im.getbbox()[3]<128),'alphaValues':sorted(set(alpha.getdata())),'pixelHash':hashlib.sha256(im.tobytes()).hexdigest(),'floorY':floor,'floorAlphaDiff':sum(a0.getpixel((x,floor))!=alpha.getpixel((x,floor)) for x in range(128)),'bottom6AlphaDiff':sum(a0.getpixel((x,y))!=alpha.getpixel((x,y)) for x in range(128) for y in range(floor-5,floor+1))})
    sheet.save(P/f'{j["id"]}-idle-candidates.png');report[j['id']]=metrics
(P/'idle-raw-metrics.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Idle candidate sheets and stationary-floor metrics saved.')
