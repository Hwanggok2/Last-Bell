using System;
using UnityEngine;

namespace LastBell
{
    [CreateAssetMenu(menuName = "Last Bell/Art Catalog")]
    public sealed class ArtCatalog : ScriptableObject
    {
        public ActorSprites[] actors = Array.Empty<ActorSprites>();
        public Sprite gateBackground;
        public Font uiFont;

        public ActorSprites Find(string id)
        {
            foreach (var actor in actors)
                if (actor.id == id) return actor;
            return null;
        }
    }

    [Serializable]
    public sealed class ActorSprites
    {
        public string id;
        public Sprite[] idle = Array.Empty<Sprite>();
        public Sprite[] attack = Array.Empty<Sprite>();
        public Sprite[] stun = Array.Empty<Sprite>();
        public Sprite[] dodgeUp = Array.Empty<Sprite>();
        public Sprite[] dodgeDown = Array.Empty<Sprite>();
        public Sprite[] dodgeBack = Array.Empty<Sprite>();
        public Sprite[] dodgeForward = Array.Empty<Sprite>();
        public bool facesRight;
        public float height = 1f;
    }
}
