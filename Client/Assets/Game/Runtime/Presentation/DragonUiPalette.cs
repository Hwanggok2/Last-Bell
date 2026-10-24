using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Shared presentation colours from Planning/Story/Dragon-v1/ui-pixel-v4.json.
    public static class DragonUiPalette
    {
        public static readonly Color Paper=new Color32(255,241,209,255),Light=new Color32(255,249,233,255),Ink=new Color32(39,61,69,255);
        public static readonly Color Edge=new Color32(113,84,63,255),Gold=new Color32(183,122,53,255),GoldLight=new Color32(244,205,108,255);
        public static readonly Color Success=new Color32(36,118,109,255),SuccessLight=new Color32(114,185,155,255);
        public static readonly Color Danger=new Color32(184,66,50,255),DangerLight=new Color32(241,140,103,255);
        public static readonly Color Empty=new Color32(89,100,99,255),Muted=new Color32(137,145,138,255),Disabled=new Color32(221,216,195,255);
        public static readonly Color Warning=new Color32(255,219,82,255),StaggerFill=new Color32(114,222,255,255),StaggerReady=new Color32(255,220,83,255),StaggerStunned=new Color32(124,245,185,255);

        public static string Copy(string text)=>string.IsNullOrEmpty(text)?text:text.Replace("종 파편","보석").Replace("파편","보석").Replace("공명","기세");
        public static void Stepped(VertexHelper vh,Rect r,float cell,Color colour)
        {
            cell=Mathf.Min(cell,Mathf.Min(r.width,r.height)*.5f);
            if(r.width<=0||r.height<=0)return;
            PixelVfxMesh.Rect(vh,new Rect(r.x+cell,r.y,r.width-2*cell,r.height),colour);
            PixelVfxMesh.Rect(vh,new Rect(r.x,r.y+cell,r.width,r.height-2*cell),colour);
        }
        static readonly string[] ShieldRows={"xxxxxxx","xllsssx","xllsssx","xllsssx","xllsssx",".xsssx.","..xsx..","...x..."};
        public static void Shield(VertexHelper vh,Vector2 center,float cell,bool spent)
        {
            var top=center+new Vector2(-3.5f,4)*cell;
            for(int y=0;y<ShieldRows.Length;y++)for(int x=0;x<7;x++)if(ShieldRows[y][x]!='.')
                for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++)
                    PixelVfxMesh.Rect(vh,new Rect(top.x+(x+xx)*cell,top.y-(y+yy+1)*cell,cell,cell),spent?Muted:Ink);
            for(int y=0;y<ShieldRows.Length;y++)for(int x=0;x<7;x++)if(ShieldRows[y][x]!='.')
            {
                char ch=ShieldRows[y][x];Color c=spent?Disabled:ch=='x'?Edge:ch=='l'?Light:new Color32(164,206,209,255);
                PixelVfxMesh.Rect(vh,new Rect(top.x+x*cell,top.y-(y+1)*cell,cell,cell),c);
            }
        }
        public static void Ring(VertexHelper vh,Rect clip,Vector2 center,float radius,float cell,Color colour)
        {
            if(radius<=0)return;cell=Mathf.Max(.5f,cell);const int segments=384;
            Vector2 previous=center+Vector2.right*radius;
            previous=new Vector2(Mathf.Round(previous.x/cell)*cell,Mathf.Round(previous.y/cell)*cell);
            for(int i=1;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;
                var next=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                next=new Vector2(Mathf.Round(next.x/cell)*cell,Mathf.Round(next.y/cell)*cell);
                Clipped(vh,new Rect(Mathf.Min(previous.x,next.x),previous.y,Mathf.Abs(next.x-previous.x)+cell,cell),clip,colour);
                Clipped(vh,new Rect(next.x,Mathf.Min(previous.y,next.y),cell,Mathf.Abs(next.y-previous.y)+cell),clip,colour);
                previous=next;
            }
        }
        static void Clipped(VertexHelper vh,Rect r,Rect clip,Color colour)
        {
            float x=Mathf.Max(r.xMin,clip.xMin),y=Mathf.Max(r.yMin,clip.yMin),right=Mathf.Min(r.xMax,clip.xMax),top=Mathf.Min(r.yMax,clip.yMax);
            if(right>x&&top>y)PixelVfxMesh.Rect(vh,new Rect(x,y,right-x,top-y),colour);
        }
    }
}
