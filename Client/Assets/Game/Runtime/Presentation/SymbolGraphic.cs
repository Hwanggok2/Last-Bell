using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public enum UiSymbol { Up, Back, Down, Forward, Sword, Bell, Star, Shield, Pause, Coin, Gem, Heart, Plus, Bag, Check, Cross }

    public sealed class SymbolGraphic : MaskableGraphic
    {
        public UiSymbol Symbol;
        public bool Filled = true;
        public Sprite NormalSprite,PressedSprite,DisabledSprite;
        public PointerCommand Command;
        public DragonButtonFeedback ButtonFeedback;
        Sprite lastSprite;
        Sprite CurrentSprite=>!Filled||Command&&!Command.Interactable||ButtonFeedback&&ButtonFeedback.Button&&!ButtonFeedback.Button.IsInteractable()?DisabledSprite:
            Command&&Command.IsPressed||ButtonFeedback&&ButtonFeedback.IsPressed?PressedSprite:NormalSprite;
        public override Texture mainTexture=>NormalSprite&&CurrentSprite?CurrentSprite.texture:base.mainTexture;
        void LateUpdate()
        {
            var sprite=CurrentSprite;if(sprite==lastSprite)return;lastSprite=sprite;SetVerticesDirty();SetMaterialDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if(NormalSprite&&CurrentSprite)
            {
                var sprite=CurrentSprite;float scale=Mathf.Min(r.width/sprite.rect.width,r.height/sprite.rect.height);
                var size=sprite.rect.size*scale;var min=r.center-size*.5f;var max=min+size;
                var uv=UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
                var tint=new Color(1,1,1,color.a);
                vh.AddVert(new Vector3(min.x,min.y),tint,new Vector2(uv.x,uv.y));
                vh.AddVert(new Vector3(min.x,max.y),tint,new Vector2(uv.x,uv.w));
                vh.AddVert(new Vector3(max.x,max.y),tint,new Vector2(uv.z,uv.w));
                vh.AddVert(new Vector3(max.x,min.y),tint,new Vector2(uv.z,uv.y));
                vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);return;
            }
            var c = r.center;
            float s = Mathf.Min(r.width, r.height) * .45f;
            if ((int)Symbol <= (int)UiSymbol.Forward)
            {
                float angle=Symbol==UiSymbol.Back?90:Symbol==UiSymbol.Down?180:Symbol==UiSymbol.Forward?-90:0;
                var q=Quaternion.Euler(0,0,angle);
                foreach(float offset in new[]{.28f,-.30f})
                {
                    Vector2 left=c+(Vector2)(q*new Vector2(-.78f,-.25f+offset))*s;
                    Vector2 tip=c+(Vector2)(q*new Vector2(0,.5f+offset))*s;
                    Vector2 right=c+(Vector2)(q*new Vector2(.78f,-.25f+offset))*s;
                    PixelVfxMesh.Line(vh,left,tip,3,color);PixelVfxMesh.Line(vh,tip,right,3,color);
                }
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

        void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color? tint=null)
        {
            int n = vh.currentVertCount;
            Color col=tint??color;vh.AddVert(a, col, Vector2.zero); vh.AddVert(b, col, Vector2.zero); vh.AddVert(c, col, Vector2.zero);
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
