using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RelicIconGraphic : MaskableGraphic
    {
        [SerializeField] int relic;
        public bool DragonTheme;
        public void SetRelic(int value){if(relic==value)return;relic=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float unit=Mathf.Min(r.width,r.height)/32;
            var center=r.center;Color gold=new Color32(231,183,86,255),light=new Color32(255,234,168,255),shadow=new Color32(104,80,49,255);
            if(DragonTheme){gold=DragonUiPalette.Gold;light=DragonUiPalette.GoldLight;shadow=DragonUiPalette.Ink;}
            System.Action<float,float,float,float,Color> box=(x,y,w,h,c)=>PixelVfxMesh.Rect(vh,new Rect(center.x+x*unit,center.y+y*unit,w*unit,h*unit),c);
            if(DragonTheme&&(relic==2||relic==3))
            {
                for(int y=-11;y<=11;y++)
                {
                    float half=12-Mathf.Abs(y)/2;
                    box(-half,y,half*2,1,shadow);box(-half+2,y,half*2-4,1,relic==2?DragonUiPalette.Success:DragonUiPalette.Gold);
                }
                box(-2,-6,4,12,light);box(-6,-2,12,4,light);return;
            }
            if(relic==0||relic==7||relic==8)
            {
                for(int i=0;i<16;i++){float a=i*Mathf.PI/8;box(Mathf.Round(Mathf.Cos(a)*9)-2,Mathf.Round(Mathf.Sin(a)*9)-2,4,4,i<8?gold:shadow);}
                box(-2,7,4,5,light);if(relic==7){box(-8,-2,16,4,gold);box(-2,-8,4,16,gold);}if(relic==8){box(-2,-2,4,4,light);box(-13,0,3,3,light);}
            }
            else if(relic==1||relic==9)
            {
                for(int y=-11;y<=11;y++)box(-Mathf.Max(1,9-Mathf.Abs(y)),y,Mathf.Max(1,9-Mathf.Abs(y))*2,1,y>0?light:gold);
                box(-1,-6,2,15,light);if(relic==9){box(-12,-12,24,3,shadow);box(-12,10,24,3,shadow);}
            }
            else if(relic==2||relic==3)
            {
                box(-1,1,2,12,shadow);box(-4,8,8,3,gold);box(-7,-9,14,10,gold);box(-4,-12,8,3,shadow);box(-4,-6,4,5,light);
                if(relic==3){box(-11,1,3,4,light);box(8,4,3,4,light);}
            }
            else if(relic==4){box(-2,-13,4,20,shadow);box(-11,4,21,8,gold);box(-9,8,16,4,light);box(7,1,4,7,gold);}
            else if(relic==5){for(int y=-11;y<11;y++)box(-10+y/5f,y,20-y/2f,1,y%4==0?shadow:gold);box(-3,7,6,5,light);}
            else {box(-8,-12,16,4,shadow);box(-5,-8,10,12,gold);box(-2,4,4,6,shadow);box(-1,8,2,4,light);box(-3,-4,3,6,light);}
        }
    }
}
