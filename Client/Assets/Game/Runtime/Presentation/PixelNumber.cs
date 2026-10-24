using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class PixelNumber : MaskableGraphic
    {
        static readonly string[] Glyphs = {
            "01110110011101111011100111001101110", "00100011000010000100001000010001110",
            "01110110010000100110010001000011111", "11110000010000101110000010000111110",
            "00010001100101010010111110001000010", "11111100001000011110000011000101110",
            "00110010001000011110100011000101110", "11111000010001000100010000100001000",
            "01110100011000101110100011000101110", "01110100011000101111000010001001100" };
        string digits = "0";
        public bool Critical;
        public bool DragonTheme;
        public bool Companion;
        public void SetValue(int number, bool critical)
        {
            digits = Mathf.Clamp(number,0,99999).ToString(); Critical = critical; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            Color tint=DragonTheme?PixelVfxMesh.Alpha(Critical?DragonUiPalette.Warning:Companion?DragonUiPalette.StaggerFill:DragonUiPalette.Light,color.a):color;
            float pixelScale=canvas?canvas.scaleFactor:1;
            float cell=Mathf.Floor(Mathf.Min(r.height/10,r.width/(digits.Length*6+3))*pixelScale)/pixelScale;
            if(DragonTheme&&canvas)
            {
                float scale=((RectTransform)canvas.transform).rect.height/720;
                cell=Mathf.Min(cell,Mathf.Max(1,Mathf.Floor((Critical?8:6)*scale*pixelScale))/pixelScale);
            }
            cell=Mathf.Max(1/pixelScale,cell);
            float left=Mathf.Round((r.center.x-(digits.Length*6-1)*cell*.5f)*pixelScale)/pixelScale;
            float top=Mathf.Round((r.center.y+cell*3.5f)*pixelScale)/pixelScale;
            Color outline=PixelVfxMesh.Alpha(DragonUiPalette.Ink,color.a);
            // One whole pixel of opaque outline keeps every digit readable over any terrain.
            for(int i=0;i<digits.Length;i++)for(int row=0;row<7;row++)for(int col=0;col<5;col++)
                if(Glyphs[digits[i]-'0'][row*5+col]=='1')
                {
                    var rect=new Rect(left+(i*6+col)*cell,top-(row+1)*cell,cell,cell);
                    Quad(vh,new Rect(rect.x-cell,rect.y,cell*3,cell),outline);
                    Quad(vh,new Rect(rect.x,rect.y-cell,cell,cell*3),outline);
                }
            for(int i=0;i<digits.Length;i++)
                for(int row=0;row<7;row++)for(int col=0;col<5;col++)
                    if(Glyphs[digits[i]-'0'][row*5+col]=='1')Quad(vh,new Rect(left+(i*6+col)*cell,top-(row+1)*cell,cell,cell),tint);
            if(Critical)
                for(int side=0;side<2;side++)
                {
                    float x=side==0?left-3*cell:left+(digits.Length*6+1)*cell;
                    Quad(vh,new Rect(x-cell,r.center.y-cell,3*cell,3*cell),outline);
                    Quad(vh,new Rect(x,r.center.y,cell,cell),tint);
                }
        }
        static void Quad(VertexHelper vh, Rect r, Color color)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
