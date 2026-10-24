using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StoryDecorGraphic : MaskableGraphic
    {
        public bool Lamp;
        float lightTime;
        public void SetLight(float time){if(!Lamp)return;lightTime=time;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(Lamp)
            {
                var c=r.center;float u=Mathf.Min(r.width,r.height)/120;
                float strength=.18f+.018f*Mathf.Sin(lightTime*2.1f)+.01f*Mathf.Sin(lightTime*5.3f);
                int n=vh.currentVertCount;vh.AddVert(c,new Color(1,.65f,.19f,strength),Vector2.zero);
                for(int i=0;i<=48;i++){float a=i*Mathf.PI/24;vh.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r.width*.48f,new Color(1,.58f,.12f,0),Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}
                System.Action<float,float,float,float,Color> box=(x,y,w,h,col)=>PixelVfxMesh.Rect(vh,new Rect(c.x+x*u,c.y+y*u,w*u,h*u),col);
                var bronze=new Color32(121,88,43,255);var gold=new Color32(225,172,77,255);
                box(-1,16,2,30,bronze);box(-10,12,20,4,gold);box(-8,-14,16,26,bronze);
                box(-5,-10,10,19,new Color32(244,176,57,255));box(-2,-7,4,12,new Color32(255,240,170,255));
                box(-11,-18,22,4,gold);box(-9,16,18,3,bronze);return;
            }
            var paper=new Color32(226,206,163,255);var dark=new Color32(98,70,43,255);var edge=new Color32(179,143,93,255);
            PixelVfxMesh.Rect(vh,new Rect(r.x+8,r.y-7,r.width-16,r.height),new Color(0,0,0,.3f));
            PixelVfxMesh.Rect(vh,r,paper);
            PixelVfxMesh.Rect(vh,new Rect(r.x,r.y,8,r.height),edge);PixelVfxMesh.Rect(vh,new Rect(r.xMax-8,r.y,8,r.height),edge);
            foreach(float y in new[]{r.yMin,r.yMax-22})
            {
                PixelVfxMesh.Rect(vh,new Rect(r.x-15,y,r.width+30,22),dark);
                PixelVfxMesh.Rect(vh,new Rect(r.x-8,y+4,r.width+16,17),edge);
                PixelVfxMesh.Rect(vh,new Rect(r.x,y+10,r.width,7),new Color32(243,223,182,255));
                PixelVfxMesh.Rect(vh,new Rect(r.x-20,y-3,9,27),dark);PixelVfxMesh.Rect(vh,new Rect(r.xMax+11,y-3,9,27),dark);
            }
            for(int i=0;i<6;i++)PixelVfxMesh.Rect(vh,new Rect(r.x+16+i%2*5,r.y+36+i*(r.height-72)/6,2,10),new Color( .48f,.32f,.16f,.18f));
        }
    }
}
