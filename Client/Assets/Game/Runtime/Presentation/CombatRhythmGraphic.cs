using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Full viewport, behind HUD. Only samples the Core queue; never advances a clock.
    public sealed class CombatRhythmGraphic : MaskableGraphic
    {
        public bool DragonTheme;
        EnemyThreatView[] threats=System.Array.Empty<EnemyThreatView>();
        double clock;bool reduced;
        static readonly Color Wave=new Color32(194,224,239,255);
        public int BellCount=>threats.Length==0||clock<threats[0].CueAtMs?0:threats.Length;
        public Vector2 Center=>rectTransform.rect.center;
        public float ContactRadius=>rectTransform.rect.width*.625f;
        EnemyThreatView Current=>System.Array.Find(threats,t=>!t.Resolved);
        public float WaveProgress=>Current==null?-1:(float)((clock-Current.WaveStartMs)/(Current.ImpactAtMs-Current.WaveStartMs));
        public float AttackProgress
        {
            get
            {
                var current=Current;if(current==null)return 0;
                double start=threats.Length>0?threats[0].CueAtMs:current.CueAtMs;
                foreach(var previous in threats)if(previous.ImpactAtMs<current.ImpactAtMs)start=System.Math.Max(start,previous.ImpactAtMs);
                return Mathf.Clamp01((float)((clock-start)/System.Math.Max(1,current.ImpactAtMs-start)));
            }
        }
        public void Sample(GameSession session,bool reducedMotion)
        {
            threats=session.Phase==SessionPhase.Combat?session.BellThreats:System.Array.Empty<EnemyThreatView>();
            clock=session.ClockMs;reduced=reducedMotion;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(BellCount==0)return;
            var r=rectTransform.rect;float scale=r.height/540;
            if(DragonTheme)
            {
                float pixel=r.height/720*4;
                foreach(var threat in threats)
                {
                    if(clock<threat.CueAtMs)continue;
                    var p=Center+new Vector2((threat.SequenceIndex-(threat.SequenceCount-1)*.5f)*48*r.height/720,r.height*.5f-90*r.height/720);
                    Direction(vh,p,pixel,threat.Direction,threat.Resolved?DragonUiPalette.Muted:threat==Current?DragonUiPalette.Warning:DragonUiPalette.Paper);
                }
                // This fill ends on impact, never on the earlier cue-order reveal.
                var bar=new Rect(Center.x-120*r.height/720,r.yMax-124*r.height/720,240*r.height/720,12*r.height/720);
                PixelVfxMesh.Rect(vh,new Rect(bar.x-pixel,bar.y-pixel,bar.width+2*pixel,bar.height+2*pixel),DragonUiPalette.Ink);
                PixelVfxMesh.Rect(vh,bar,DragonUiPalette.Empty);
                PixelVfxMesh.Rect(vh,new Rect(bar.x,bar.y,Mathf.Floor(bar.width*AttackProgress/pixel)*pixel,bar.height),Current!=null&&Current.ImpactAtMs-clock<=120?DragonUiPalette.Light:DragonUiPalette.Warning);
                float q=WaveProgress;if(q<0||q>1)return;
                foreach(float x in new[]{r.xMin-r.width*.125f,r.xMax+r.width*.125f})
                {
                    var origin=new Vector2(x,r.center.y);float ringRadius=ContactRadius*q;
                    DragonUiPalette.Ring(vh,r,origin,ringRadius,pixel,PixelVfxMesh.Alpha(DragonUiPalette.Ink,.8f));
                    if(!reduced)DragonUiPalette.Ring(vh,r,origin,ringRadius-2*pixel,pixel,PixelVfxMesh.Alpha(DragonUiPalette.SuccessLight,.6f));
                }
                PixelVfxMesh.Square(vh,Center,pixel,DragonUiPalette.Success);return;
            }
            foreach(var threat in threats)
            {
                var p=Center+new Vector2((threat.SequenceIndex-(threat.SequenceCount-1)*.5f)*44*scale,r.height*.5f-80*scale);
                Bell(vh,p,15*scale,threat.Resolved?PixelVfxMesh.Alpha(PixelVfxMesh.Gold,.22f):PixelVfxMesh.Gold);
                if(threat==Current)Ring(vh,p,23*scale,2*scale,PixelVfxMesh.Alpha(Wave,.6f));
            }
            float progress=WaveProgress;
            if(progress<0||progress>1)return;
            float radius=ContactRadius*progress;
            foreach(float x in new[]{r.xMin-r.width*.125f,r.xMax+r.width*.125f})
            {
                var origin=new Vector2(x,r.center.y);
                if(!reduced)Ring(vh,origin,radius,18*scale,PixelVfxMesh.Alpha(Wave,.09f));
                Ring(vh,origin,radius,4*scale,PixelVfxMesh.Alpha(Wave,.85f));
                if(!reduced&&radius>28*scale)Ring(vh,origin,radius-28*scale,2*scale,PixelVfxMesh.Alpha(Wave,.18f));
            }
            float size=4*scale;var c=PixelVfxMesh.Alpha(Wave,.6f);
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI/2,b=(i+1)*Mathf.PI/2;
                PixelVfxMesh.Line(vh,Center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size,Center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*size,1.5f*scale,c);
            }
            if(Current!=null&&Current.ImpactAtMs-clock<=90)
                foreach(float sx in new[]{-1f,1f})foreach(float sy in new[]{-1f,1f})
                {
                    var p=Center+new Vector2(sx*11,sy*11)*scale;
                    PixelVfxMesh.Line(vh,p,p+Vector2.right*(-sx*5*scale),2*scale,Wave);
                    PixelVfxMesh.Line(vh,p,p+Vector2.up*(-sy*5*scale),2*scale,Wave);
                }
        }
        static void Direction(VertexHelper vh,Vector2 center,float cell,DodgeDirection direction,Color color)
        {
            // Seven-pixel triangle, the same directional language as the touch controls.
            for(int row=0;row<4;row++)for(int col=-row;col<=row;col++)
            {
                Vector2 p=new Vector2(col,2-row);
                if(direction==DodgeDirection.Down)p=-p;
                else if(direction==DodgeDirection.Back)p=new Vector2(-p.y,p.x);
                else if(direction==DodgeDirection.Forward)p=new Vector2(p.y,p.x);
                PixelVfxMesh.Square(vh,center+p*cell,cell*2,DragonUiPalette.Ink);
            }
            for(int row=0;row<4;row++)for(int col=-row;col<=row;col++)
            {
                Vector2 p=new Vector2(col,2-row);
                if(direction==DodgeDirection.Down)p=-p;
                else if(direction==DodgeDirection.Back)p=new Vector2(-p.y,p.x);
                else if(direction==DodgeDirection.Forward)p=new Vector2(p.y,p.x);
                PixelVfxMesh.Square(vh,center+p*cell,cell,color);
            }
        }
        static void Bell(VertexHelper vh,Vector2 p,float s,Color c)
        {
            Vector2[] points={new Vector2(-.8f,-.5f),new Vector2(-.55f,-.2f),new Vector2(-.5f,.5f),new Vector2(0,.8f),new Vector2(.5f,.5f),new Vector2(.55f,-.2f),new Vector2(.8f,-.5f)};
            for(int i=0;i<points.Length;i++)PixelVfxMesh.Line(vh,p+points[i]*s,p+points[(i+1)%points.Length]*s,3,c);
            PixelVfxMesh.Line(vh,p+new Vector2(-.15f,-.8f)*s,p+new Vector2(.15f,-.8f)*s,3,c);
        }
        static void Ring(VertexHelper vh,Vector2 origin,float radius,float width,Color c)
        {
            if(radius<=0)return;
            const int segments=256;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));int n=vh.currentVertCount;
                vh.AddVert(origin+u*(radius-width*.5f),c,Vector2.zero);vh.AddVert(origin+u*(radius+width*.5f),c,Vector2.zero);
                vh.AddVert(origin+v*(radius+width*.5f),c,Vector2.zero);vh.AddVert(origin+v*(radius-width*.5f),c,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
