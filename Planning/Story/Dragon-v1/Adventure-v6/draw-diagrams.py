"""Code-native UI diagrams using existing pixel assets. Not game screenshots."""
from pathlib import Path
import importlib.util,json,sys
sys.dont_write_bytecode=True
B=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('v5_diagram_helpers',B.parent/'Adventure-v5/draw-wireframes.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);m.B=B
class Scene(m.Scene):
 def text(self,x,y,s,size=32,fill=m.INK):super().text(x,y,s.replace('→','->'),size,fill)
PAPER=m.PAPER;INK=m.INK;GREEN=m.GREEN;GOLD=m.GOLD;LIGHT=m.LIGHT
portraits=B.parent/'Adventure-v5/portraits'
def star(s,x,y,color):
 for row,line in enumerate(['0001000','0011100','1111111','0111110','0011100','0110110','1100011']):
  for col,v in enumerate(line):
   if v=='1':s.rect(x+col*2,y+row*2,2,2,color)
def header(s,title,party=False):
 s.rect(0,0,1280,720,'#e7d9b8');s.button(20,8,144,96,'뒤로');s.text(360,30,title,44)
 if party:s.text(704,36,'2,480 G   ◆ 12',30)
 else:s.text(820,28,'◆',36);s.text(864,30,'12',36)
 s.button(1096,8,160,96,'동료',party)
def growth():
 s=Scene();header(s,'업그레이드');s.panel(20,112,320,480)
 for i,title in enumerate(['공격력','체력','행운','기세','회복력']):
  y=112+i*96;s.rect(28,y,304,96,GREEN if i==0 else LIGHT,INK);s.text(44,y+16,title,32,PAPER if i==0 else INK);s.text(44,y+56,'Lv.10',24,GOLD if i==0 else GREEN)
  for n in range(3):star(s,136+n*20,y+60,GOLD if i==0 else GREEN)
 s.panel(360,112,900,480);s.text(392,144,'공격력',40);s.text(392,212,'공격 피해량 증가',28)
 s.button(1036,128,96,96,'+1',True);s.button(1148,128,96,96,'최대')
 s.text(408,310,'현재 능력치  →  다음 능력치',36);s.text(408,398,'강화 비용',32);s.text(408,452,'소지 골드',28);s.text(1040,452,'2,480 G',28)
 s.button(20,608,280,96,'유물 / 교환');s.button(560,608,176,96,'강화',True);s.button(984,608,272,96,'Stage 11')
 s.save('growth-layout')
def companions():
 s=Scene();header(s,'동료',True);s.panel(24,112,376,584);s.text(48,128,'동행 1 / 2',28)
 s.img(portraits/'tori.png',48,176,96);s.rect(160,176,96,96,LIGHT,INK);s.text(192,202,'+',40);s.rect(272,176,96,96,'#c8bea6',INK);s.text(286,210,'잠김',24)
 for i,(id,name) in enumerate([('tori','토리'),('barun','바룬'),('doto','도토'),('rina','리나')]):
  y=288+100*i;s.rect(40,y,344,96,GREEN if i==0 else LIGHT,INK);s.img(portraits/f'{id}.png',48,y+12,72);s.text(136,y+30,name+'  Lv.1',28,PAPER if i==0 else INK)
 s.panel(424,112,832,584);s.img(B.parent/'Production/Companions-v5/tori/canonical.png',456,136,128)
 s.text(632,136,'토리  Lv.1',40);s.text(632,194,'짧은 활',28);s.text(632,242,'기대 DPS   현재 → 다음',28);s.text(632,284,'주기  6.00초 → 5.94초',28)
 s.text(456,328,'쌍쏘기',32);s.text(456,368,'기본 타격의 180%  ·  판정 1회',28);s.text(456,414,'발동률  12% → 13%',28)
 s.button(456,468,768,96,'길잡이  ·  클리어 골드 +3%')
 s.button(456,576,264,96,'강화 20 G',True);s.button(752,576,200,96,'동행 해제');s.button(984,576,240,96,'Stage 11');s.save('companions-layout')
DIGITS={'1':['00100','01100','00100','00100','00100','00100','01110'],'2':['01110','10001','00001','00010','00100','01000','11111'],'4':['00010','00110','01010','10010','11111','00010','00010'],'8':['01110','10001','10001','01110','10001','10001','01110']}
def number(s,x,y,text,cell,color):
 points=[]
 for n,char in enumerate(text):
  for yy,row in enumerate(DIGITS[char]):
   for xx,v in enumerate(row):
    if v=='1':points.append((x+(n*6+xx)*cell,y+yy*cell))
 for xx,yy in points:s.rect(xx-cell,yy-cell,cell*3,cell*3,'#142932')
 for xx,yy in points:s.rect(xx,yy,cell,cell,color)
def bar(s,x,y,width,fill=.68):
 cell=4;cols=width//cell
 for row in range(11):
  for col in range(cols):
   if (row in (0,10) and col in (0,1,cols-2,cols-1)) or (row in (1,9) and col in (0,cols-1)):continue
   edge=row in (0,1,9,10) or col<2 or col>=cols-2
   filled=col>=11 and col-11<max(1,int((cols-13)*fill))
   color='#142932' if edge else ('#D2EF9C' if row==2 and filled else '#A8D16B' if filled else '#65434B')
   s.rect(x+col*cell,y+row*cell,cell,cell,color)
 for row,line in enumerate(['0110110','1111111','1111111','0111110','0011100','0001000']):
  for col,v in enumerate(line):
   if v=='1':s.rect(x+(col+3)*cell,y+(row+2)*cell,cell,cell,'#F4D3BA' if row<2 else '#E08C89')
def hud():
 s=Scene();s.rect(0,0,1280,720,'#263E49');s.text(40,28,'HP / DAMAGE · 정수 도트 격자',40,'#fff4d9')
 s.text(52,130,'플레이어 208×44',28,'#fff4d9');bar(s,52,184,208)
 s.text(376,130,'일반 적 224×44',28,'#fff4d9');bar(s,376,184,224)
 s.text(720,130,'보스 560×44',28,'#fff4d9');bar(s,680,184,560)
 s.text(52,320,'일반 6px 셀',28,'#fff4d9');number(s,64,388,'124',6,'#FFF3D6')
 s.text(376,320,'동료 6px 셀',28,'#fff4d9');number(s,388,388,'124',6,'#8DCEE5')
 s.text(720,320,'치명타 8px 셀',28,'#fff4d9');number(s,732,376,'248',8,'#F3CC61')
 s.text(52,540,'5×7 bitmap  ·  어두운 1셀 외곽',32,'#fff4d9');s.text(52,594,'0.85초 동안 우측 상단 이동  ·  동시 최대 3 lane',28,'#fff4d9')
 s.text(52,660,'시각 규격 설명도 · 실제 Unity 캡처 아님',24,'#c7d2c2');s.save('hud-spec')
if __name__=='__main__':growth();companions();hud();print('Three v6 diagrams generated; existing v5 source files untouched.')
