using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    public sealed class ReferencePanelGraphic : MaskableGraphic
    {
        public bool Diamond;
        public bool Underline;
        public bool DragonTheme,Selected,Shelf;
        public float Cell=4;
        public Selectable Control;
        bool pressed;
        public void SetPressed(bool value){if(pressed==value)return;pressed=value;SetVerticesDirty();}
        public void SetSelected(bool value){if(Selected==value)return;Selected=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var edge=new Color32(153,125,74,255);
            if(DragonTheme)
            {
                float p=Mathf.Max(.5f,Cell);bool disabled=Control&&!Control.IsInteractable();
                if(Shelf)
                {
                    PixelVfxMesh.Rect(vh,r,DragonUiPalette.Paper);
                    PixelVfxMesh.Rect(vh,new Rect(r.x,r.yMax-p,r.width,p),DragonUiPalette.Edge);
                    PixelVfxMesh.Rect(vh,new Rect(r.x,r.yMax-2*p,r.width,p),DragonUiPalette.GoldLight);return;
                }
                var shifted=r;shifted.y-=pressed&&!disabled?p:0;
                DragonUiPalette.Stepped(vh,new Rect(r.x,r.y-p,r.width,r.height),p*2,DragonUiPalette.Edge);
                DragonUiPalette.Stepped(vh,shifted,p*2,DragonUiPalette.Ink);
                var border=new Rect(shifted.x+p,shifted.y+p,shifted.width-2*p,shifted.height-2*p);
                DragonUiPalette.Stepped(vh,border,p,Selected?DragonUiPalette.Gold:DragonUiPalette.GoldLight);
                var face=new Rect(shifted.x+2*p,shifted.y+2*p,shifted.width-4*p,shifted.height-4*p);
                DragonUiPalette.Stepped(vh,face,p,disabled?DragonUiPalette.Disabled:DragonUiPalette.Paper);
                PixelVfxMesh.Rect(vh,new Rect(face.x+p,face.yMax-p,face.width-2*p,p),DragonUiPalette.Light);
                if(Selected)
                {
                    var c=new Vector2(r.xMax-5*p,r.yMax-3*p);
                    PixelVfxMesh.Rect(vh,new Rect(c.x-2*p,c.y,p,2*p),DragonUiPalette.Success);
                    PixelVfxMesh.Rect(vh,new Rect(c.x-p,c.y-p,p,2*p),DragonUiPalette.Success);
                    PixelVfxMesh.Rect(vh,new Rect(c.x,c.y,2*p,p),DragonUiPalette.Success);
                    PixelVfxMesh.Rect(vh,new Rect(c.x+p,c.y+p,p,p),DragonUiPalette.Success);
                }
                return;
            }
            if(Underline)
            {
                PixelVfxMesh.Rect(vh,new Rect(r.x+12,r.y+6,r.width-24,2),edge);
                return;
            }
            if(Diamond)
            {
                Vector2[] p={new Vector2(r.center.x,r.yMax),new Vector2(r.xMax,r.center.y),new Vector2(r.center.x,r.yMin),new Vector2(r.xMin,r.center.y)};
                vh.AddVert(r.center,color,Vector2.zero);foreach(var x in p)vh.AddVert(x,color,Vector2.zero);
                for(int i=0;i<4;i++){vh.AddTriangle(0,i+1,(i+1)%4+1);PixelVfxMesh.Line(vh,p[i],p[(i+1)%4],3,edge);}return;
            }
            PixelVfxMesh.Rect(vh,r,color);
            PixelVfxMesh.Rect(vh,new Rect(r.x+4,r.y+4,r.width-8,2),edge);
            PixelVfxMesh.Rect(vh,new Rect(r.x+4,r.yMax-6,r.width-8,2),edge);
            PixelVfxMesh.Rect(vh,new Rect(r.x+4,r.y+4,2,r.height-8),edge);
            PixelVfxMesh.Rect(vh,new Rect(r.xMax-6,r.y+4,2,r.height-8),edge);
            foreach(float x in new[]{r.x+2,r.xMax-10})foreach(float y in new[]{r.y+2,r.yMax-10})
            {PixelVfxMesh.Rect(vh,new Rect(x,y,8,8),edge);PixelVfxMesh.Rect(vh,new Rect(x+2,y+2,4,4),color);}
        }
    }
}
