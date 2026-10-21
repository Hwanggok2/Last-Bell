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
