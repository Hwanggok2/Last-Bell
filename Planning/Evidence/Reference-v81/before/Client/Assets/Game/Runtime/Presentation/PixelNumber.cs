using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class PixelNumber : MaskableGraphic
    {
        static readonly string[] Glyphs = {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
            "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111" };
        string digits = "0";
        public bool Critical;
        public void SetValue(int number, bool critical)
        {
            digits = Mathf.Clamp(number,0,99999).ToString(); Critical = critical; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            float cell = Mathf.Floor(Mathf.Min(r.height/5, (r.width-(Critical?20:0))/(digits.Length*4-1)));
            float left=r.center.x-(digits.Length*4-1)*cell*.5f, top=r.center.y+cell*2.5f;
            for(int i=0;i<digits.Length;i++)
                for(int row=0;row<5;row++) for(int col=0;col<3;col++)
                    if(Glyphs[digits[i]-'0'][row*3+col]=='1') Quad(vh,new Rect(left+(i*4+col)*cell,top-(row+1)*cell,cell,cell),color);
            if(Critical)
                for(int i=0;i<3;i++)
                {
                    Quad(vh,new Rect(left-10-i*2,r.center.y-2+(i-1)*5,4,4),color);
                    Quad(vh,new Rect(left+(digits.Length*4-1)*cell+6+i*2,r.center.y-2+(i-1)*5,4,4),color);
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
