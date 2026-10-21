using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    public sealed class EnemyImpactGraphic : MaskableGraphic
    {
        readonly double[] starts={-10000,-10000};readonly Vector2[] points=new Vector2[2];readonly bool[] parries=new bool[2];double clock;bool reduced;
        public void Confirm(SessionEvent e){int i=starts[0]<=starts[1]?0:1;starts[i]=e.TimeMs;parries[i]=e.Kind==SessionEventKind.Parried||e.Kind==SessionEventKind.PerfectParry;points[i]=parries[i]?EnemyTelegraphGraphic.Point(420,332):EnemyTelegraphGraphic.Point(EnemyEffectBurst.Contact(e.PatternId).x,440);}
        public void Clear(){starts[0]=starts[1]=-10000;SetVerticesDirty();}
        public void Sample(double time,bool reduce){clock=time;reduced=reduce;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for(int i=0;i<2;i++)
            {
                float t=(float)(clock-starts[i])/300;if(t<0||t>=1)continue;
                if(parries[i])
                {
                    float age=t*300;var p=points[i];var color=PixelVfxMesh.Alpha(PixelVfxMesh.Cream,1-age/180);
                    if(age<100){PixelVfxMesh.Line(vh,p-new Vector2(21,21),p+new Vector2(21,21),4,color);PixelVfxMesh.Line(vh,p+new Vector2(-21,21),p+new Vector2(21,-21),4,color);}
                    if(!reduced&&age<140){var head=p+new Vector2(100,120)*(age/140);PixelVfxMesh.Line(vh,head-new Vector2(25,30),head,5,color);}
                    continue;
                }
                if(reduced)continue;
                float radius=12+75*t;var c=PixelVfxMesh.Alpha(PixelVfxMesh.Cream,(1-t)*.55f);
                for(int j=0;j<40;j++){float a=j*Mathf.PI/20,b=(j+1)*Mathf.PI/20;PixelVfxMesh.Line(vh,points[i]+new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.2f),points[i]+new Vector2(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius*.2f),1.5f,c);}
            }
        }
    }
}
