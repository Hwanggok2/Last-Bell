using System;
using UnityEngine;

namespace LastBell
{
    [Serializable]
    public sealed class ParallaxLayer
    {
        public float Factor;
        public float Width;
        public Transform[] Tiles;
    }

    // Stored tiles repeat at seamless boundaries. Travel is owned by the session presentation.
    public sealed class ParallaxWorld : MonoBehaviour
    {
        public ParallaxLayer[] Layers = Array.Empty<ParallaxLayer>();
        public SpriteRenderer Gate;
        public Sprite GateClosed, GateOpen;
        MaterialPropertyBlock gateProperties;
        static readonly int GateLift=Shader.PropertyToID("_GateLift");
        public static float TravelDistance(TransitionView transition) => transition!=null&&transition.FromStage==10&&transition.ToStage==11?1020:1920;
        // A single ground object, never a repeating parallax tile.
        public void ShowStage(int stage,TransitionView transition,bool defeated,bool reduced)
        {
            if(!Gate)return;
            bool travel=transition!=null&&transition.Kind==TransitionKind.Travel;
            bool leaving=travel&&transition.FromStage==11&&transition.ToStage==12;
            Gate.gameObject.SetActive(stage==10||stage==11||leaving);
            if(!Gate.gameObject.activeSelf)return;
            float x=stage==10?1120:100;
            if(travel&&!reduced)x+=leaving?-TravelDistance(transition)*transition.Progress:TravelDistance(transition)*(1-transition.Progress);
            Gate.transform.localPosition=PlanPoint(x,590);
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
                float offset=reduced?0:Mathf.Repeat(pixels*UnitsPerPixel*layer.Factor,layer.Width);
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
