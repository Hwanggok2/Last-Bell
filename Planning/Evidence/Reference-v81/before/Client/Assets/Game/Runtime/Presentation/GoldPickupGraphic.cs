using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // One bounded UI mesh; no object or audio voice is allocated per gold unit.
    public sealed class GoldPickupGraphic : MaskableGraphic
    {
        public Text Label;
        MvpPresenter owner;
        GoldCollection collection;
        Vector2 source;
        Vector2 batchSize,target;
        double elapsed;
        float soundAt=-10;
        int arrived;
        bool waitingForStory;
        public int DotCount => collection?.Count??0;
        public long Pending => collection==null?0:collection.Amount-collection.Collected(elapsed);
        public void Bind(MvpPresenter presenter){owner=presenter;collection=null;elapsed=0;arrived=0;soundAt=-10;SetVerticesDirty();}
        public void Begin(long amount,Vector2 from,bool wait)
        {
            collection=new GoldCollection(amount);source=from;batchSize=rectTransform.rect.size;target=Target;elapsed=0;arrived=0;soundAt=-10;waitingForStory=wait;SetVerticesDirty();
        }
        public void Sample(float delta)
        {
            if(!owner||owner.Session==null)return;
            var s=owner.Session;
            if(collection!=null&&batchSize!=rectTransform.rect.size)collection=null;
            if(s.Phase==SessionPhase.Lobby||s.Phase==SessionPhase.Prologue||s.Phase==SessionPhase.DeathResult||s.Phase==SessionPhase.MvpComplete)collection=null;
            bool show=owner.Presentation.ShowsBattle(s)||s.Phase==SessionPhase.RewardChoice;
            Label.gameObject.SetActive(show);
            if(waitingForStory&&s.Phase!=SessionPhase.StoryDialogue)waitingForStory=false;
            if(collection!=null&&!s.CombatSuspended&&!waitingForStory)
            {
                elapsed+=delta;
                bool reduced=owner.Effects.ReducedShake;
                int next=reduced?(elapsed>=.12?collection.Count:0):collection.Arrived(elapsed);
                if(next>arrived&&elapsed-soundAt>=.075)
                {
                    soundAt=(float)elapsed;var audio=owner.Presentation.Audio;
                    if(audio)audio.Play(audio.GoldPickup,.26f);
                }
                arrived=next;
                if(elapsed>=(reduced?.12:GoldCollection.Duration))collection=null;
            }
            Label.text="골드 "+MvpPresenter.WalletAmount(s.Gold-Pending);
            SetVerticesDirty();
        }
        Vector2 Target
        {
            get
            {
                var r=Label.rectTransform;
                // Right-aligned text: destination is the center of the last digit.
                return rectTransform.InverseTransformPoint(r.TransformPoint(new Vector3(r.rect.xMax-Label.fontSize*.25f,r.rect.center.y,0)));
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(collection==null||waitingForStory||owner&&owner.Effects.ReducedShake)return;
            float scale=rectTransform.rect.height/540f;
            int first=collection.Arrived(elapsed);
            int last=collection.Count<=1?0:Mathf.Min(collection.Count-1,(int)(elapsed/GoldCollection.EmissionSeconds*(collection.Count-1)));
            for(int i=first;i<=last&&i<collection.Count;i++)
            {
                double age=elapsed-collection.StartAt(i);if(age<0||age>=GoldCollection.FlightSeconds)continue;
                // Discrete square pixels begin across the torso and fly straight.
                // Different origins keep dense rewards from becoming one long strand.
                float x=Scatter((uint)i+1)*48*scale,y=Scatter((uint)i+17003)*76*scale;
                var from=source+new Vector2(x,y);
                var p=Vector2.Lerp(from,target,(float)(age/GoldCollection.FlightSeconds));
                // Large rewards stay granular instead of merging into a thick ribbon.
                float pixels=collection.Count>2000?1:collection.Count>150?2:3;
                PixelVfxMesh.Square(vh,p,pixels*scale,Scatter((uint)i+509)> .3f?PixelVfxMesh.Cream:PixelVfxMesh.Gold);
            }
        }
        static float Scatter(uint seed)
        {
            seed^=seed>>16;seed*=0x7feb352du;seed^=seed>>15;seed*=0x846ca68bu;seed^=seed>>16;
            return (seed&65535)/65535f-.5f;
        }
    }
}
