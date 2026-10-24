using System;
using UnityEngine;

namespace LastBell
{
    public enum GroundKind { Meadow,Forest,Quarry,Ridge,DragonNest }
    public readonly struct StageGround
    {
        public readonly int FirstStage,LastStage;
        public readonly string Region;
        public readonly GroundKind Kind;
        public readonly float PlanY;
        public readonly Color FloorTint;
        public StageGround(int first,int last,string region,GroundKind kind,Color tint)
        {FirstStage=first;LastStage=last;Region=region;Kind=kind;PlanY=ParallaxWorld.GroundPlanY;FloorTint=tint;}
    }
    [Serializable]
    public sealed class ParallaxLayer
    {
        public float Factor;
        public float Width;
        public bool MirrorAlternating;
        public Transform[] Tiles;
    }

    // Stored tiles repeat at seamless boundaries. Travel is owned by the session presentation.
    public sealed class ParallaxWorld : MonoBehaviour
    {
        public ParallaxLayer[] Layers = Array.Empty<ParallaxLayer>();
        public SpriteRenderer Gate;
        public Sprite GateClosed, GateOpen;
        public bool DragonTheme;
        public SpriteRenderer[] GroundTiles=Array.Empty<SpriteRenderer>();
        // 1280x720 plan space. This is the contact seam, not the tall grass tips.
        public const float GroundPlanY=590;
        static readonly StageGround[] Grounds={
            new StageGround(1,10,"meadow",GroundKind.Meadow,Color.white),
            new StageGround(11,20,"forest",GroundKind.Forest,new Color32(202,211,174,255)),
            // Design contracts only; the current catalog still stops at stage 20.
            new StageGround(21,30,"mine",GroundKind.Quarry,new Color32(191,192,184,255)),
            new StageGround(31,40,"ridge",GroundKind.Ridge,new Color32(202,210,215,255)),
            new StageGround(41,50,"dragon_nest",GroundKind.DragonNest,new Color32(212,161,142,255))};
        int groundRegion=-1;
        public static StageGround GroundForStage(int stage)
        {
            if(stage<1||stage>50)throw new ArgumentOutOfRangeException(nameof(stage));
            return Grounds[(stage-1)/10];
        }
        public static Vector3 GroundPoint(float x,int stage)=>PlanPoint(x,GroundForStage(stage).PlanY);
        public void RefreshGround(int stage)
        {
            var ground=GroundForStage(stage);
            if(groundRegion==ground.FirstStage)return;groundRegion=ground.FirstStage;
            foreach(var tile in GroundTiles)if(tile)tile.color=ground.FloorTint;
        }
        MaterialPropertyBlock gateProperties;
        static readonly int GateLift=Shader.PropertyToID("_GateLift");
        public static float TravelDistance(TransitionView transition) => transition!=null&&transition.FromStage==10&&transition.ToStage==11?1020:1920;
        // A single ground object, never a repeating parallax tile.
        public void ShowStage(int stage,TransitionView transition,bool defeated,bool reduced)
        {
            if(DragonTheme){RefreshGround(stage);if(Gate)Gate.gameObject.SetActive(false);return;}
            if(!Gate)return;
            bool travel=transition!=null&&transition.Kind==TransitionKind.Travel;
            bool leaving=travel&&transition.FromStage==11&&transition.ToStage==12;
            Gate.gameObject.SetActive(stage==10||stage==11||leaving);
            if(!Gate.gameObject.activeSelf)return;
            float x=stage==10?1120:100;
            if(travel&&!reduced)x+=leaving?-TravelDistance(transition)*transition.Progress:TravelDistance(transition)*(1-transition.Progress);
            Gate.transform.localPosition=GroundPoint(x,stage);
            // Keep the same gate image and stone frame. Only its iron grille lifts.
            float lift=stage>=11?1:defeated?(transition?.Kind==TransitionKind.EnemyDefeat?transition.Progress:1):0;
            Gate.sprite=GateClosed;
            if(gateProperties==null)gateProperties=new MaterialPropertyBlock();
            Gate.GetPropertyBlock(gateProperties);gateProperties.SetFloat(GateLift,lift);Gate.SetPropertyBlock(gateProperties);
            Gate.color=new Color(1,1,1,travel&&reduced?(leaving?1-transition.Progress:transition.Progress):1);
        }
        public float TravelPixels { get; private set; }
        public const float UnitsPerPixel = 6.2f / 720f;
        public void ShowTravel(float pixels,bool reduced)
        {
            TravelPixels=pixels;
            foreach(var layer in Layers)
            {
                float offset=reduced?0:Mathf.Repeat(pixels*UnitsPerPixel*layer.Factor,layer.Width*(layer.MirrorAlternating?2:1));
                for(int i=0;i<layer.Tiles.Length;i++)
                {
                    var tile=layer.Tiles[i];if(!tile)continue;
                    tile.localPosition=new Vector3((i-layer.Tiles.Length/2)*layer.Width-offset,0,0);
                }
            }
        }
        public static Vector3 PlanPoint(float x,float y) => new Vector3((x-640)*UnitsPerPixel,(360-y)*UnitsPerPixel,0);
    }
}
