using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public enum UiSymbol { Up, Back, Down, Forward, Sword, Bell, Star }

    public sealed class SymbolGraphic : MaskableGraphic
    {
        public UiSymbol Symbol;
        public bool Filled = true;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var c = r.center;
            float s = Mathf.Min(r.width, r.height) * .45f;
            if ((int)Symbol <= (int)UiSymbol.Forward)
            {
                Vector2[] points = { new Vector2(0, 1), new Vector2(-1, -1), new Vector2(1, -1) };
                float angle = Symbol == UiSymbol.Back ? 90 : Symbol == UiSymbol.Down ? 180 : Symbol == UiSymbol.Forward ? -90 : 0;
                var q = Quaternion.Euler(0, 0, angle);
                Triangle(vh, c + (Vector2)(q * points[0]) * s, c + (Vector2)(q * points[1]) * s, c + (Vector2)(q * points[2]) * s);
                return;
            }
            if (Symbol == UiSymbol.Star)
            {
                var pts = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float a = (90 + i * 36) * Mathf.Deg2Rad;
                    pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * (i % 2 == 0 ? 1 : .43f);
                }
                for (int i = 0; i < 10; i++)
                    if (Filled) Triangle(vh, c, pts[i], pts[(i + 1) % 10]);
                    else Line(vh, pts[i], pts[(i + 1) % 10], Mathf.Max(1, s * .12f));
                return;
            }
            if (Symbol == UiSymbol.Bell)
            {
                Vector2[] points = { new Vector2(-.8f,-.5f), new Vector2(-.55f,-.2f), new Vector2(-.5f,.5f), new Vector2(0,.8f), new Vector2(.5f,.5f), new Vector2(.55f,-.2f), new Vector2(.8f,-.5f) };
                for (int i = 0; i < points.Length; i++)
                    if (Filled) Triangle(vh, c, c + points[i]*s, c + points[(i+1)%points.Length]*s);
                    else Line(vh,c+points[i]*s,c+points[(i+1)%points.Length]*s,Mathf.Max(1,s*.12f));
                Line(vh,c+new Vector2(-.15f,-.8f)*s,c+new Vector2(.15f,-.8f)*s,Mathf.Max(1,s*.2f));
                return;
            }
            Line(vh,c+new Vector2(-.5f,-.5f)*s,c+new Vector2(.8f,.8f)*s,s*.35f);
            Line(vh,c+new Vector2(-.85f,-.3f)*s,c+new Vector2(-.3f,-.85f)*s,s*.2f);
            Line(vh,c+new Vector2(-.55f,-.55f)*s,c+new Vector2(-.95f,-.95f)*s,s*.2f);
        }

        void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c)
        {
            int n = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
        }
        void Line(VertexHelper vh, Vector2 a, Vector2 b, float width)
        {
            Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized * width*.5f;
            Triangle(vh,a-n,a+n,b+n); Triangle(vh,a-n,b+n,b-n);
        }
        public void SetFilled(bool value) { if (Filled == value) return; Filled = value; SetVerticesDirty(); }
    }
}
