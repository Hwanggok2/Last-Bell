using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class DangerGraphic : MaskableGraphic
    {
        // Spatial danger region. This is deliberately separate from the required dodge key.
        public int Region;
        public float Progress;
        public void Show(int region,float progress) { Region=region;Progress=progress;SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();float x=r.center.x,y=r.center.y,w=r.width*.44f,h=r.height*.44f;
            Vector2[] p;
            if(Region==0)p=new[]{new Vector2(x-w,y+h-16),new Vector2(x-w,y+h),new Vector2(x+w,y+h),new Vector2(x+w,y+h-16)};
            else if(Region==2)p=new[]{new Vector2(x-w,y-h+18),new Vector2(x-w*.6f,y-h),new Vector2(x-w*.2f,y-h+18),new Vector2(x+w*.2f,y-h),new Vector2(x+w*.6f,y-h+18),new Vector2(x+w,y-h)};
            else {float side=Region==1?-1:1;p=new[]{new Vector2(x+side*w*.55f,y+h),new Vector2(x+side*w,y),new Vector2(x+side*w*.55f,y-h)};}
            float alpha=color.a,total=0;
            for(int i=0;i<p.Length-1;i++)
            {
                total+=Vector2.Distance(p[i],p[i+1]);
                Line(vh,p[i],p[i+1],10,new Color(.04f,.06f,.1f,alpha));
                Line(vh,p[i],p[i+1],7,new Color(1,.92f,.65f,.6f*alpha));
            }
            // Spatial progress remains readable with muted colors: the bright section grows
            // along the dangerous trajectory and a square marks the approaching impact.
            float remaining=total*Mathf.Clamp01(Progress);Vector2 head=p[0];
            for(int i=0;i<p.Length-1&&remaining>0;i++)
            {
                float length=Vector2.Distance(p[i],p[i+1]);float fraction=Mathf.Min(1,remaining/Mathf.Max(.001f,length));
                head=Vector2.Lerp(p[i],p[i+1],fraction);
                Line(vh,p[i],head,5,new Color(1,.68f,.16f,alpha));remaining-=length;
            }
            Line(vh,head-Vector2.right*4,head+Vector2.right*4,8,new Color(1,.98f,.82f,alpha));
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color col)
        {
            Vector2 d=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int n=vh.currentVertCount;
            vh.AddVert(a-d,col,Vector2.zero);vh.AddVert(a+d,col,Vector2.zero);vh.AddVert(b+d,col,Vector2.zero);vh.AddVert(b-d,col,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
