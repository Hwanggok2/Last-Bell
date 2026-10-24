"""Render planning-only UI diagrams, never imports or changes Unity assets."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import html, base64, json

B=Path(__file__).resolve().parent
ROOT=B.parents[3]
FONT=ROOT/'Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf'
INK='#273d45'; PAPER='#fff1d1'; GREEN='#315e4d'; GOLD='#efc36d'; LIGHT='#fff9e9'

class Scene:
    def __init__(self, gray=False):
        self.gray=gray; self.im=Image.new('RGB',(1280,720),PAPER); self.d=ImageDraw.Draw(self.im); self.svg=[]
    def color(self,c):
        if not self.gray:return c
        if c in (INK,GREEN):return '#383838'
        if c==LIGHT:return '#ffffff'
        return '#dedede' if c==PAPER else '#a9a9a9'
    def rect(self,x,y,w,h,fill,stroke=None,width=4):
        fill=self.color(fill); stroke=self.color(stroke) if stroke else None
        self.d.rectangle((x,y,x+w,y+h),fill=fill,outline=stroke,width=width)
        self.svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="{stroke or "none"}" stroke-width="{width}"/>')
    def text(self,x,y,s,size=32,fill=INK):
        fill=self.color(fill);font=ImageFont.truetype(str(FONT),size)
        self.d.text((x,y),s,font=font,fill=fill,anchor='lt')
        self.svg.append(f'<text x="{x}" y="{y+size*.83}" font-size="{size}" fill="{fill}">{html.escape(s)}</text>')
    def img(self,path,x,y,size):
        path=Path(path)
        if not path.exists():
            self.rect(x,y,size,size,'#b5c4a5');self.text(x+8,y+size//2,'초상',24);return
        im=Image.open(path).convert('RGBA')
        if self.gray:
            alpha=im.getchannel('A');im=im.convert('L').convert('RGBA');im.putalpha(alpha)
        self.im.paste(im.resize((size,size),Image.Resampling.NEAREST),(x,y),im.resize((size,size),Image.Resampling.NEAREST))
        self.svg.append(f'<image x="{x}" y="{y}" width="{size}" height="{size}" href="data:image/png;base64,{base64.b64encode(path.read_bytes()).decode()}" style="image-rendering:pixelated"/>')
    def panel(self,x,y,w,h):
        self.rect(x+4,y+4,w,h,INK); self.rect(x,y,w,h,PAPER,INK);self.rect(x+6,y+6,w-12,4,GOLD)
    def button(self,x,y,w,h,label,active=False):
        self.rect(x,y,w,h,GREEN if active else LIGHT,INK);self.text(x+16,y+(h-32)//2,label,32,PAPER if active else INK)
    def save(self,name):
        self.im.save(B/(name+'.png'))
        fonturl='../'+'../'*3+'Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf'
        (B/(name+'.svg')).write_text('<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="720" viewBox="0 0 1280 720"><style>@font-face{font-family:Pixel;src:url("'+fonturl+'")}text{font-family:Pixel,monospace}</style>'+''.join(self.svg)+'</svg>',encoding='utf-8')

def portrait(who):
    p=B/'portraits'/f'{who}.png'
    return p if p.is_file() else B.parent/'Production/Portraits-v1'/f'{who}.png'

def landscape(s,who='on'):
    s.rect(0,0,1280,480,'#88c8cd')
    for x,y,w,h in [(60,80,230,32),(90,48,140,32),(850,100,280,32),(900,68,170,32)]:s.rect(x,y,w,h,LIGHT)
    for x,y,w,h in [(0,240,200,240),(160,208,180,272),(340,280,260,200),(630,224,160,256),(780,192,280,288),(1040,270,240,210)]:s.rect(x,y,w,h,'#7ca78f')
    for x,y,w,h in [(0,370,140,110),(210,330,70,150),(800,370,200,110),(1120,340,160,140)]:s.rect(x,y,w,h,'#4d7960')
    s.rect(0,468,1280,12,GREEN);s.rect(0,480,1280,20,'#b98a53')
    actor=B.parent/'Production/Player-v4/miel/clips/Idle/00.png' if who=='rin' else B.parent/'Production/Player-v3/canonical-a-outline.png'
    s.img(actor,410,294,192)

def dialogue(gray=False,who='tori'):
    s=Scene(gray);landscape(s,who)
    s.panel(24,24,240,96);s.text(48,56,'Stage 2',40)
    s.button(1104,24,152,96,'건너뛰기')
    s.panel(24,488,1232,208)
    player=who in ('on','dam','rin')
    name={'tori':'토리','on':'루카','dam':'다렌','rin':'미엘'}[who]
    name_x=216 if player else 48
    s.panel(name_x,440,176,56);s.text(name_x+24,452,name,36)
    s.img(portrait(who),48 if player else 1056,384,144)
    lines=('그럼 길 안내는 부탁할게.','용을 만나면 내가 앞에 설게!') if player else ('숲길은 내가 안내할게.','대신 무거운 짐은 네가 들어 줘!')
    s.text(56,532,lines[0],32)
    s.text(56,580,lines[1],32)
    s.button(1104,568,128,96,'다음')
    scene='dialogue-player' if player else 'dialogue'
    s.save(scene+('-wireframe' if gray else '-draft'))

def inn(gray=False):
    s=Scene(gray);s.rect(0,0,1280,720,'#e7d9b8')
    s.text(32,24,'여관',44);s.text(880,32,'골드  2,480',28);s.text(1110,32,'보석  12',28)
    for x,label in [(24,'성장'),(204,'동료'),(384,'유물')]:s.button(x,88,160,96,label,label=='동료')
    s.panel(24,208,376,488);s.text(48,228,'동행  1 / 2',28)
    s.img(portrait('tori'),48,272,96);s.rect(160,272,96,96,LIGHT,INK);s.text(192,300,'+',40)
    s.rect(272,272,96,96,'#d0c6ae',INK);s.text(288,304,'잠김',24)
    for y,who,label in [(384,'tori','토리  Lv.1'),(484,'barun','바룬 Lv.1'),(584,'doto','도토  미발견')]:
        s.rect(40,y,344,96,GREEN if who=='tori' else LIGHT,INK)
        if who!='doto':s.img(portrait(who),48,y+8,72)
        s.text(136,y+28,label,28,PAPER if who=='tori' else INK)
    s.panel(424,208,832,488);s.img(portrait('tori'),456,232,144)
    s.text(632,244,'토리',44);s.text(632,300,'숲길을 아는 여우 길잡이',28)
    s.text(456,406,'지원 사격  ·  6초마다 화살',32)
    s.text(456,454,'길잡이  ·  클리어 골드 +3%',28)
    s.text(456,506,'Lv.1   피해: 공격력의 24%',28)
    s.button(456,572,264,96,'강화  20 G')
    s.button(752,572,200,96,'동행 해제')
    s.button(984,572,240,96,'Stage 21',True)
    s.save('inn-wireframe' if gray else 'inn-draft')

def growth(gray=False):
    s=Scene(gray);s.rect(0,0,1280,720,'#e7d9b8')
    s.text(32,24,'여관',44);s.text(880,32,'골드  2,480',28);s.text(1110,32,'보석  12',28)
    for x,label in [(24,'성장'),(204,'동료'),(384,'유물')]:s.button(x,88,160,96,label,label=='성장')
    s.panel(24,192,304,504);s.text(48,212,'루카',40)
    s.img(B.parent/'Production/Player-v3/canonical-a-outline.png',56,276,224)
    s.text(48,526,'바람 베기',28);s.button(48,572,256,96,'Stage 21',True)
    for i,(label,level,val,stars) in enumerate([('공격력',10,'40',3),('체력',10,'260',3),('행운',5,'5',2),('기세',5,'5',3),('회복력',0,'5 HP',3)]):
        y=192+i*100;s.panel(352,y,880,96)
        s.text(372,y+14,label,30);s.text(372,y+54,'Lv.'+str(level),24)
        for j in range(5):
            for row,line in enumerate(['0001000','0011100','1111111','0111110','0011100','0110110','1100011']):
                for col,bit in enumerate(line):
                    if bit=='1':s.rect(520+j*28+col*3,y+16+row*3,3,3,GOLD if j<stars else '#b7ad94')
        for j in range(10):s.rect(680+j*24,y+44,16,24,GREEN if level and (level%10==0 or j<level%10) else LIGHT,INK,2)
        s.text(952,y+30,val,32)
        s.button(1116,y,116,96,'+')
    s.save('growth-wireframe' if gray else 'growth-draft')

def rhythm():
    s=Scene();s.rect(0,0,1280,720,LIGHT);s.text(40,30,'공격 직전, 뒤로 한 걸음.',48)
    s.text(40,104,'몸을 보고 박자를 읽고, 방향 예고로 대응을 고른다.',28)
    frames=[('01  뒤로 물러남','체형에 따라 24~48px'),('02  힘을 모음','발을 고정하고 방향 예고'),('03  타격','예고 끝 = 판정 순간'),('04  되돌아옴','다음 타가 있으면 짧게 재준비')]
    for i,(a,b) in enumerate(frames):
        x=32+i*312;s.panel(x,176,288,320);s.text(x+16,196,a,28)
        s.rect(x+16,424,256,8,GREEN)
        dx=[28,28,-32,0][i]
        for px,py,pw,ph in [(x+102+dx,304 if i==1 else 286,64,64),(x+90+dx,350,88,56),(x+90+dx,406,24,18),(x+150+dx,406,24,18)]:s.rect(px,py,pw,ph,'#ce8555',INK)
        if i==0:s.text(x+220,312,'>',44)
        if i==1:s.text(x+40,300,'!',52,'#a14032')
        if i==2:s.rect(x+26,360,88,12,GOLD,INK)
        s.text(x+12,460,b,18)
    s.text(48,544,'첫 2연타 · Stage 5 큰뿔토끼',36)
    s.text(48,604,'위로 피해!     >     한 번 더! 아래로 쳐내!',32)
    s.text(48,660,'안내 중에는 타격 순간 시간이 멈추고, 올바른 새 입력에만 다음으로 넘어갑니다.',22)
    s.save('rhythm-draft')

if __name__=='__main__':
    import sys
    for gray in [True,False]:
        dialogue(gray);dialogue(gray,'rin')
        if '--dialogue-only' not in sys.argv:inn(gray);growth(gray)
    if '--dialogue-only' not in sys.argv:rhythm()
    print('Planning wireframes rendered; Unity assets unchanged.')
