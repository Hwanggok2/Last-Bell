using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Authored, saved button ornament; selectable owns press/disabled color.
    public sealed class PlateGraphic : MaskableGraphic
    {
        public bool Emphasis;
        float cooldown;
        public void SetCooldown(float value){value=Mathf.Clamp01(value);if(Mathf.Approximately(value,cooldown))return;cooldown=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            PixelVfxMesh.Rect(vh,r,new Color32(12,23,35,255));
            PixelVfxMesh.Rect(vh,new Rect(r.x+3,r.y+3,r.width-6,r.height-6),color);
            Color edge=Emphasis?PixelVfxMesh.Gold:(Color)new Color32(93,113,122,255);
            PixelVfxMesh.Rect(vh,new Rect(r.x+5,r.yMax-5,r.width-10,2),edge);
            PixelVfxMesh.Rect(vh,new Rect(r.x+5,r.y+3,r.width-10,2),new Color32(7,15,24,255));
            foreach(float x in new[]{r.x+4,r.xMax-8})foreach(float y in new[]{r.y+4,r.yMax-8})PixelVfxMesh.Rect(vh,new Rect(x,y,4,4),PixelVfxMesh.Old);
            if(cooldown>0)PixelVfxMesh.Rect(vh,new Rect(r.x+5,r.y+6,(r.width-10)*(1-cooldown),3),PixelVfxMesh.Cream);
        }
    }
}
