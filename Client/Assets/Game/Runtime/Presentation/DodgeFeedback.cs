using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    // Full viewport overlay, before HUD. Fast-combo perfect waves can overlap.
    public sealed class DodgeFeedback : MaskableGraphic
    {
        public Text Label;
        public Image StaggerGain;
        public bool DragonTheme;
        readonly double[] starts={-10000,-10000};
        string resultLabel="";
        Color resultColor;
        double clock,gainAt=-10000,labelAt=-10000;bool reduced;
        static readonly Color Mint=new Color32(158,227,212,255),Cream=new Color32(255,241,205,255);
        float PixelScale=>canvas?Mathf.Max(.01f,canvas.scaleFactor):1;
        public float MaximumRadius=>rectTransform.rect.size.magnitude*.5f+64/PixelScale;
        public Vector2 WaveCenter=>rectTransform.rect.center;
        public void Confirm(SessionEvent e,float threshold,bool reducedMotion)
        {
            clock=e.TimeMs;gainAt=clock;reduced=reducedMotion;
            if((e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.PerfectParry)){starts[starts[0]<=starts[1]?0:1]=clock;labelAt=clock;resultLabel=e.Kind==SessionEventKind.PerfectParry?"완벽 쳐내기":"완벽";Label.text=resultLabel;Label.color=Mint;}
            resultColor=DragonTheme?DragonUiPalette.Success:Mint;
            if(e.Kind==SessionEventKind.Parried){labelAt=clock;resultLabel="쳐냄";resultColor=DragonTheme?DragonUiPalette.Ink:Cream;}
            if(StaggerGain)
            {
                var r=StaggerGain.rectTransform;r.anchorMin=new Vector2((float)e.StaggerBefore/threshold,0);r.anchorMax=new Vector2((float)(e.StaggerBefore+e.StaggerGained)/threshold,1);
                r.offsetMin=r.offsetMax=Vector2.zero;StaggerGain.enabled=e.StaggerGained>0;StaggerGain.color=(e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.PerfectParry)?Mint:Cream;
            }
            SetVerticesDirty();
        }
        public void Clear(){starts[0]=starts[1]=gainAt=labelAt=-10000;if(Label)Label.text="";if(StaggerGain)StaggerGain.enabled=false;SetVerticesDirty();}
        public void Sample(double clockMs,bool reducedMotion)
        {
            clock=clockMs;reduced=reducedMotion;float age=(float)(clock-labelAt);
            if(Label){Label.text=age>=0&&age<320?resultLabel:"";Label.color=PixelVfxMesh.Alpha(resultColor,1-Mathf.Clamp01((age-220)/100));}
            if(StaggerGain&&clock-gainAt>=180)StaggerGain.enabled=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach(double start in starts)
            {
                float age=(float)(clock-start);if(age<0||age>=420)continue;
                if(DragonTheme)
                {
                    float p=age/420,radius=reduced?36:Mathf.Lerp(12/PixelScale,MaximumRadius,1-(1-p)*(1-p));
                    DragonUiPalette.Ring(vh,rectTransform.rect,WaveCenter,radius,rectTransform.rect.height/180,PixelVfxMesh.Alpha(DragonUiPalette.Success,1-p*.8f));continue;
                }
                if(reduced){Ring(vh,WaveCenter,36,2,PixelVfxMesh.Alpha(Mint,1-age/420));continue;}
                for(int i=0;i<2;i++)
                {
                    float p=(age-i*45)/(420-i*45);if(p<0||p>=1)continue;
                    float radius=Mathf.Lerp(12/PixelScale,MaximumRadius,1-(1-p)*(1-p));
                    Ring(vh,WaveCenter,radius,i==0?3:2,PixelVfxMesh.Alpha(i==0?Mint:Cream,(i==0?.65f:.46f)*(1-p*.78f)));
                }
            }
        }
        static void Ring(VertexHelper vh,Vector2 center,float radius,float width,Color c)
        {
            const int count=160;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;int n=vh.currentVertCount;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                vh.AddVert(center+u*(radius-width*.5f),c,Vector2.zero);vh.AddVert(center+u*(radius+width*.5f),c,Vector2.zero);
                vh.AddVert(center+v*(radius+width*.5f),c,Vector2.zero);vh.AddVert(center+v*(radius-width*.5f),c,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
