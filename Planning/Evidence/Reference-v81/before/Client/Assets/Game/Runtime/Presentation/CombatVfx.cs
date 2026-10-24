using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // All coordinates use the existing 960x540 canvas. Time and hit decisions belong to Presenter/Core.
    public sealed class CombatVfx : MaskableGraphic
    {
        public Image SlashSprite;
        public Sprite[] SlashFrames = new Sprite[0];
        Vector2 slashFrom,slashHit,dissolveFoot,dissolveSize;
        float slashAge=-1,roarProgress=-1,lastRoarProgress=-1,dissolveProgress=-1,roarRadius;
        int slashVariant;
        bool slashCritical,reduced;
        readonly Vector2[] roarAnchors=new Vector2[3];
        readonly bool[] roarEmitted=new bool[3];

        // ageSeconds is measured from AttackStarted: lead .175, hit .200, end .425.
        public void SetSlash(Vector2 from,Vector2 hit,int variant,float ageSeconds,bool critical)
        {
            slashFrom=from;slashHit=hit;slashVariant=Mathf.Clamp(variant,0,5);slashAge=ageSeconds;slashCritical=critical;
            bool show=ageSeconds>=.175f&&ageSeconds<.425f&&SlashFrames!=null&&SlashFrames.Length>0;
            if(SlashSprite)
            {
                SlashSprite.gameObject.SetActive(show);
                if(show)
                {
                    int frame=Mathf.Min(SlashFrames.Length-1,(int)((ageSeconds-.175f)/.025f));SlashSprite.sprite=SlashFrames[frame];
                    Vector2 direction=(hit-from).normalized;float length=(hit-from).magnitude+41.25f;
                    float scale=length/Mathf.Sqrt(213*213+86*86);var r=SlashSprite.rectTransform;
                    r.anchoredPosition=from;r.sizeDelta=new Vector2(256,128)*scale;
                    r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-Mathf.Atan2(86,213)*Mathf.Rad2Deg);
                }
            }
            SetVerticesDirty();
        }
        public void SetRoar(Vector2 face,float radius,float progress)
        {
            if(progress<0||lastRoarProgress<0||progress<lastRoarProgress)System.Array.Clear(roarEmitted,0,roarEmitted.Length);
            roarRadius=radius;roarProgress=lastRoarProgress=progress;
            for(int i=0;i<3;i++)if(progress>=i*.3f&&!roarEmitted[i]){roarAnchors[i]=face;roarEmitted[i]=true;}
            SetVerticesDirty();
        }
        public void SetDissolve(Vector2 foot,Vector2 bodySize,float progress,bool reducedMotion){dissolveFoot=foot;dissolveSize=bodySize;dissolveProgress=progress;reduced=reducedMotion;SetVerticesDirty();}
        public void ClearEffects(bool preserveRoarHistory=false){slashAge=roarProgress=dissolveProgress=-1;if(!preserveRoarHistory){lastRoarProgress=-1;System.Array.Clear(roarEmitted,0,roarEmitted.Length);}if(SlashSprite)SlashSprite.gameObject.SetActive(false);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(slashAge>=.175f&&slashAge<.425f)
            {
                float age=slashAge-.175f,fade=1-Mathf.Clamp01((age-.1f)/.15f);
                if(SlashFrames==null||SlashFrames.Length==0||!SlashSprite)
                {
                    Vector2 dir=(slashHit-slashFrom).normalized;
                    if(dir.sqrMagnitude<.01f)dir=new Vector2(1,slashVariant<3?.4f:-.4f).normalized;
                    Vector2 end=slashHit+dir*41.25f;float reveal=Mathf.Clamp01(age/.025f);end=Vector2.Lerp(slashFrom,end,reveal);
                    int count=Mathf.Max(2,Mathf.CeilToInt(Vector2.Distance(slashFrom,end)/3));
                    for(int i=0;i<count;i++)
                    {
                        float q=i/(float)(count-1),width=(slashCritical?15:11)*Mathf.Sin(Mathf.PI*q);Vector2 p=Vector2.Lerp(slashFrom,end,q);
                        PixelVfxMesh.Square(vh,p,width+4,PixelVfxMesh.Alpha(PixelVfxMesh.Dark,fade));
                        PixelVfxMesh.Square(vh,p,width,PixelVfxMesh.Alpha(PixelVfxMesh.Gold,fade));
                        PixelVfxMesh.Square(vh,p,Mathf.Max(1.5f,width*.35f),PixelVfxMesh.Alpha(PixelVfxMesh.Cream,fade));
                    }
                }
                if(slashAge>=.2f&&slashAge<.34f)PixelVfxMesh.Spark(vh,slashHit,(slashAge-.2f)/.14f,slashCritical?26:18,fade);
            }
            if(roarProgress>=0&&roarProgress<1)
            {
                // Exactly three pulses. Each retains the face position captured at its emission.
                for(int ring=0;ring<3;ring++)
                {
                    float q=(roarProgress-ring*.3f)/.4f;if(q<0||q>=1||!roarEmitted[ring])continue;
                    float radius=Mathf.Lerp(roarRadius*(24f/108f),roarRadius,q),alpha=(1-q)*.85f;
                    for(int step=0;step<40;step++)
                    {
                        float a=step*Mathf.PI*2/40;Vector2 p=roarAnchors[ring]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                        PixelVfxMesh.Square(vh,p,5,PixelVfxMesh.Alpha(PixelVfxMesh.Dark,alpha));
                        PixelVfxMesh.Square(vh,p,3,PixelVfxMesh.Alpha(PixelVfxMesh.Gold,alpha));
                    }
                }
            }
            if(dissolveProgress>0&&dissolveProgress<1)
            {
                int count=reduced?18:36;
                for(int i=0;i<count;i++)
                {
                    float u=(i*37%101)/101f,v=(i*61%97)/97f,delay=u*.35f,k=Mathf.Clamp01((dissolveProgress-delay)/.65f);
                    if(k<=0||k>=1)continue;
                    Vector2 p=dissolveFoot+new Vector2((u-.5f)*dissolveSize.x,v*dissolveSize.y);
                    p+=new Vector2((u-.5f)*k*(reduced?9:48),-75*k*k);
                    Color tint=i%3==0?PixelVfxMesh.Cream:i%3==1?PixelVfxMesh.Gold:PixelVfxMesh.Old;
                    PixelVfxMesh.Square(vh,p,i%3==0?6:3,PixelVfxMesh.Alpha(tint,1-k));
                }
            }
        }
    }

    internal static class PixelVfxMesh
    {
        internal static readonly Color Dark=new Color32(12,23,43,255),Old=new Color32(172,120,51,255),Gold=new Color32(220,169,71,255),Cream=new Color32(255,240,180,255);
        internal static Color Alpha(Color c,float a){c.a*=Mathf.Clamp01(a);return c;}
        internal static void Rect(VertexHelper vh,Rect r,Color c)
        {
            if(c.a<=0||r.width<=0||r.height<=0)return;int n=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        internal static void Square(VertexHelper vh,Vector2 p,float size,Color c){p=new Vector2(Mathf.Round(p.x*2)*.5f,Mathf.Round(p.y*2)*.5f);Rect(vh,new Rect(p.x-size*.5f,p.y-size*.5f,size,size),c);}
        internal static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/2));
            for(int i=0;i<=count;i++)Square(vh,Vector2.Lerp(a,b,i/(float)count),width,c);
        }
        internal static void Spark(VertexHelper vh,Vector2 p,float progress,float radius,float alpha)
        {
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Vector2 q=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*(1+progress);Square(vh,q,i%2==0?6:3,Alpha(i%2==0?Cream:Gold,alpha));}
            Square(vh,p,12,Alpha(Dark,alpha));Square(vh,p,8,Alpha(Cream,alpha));
        }
    }
}
