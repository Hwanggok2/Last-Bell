using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    public sealed class ReferencePanelGraphic : MaskableGraphic
    {
        public bool Diamond;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var edge=new Color32(153,125,74,255);
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
            foreach(float x in new[]{r.x+2,r.xMax-10})foreach(float y in new[]{r.y+2,r.yMax-10})PixelVfxMesh.Rect(vh,new Rect(x,y,8,8),edge);
        }
    }
}
