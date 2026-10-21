using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Saved world-space canvas behind the actors. The Core owns this same geometry.
    public sealed class EnemyTelegraphGraphic : MaskableGraphic
    {
        EnemyThreatView[] threats=System.Array.Empty<EnemyThreatView>();
        double clock;bool reduced,tutorial;
        static readonly Color Gold=new Color32(233,184,90,255);
        public void Sample(GameSession session,bool reducedMotion)
        {
            threats=session.EnemyThreats;clock=session.ClockMs;reduced=reducedMotion;tutorial=session.Phase==SessionPhase.TutorialDodge;
            SetVerticesDirty();
        }
        public static Vector2 Point(float x,float y)=>new Vector2(x-480,269.5f-y);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach(var t in threats)
            {
                if(clock<t.CueAtMs||clock>t.ImpactAtMs)continue;
                bool flashing=clock<t.CueEndMs;
                float phase=(float)((clock-t.CueAtMs)/(t.CueEndMs-t.CueAtMs)*3),beat=phase-Mathf.Floor(phase);
                bool bright=beat<.5f;var b=t.Bounds;var p=Point(b.X,b.Y+b.Height);var r=new Rect(p.x,p.y,b.Width,b.Height);
                float fill=tutorial?.12f:!flashing?0:reduced?.12f:bright?.20f:.04f;
                float edge=tutorial?.62f:!flashing?.14f:reduced?.62f:bright?.62f:.20f;
                PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Alpha(Gold,fill));Border(vh,r,2,PixelVfxMesh.Alpha(Gold,edge));
                if(reduced&&flashing&&!tutorial)
                {
                    float inset=(1-beat)*7;var c=PixelVfxMesh.Alpha(Gold,.75f);
                    foreach(float x in new[]{r.xMin+inset,r.xMax-inset})foreach(float y in new[]{r.yMin+inset,r.yMax-inset})
                    {
                        float dx=x<r.center.x?9:-9,dy=y<r.center.y?9:-9;
                        PixelVfxMesh.Line(vh,new Vector2(x,y),new Vector2(x+dx,y),2,c);PixelVfxMesh.Line(vh,new Vector2(x,y),new Vector2(x,y+dy),2,c);
                    }
                }
            }
        }
        static void Border(VertexHelper vh,Rect r,float w,Color c)
        {
            PixelVfxMesh.Rect(vh,new Rect(r.xMin,r.yMin,r.width,w),c);PixelVfxMesh.Rect(vh,new Rect(r.xMin,r.yMax-w,r.width,w),c);
            PixelVfxMesh.Rect(vh,new Rect(r.xMin,r.yMin,w,r.height),c);PixelVfxMesh.Rect(vh,new Rect(r.xMax-w,r.yMin,w,r.height),c);
        }
    }
}
