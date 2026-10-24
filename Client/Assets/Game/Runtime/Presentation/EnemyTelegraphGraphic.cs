using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Saved world-space canvas behind the actors. The Core owns this same geometry.
    public sealed class EnemyTelegraphGraphic : MaskableGraphic
    {
        public bool DragonTheme;
        [Range(.6f,1f)] public float WarningOpacity=.92f;
        [Range(2f,8f)] public float WarningStroke=4f;
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
            EnemyThreatView active=null;
            foreach(var threat in threats)if(!threat.Resolved&&clock>=threat.CueAtMs&&clock<=threat.ImpactAtMs&&(active==null||threat.ImpactAtMs<active.ImpactAtMs))active=threat;
            foreach(var t in threats)
            {
                if(clock<t.CueAtMs||clock>t.ImpactAtMs)continue;
                if(DragonTheme&&t!=active)continue;
                bool flashing=clock<t.CueEndMs;
                float phase=(float)((clock-t.CueAtMs)/(t.CueEndMs-t.CueAtMs)*3),beat=phase-Mathf.Floor(phase);
                bool bright=beat<1f/3f;var b=t.Bounds;var p=Point(b.X,b.Y+b.Height);var r=new Rect(p.x,p.y,b.Width,b.Height);
                float fill=tutorial?.12f:!flashing?0:reduced?.12f:bright?.20f:.04f;
                float edge=tutorial?.62f:!flashing?.14f:reduced?.62f:bright?.62f:.20f;
                if(DragonTheme)
                {
                    // Keep the authored danger bounds and Core clock. The warning stays
                    // readable after the three cue beats, right up to the actual impact.
                    float opacity=WarningOpacity;
                    bool imminent=t.ImpactAtMs-clock<=120;
                    Color warning=imminent?DragonUiPalette.Light:DragonUiPalette.Warning;
                    PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Alpha(DragonUiPalette.Warning,.15f));
                    Border(vh,r,WarningStroke+3,PixelVfxMesh.Alpha(DragonUiPalette.Ink,opacity));
                    var inner=new Rect(r.x+2,r.y+2,r.width-4,r.height-4);
                    Border(vh,inner,WarningStroke,PixelVfxMesh.Alpha(warning,opacity));
                    WarningMarks(vh,r,t.PatternId,warning,opacity);
                    continue;
                }
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
        static void WarningMarks(VertexHelper vh,Rect r,string pattern,Color colour,float opacity)
        {
            // Spatial pixel chevrons supplement colour, including silent/reduced-motion play.
            Vector2 direction=pattern=="low_sweep"?Vector2.up:pattern=="high_sweep"?Vector2.down:Vector2.left;
            Vector2 tangent=new Vector2(-direction.y,direction.x);
            Vector2 start=pattern=="low_sweep"?new Vector2(r.center.x,r.yMin+13):pattern=="high_sweep"?new Vector2(r.center.x,r.yMax-13):new Vector2(r.xMax-18,r.center.y);
            for(int i=-1;i<=1;i++)
            {
                Vector2 p=start+tangent*(i*24);
                for(int step=0;step<3;step++)foreach(float side in new[]{-1f,1f})
                {
                    Vector2 cell=p-direction*(step*3)+tangent*(side*step*3);
                    PixelVfxMesh.Square(vh,cell,7,PixelVfxMesh.Alpha(DragonUiPalette.Ink,opacity));
                    PixelVfxMesh.Square(vh,cell,3,PixelVfxMesh.Alpha(colour,opacity));
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
