using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Integer pixel cells; only the presenter supplies HP, including the last live cell.
    public sealed class HealthBarGraphic : MaskableGraphic
    {
        public bool Enemy;
        public bool DragonTheme;
        public float Value=1;
        public float TrailValue { get; private set; }=1;
        public bool TrailActive => trailActive;
        public const float TrailHoldSeconds=.12f,TrailLerpSeconds=.55f;
        bool trailActive;
        float trailStart=1,trailHold,trailElapsed,trailDuration;
        static readonly Color HealthGreen=new Color32(81,164,65,255),HealthLight=new Color32(181,226,113,255);
        static readonly Color TrailOrange=new Color32(239,143,53,255),TrailLight=new Color32(255,199,111,255);
        // Integer ceiling avoids float rounding at exactly one third and overflow from damage*3.
        public static bool IsHeavyDamage(long damageMilli,long maxHpMilli)=>maxHpMilli>0&&damageMilli>0&&damageMilli>=maxHpMilli/3+(maxHpMilli%3==0?0:1);
        public static int FilledCells(float value,int capacity)=>capacity<=0||value<=0?0:Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(value)*capacity),1,capacity);
        public void ResetValue(float value)
        {
            Value=TrailValue=Mathf.Clamp01(value);trailActive=false;trailHold=trailElapsed=0;SetVerticesDirty();
        }
        public void SetValue(float value)
        {
            value=Mathf.Clamp01(value);if(Value==value)return;
            if(value>Value||!trailActive){ResetValue(value);return;}
            // A smaller follow-up hit moves the target, without restarting the hold or extending the deadline.
            Value=value;trailStart=TrailValue;trailDuration=Mathf.Max(.0001f,trailDuration-trailElapsed);trailElapsed=0;SetVerticesDirty();
        }
        public void ApplyDamage(float currentFraction,long damageMilli,long maxHpMilli)
        {
            float before=Value,previousTrail=TrailValue;
            SetValue(currentFraction);
            if(!IsHeavyDamage(damageMilli,maxHpMilli)||Value>=before)return;
            TrailValue=trailStart=Mathf.Max(previousTrail,before);trailHold=TrailHoldSeconds;
            trailElapsed=0;trailDuration=TrailLerpSeconds;trailActive=true;SetVerticesDirty();
        }
        // Presenter advances this only with the unpaused presentation clock. No gameplay HP is interpolated.
        public void Sample(float deltaSeconds)
        {
            if(!trailActive||deltaSeconds<=0)return;
            float held=Mathf.Min(trailHold,deltaSeconds);trailHold-=held;deltaSeconds-=held;
            trailElapsed=Mathf.Min(trailDuration,trailElapsed+deltaSeconds);
            TrailValue=Mathf.Lerp(trailStart,Value,trailElapsed/trailDuration);
            if(trailElapsed>=trailDuration){TrailValue=Value;trailActive=false;}
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float p=r.height/16f;
            if(DragonTheme)
            {
                DrawDragon(vh,GetPixelAdjustedRect());
                return;
            }
            Color edge=new Color32(126,109,76,255),ink=new Color32(9,19,27,255);
            Color fill=Enemy?new Color32(193,91,68,255):new Color32(99,183,153,255);
            Color light=Enemy?new Color32(243,165,113,255):new Color32(184,227,191,255);
            Quad(vh,r.xMin+4*p,r.yMin+2*p,r.width-8*p,12*p,edge);
            Quad(vh,r.xMin+5*p,r.yMin+3*p,r.width-10*p,10*p,ink);
            float width=Mathf.Floor((r.width-16*p)*Value/p)*p;
            if(width>0){Quad(vh,r.xMin+8*p,r.yMin+5*p,width,6*p,fill);Quad(vh,r.xMin+8*p,r.yMin+9*p,width,2*p,light);}
            foreach(float x in new[]{r.xMin,r.xMax-6*p})
            {
                Quad(vh,x,r.yMin+5*p,6*p,6*p,ink);Quad(vh,x+2*p,r.yMin+6*p,3*p,4*p,edge);
                Quad(vh,x+3*p,r.yMin+7*p,p,2*p,new Color32(234,207,151,255));
            }
        }
        static readonly string[] Heart={".xx.xx.","xxxxxxx","xxxxxxx",".xxxxx.","..xxx..","...x..."};
        void DrawDragon(VertexHelper vh,Rect r)
        {
            float scale=canvas?canvas.scaleFactor:1;
            float cell=Mathf.Max(1,Mathf.Floor(r.height*scale/11))/scale;
            int columns=Mathf.Max(20,Mathf.FloorToInt(r.width/cell));
            float left=Mathf.Round(r.xMin*scale)/scale,top=Mathf.Round(r.yMax*scale)/scale;
            Color ink=DragonUiPalette.Ink,fill=HealthGreen,light=HealthLight;
            // Stepped corners and solid, square highlights are drawn on the same grid.
            for(int y=0;y<11;y++)
            {
                int inset=y==0||y==10?2:y==1||y==9?1:0;
                Cell(vh,left,top,cell,inset,y,columns-2*inset,1,ink);
            }
            Cell(vh,left,top,cell,3,1,columns-6,1,DragonUiPalette.Paper);
            Cell(vh,left,top,cell,2,2,columns-4,1,DragonUiPalette.Edge);
            Cell(vh,left,top,cell,2,8,columns-4,1,DragonUiPalette.Edge);
            Cell(vh,left,top,cell,3,9,columns-6,1,DragonUiPalette.Empty);
            for(int y=0;y<Heart.Length;y++)for(int x=0;x<7;x++)if(Heart[y][x]=='x')
                Cell(vh,left,top,cell,x+2,y+3,1,1,Value>0?(y<2?light:fill):DragonUiPalette.Empty);
            Cell(vh,left,top,cell,11,3,1,5,DragonUiPalette.Muted);
            int width=columns-16,capacity=width-(width/5),filled=FilledCells(Value,capacity),trailFilled=FilledCells(TrailValue,capacity),index=0;
            for(int x=0;x<width;x++)
            {
                if(x%5==4)continue;
                bool active=index<filled,afterimage=index++<trailFilled;
                Cell(vh,left,top,cell,13+x,4,1,4,active?fill:afterimage?TrailOrange:DragonUiPalette.Empty);
                if(active||afterimage)Cell(vh,left,top,cell,13+x,3,1,1,active?light:TrailLight);
            }
        }
        static void Cell(VertexHelper vh,float left,float top,float cell,int x,int y,int w,int h,Color color)
            =>Quad(vh,left+x*cell,top-(y+h)*cell,w*cell,h*cell,color);
        static void Quad(VertexHelper vh,float x,float y,float w,float h,Color c)
        {
            int i=vh.currentVertCount;vh.AddVert(new Vector3(x,y),c,Vector2.zero);vh.AddVert(new Vector3(x,y+h),c,Vector2.zero);
            vh.AddVert(new Vector3(x+w,y+h),c,Vector2.zero);vh.AddVert(new Vector3(x+w,y),c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
