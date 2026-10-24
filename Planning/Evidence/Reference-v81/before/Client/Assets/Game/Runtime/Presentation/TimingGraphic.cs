using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Draws the Core's cue, never owns another combat clock or hit decision.
    public sealed class TimingGraphic : MaskableGraphic
    {
        public SymbolGraphic DirectionMarker, NextMarker;
        public Text DirectionLabel, NextLabel;
        float progress, duration=650, active=160, perfect=60;
        public void Show(EnemyIntentView cue, CombatConfig combat)
        {
            progress=cue.Progress;duration=Mathf.Max(1,cue.TelegraphMs);
            active=combat.dodgeActiveMs;perfect=combat.perfectWindowMs;SetVerticesDirty();
            if(DirectionMarker){DirectionMarker.Symbol=(UiSymbol)cue.Direction;DirectionMarker.SetVerticesDirty();}
            if(DirectionLabel)DirectionLabel.text=new[]{"위로 피하기","왼쪽으로 피하기","아래로 피하기","오른쪽으로 피하기"}[(int)cue.Direction];
            if(NextMarker){NextMarker.gameObject.SetActive(cue.HasNext);NextMarker.Symbol=(UiSymbol)cue.NextDirection;NextMarker.SetVerticesDirty();}
            if(NextLabel)NextLabel.gameObject.SetActive(cue.HasNext);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Dark);
            var b=new Rect(r.x+2,r.y+3,r.width-4,r.height-6);
            PixelVfxMesh.Rect(vh,b,new Color32(45,61,73,255));
            float normalX=b.x+b.width*Mathf.Clamp01(1-active/duration);
            float perfectX=b.x+b.width*Mathf.Clamp01(1-perfect/duration);
            PixelVfxMesh.Rect(vh,new Rect(normalX,b.y,perfectX-normalX,b.height),new Color32(103,83,45,255));
            PixelVfxMesh.Rect(vh,new Rect(perfectX,b.y,b.xMax-perfectX,b.height),new Color32(49,105,100,255));
            PixelVfxMesh.Rect(vh,new Rect(b.x,b.y+3,b.width*progress,Mathf.Max(2,b.height-6)),PixelVfxMesh.Cream);
            PixelVfxMesh.Rect(vh,new Rect(normalX,r.y,1,r.height),PixelVfxMesh.Gold);
            PixelVfxMesh.Rect(vh,new Rect(perfectX,r.y,1,r.height),new Color32(158,227,212,255));
            PixelVfxMesh.Rect(vh,new Rect(r.xMax-2,r.y-2,2,r.height+4),PixelVfxMesh.Cream);
            PixelVfxMesh.Rect(vh,new Rect(perfectX+2,r.y+1,1,r.height-2),new Color32(158,227,212,255));
        }
    }
}
