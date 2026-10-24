using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class BattleEffects : MonoBehaviour
    {
        public RectTransform Root;
        public PixelNumber[] Numbers;
        public Image[] Fragments;
        public Transform WorldRoot;
        public bool ReducedShake;
        public bool Paused;
        float[] numberLife,fragmentLife;
        int[] numberLanes;
        long[] numberDamageMilli;
        Vector2[] origins,fragmentOrigins,velocities;
        public const float PopupDuration = .85f;
        // The canvas uses 960x540; this is 96x72 in the 1280x720 planning preview.
        static readonly Vector2 PopupTravel = new Vector2(72,54);
        float shake,shakeStrength,effectClock;
        Vector3 worldOrigin;

        void Awake()
        {
            numberLife=new float[Numbers.Length];origins=new Vector2[Numbers.Length];
            numberLanes=new int[Numbers.Length];
            numberDamageMilli=new long[Numbers.Length];
            fragmentLife=new float[Fragments.Length];fragmentOrigins=new Vector2[Fragments.Length];velocities=new Vector2[Fragments.Length];
            if(WorldRoot)worldOrigin=WorldRoot.localPosition;
            Clear();
        }
        public void Clear()
        {
            foreach(var x in Numbers)if(x)x.gameObject.SetActive(false);
            foreach(var x in Fragments)if(x)x.gameObject.SetActive(false);
            if(numberLife!=null)System.Array.Clear(numberLife,0,numberLife.Length);
            if(fragmentLife!=null)System.Array.Clear(fragmentLife,0,fragmentLife.Length);
            shake=0;if(WorldRoot)WorldRoot.localPosition=worldOrigin;
        }
        // This is the only writer of world shake. Enemy hits never move the screen overlay/HUD.
        public void SampleEnemyShake(float age,Camera camera)
        {
            if(!WorldRoot||!camera)return;
            float amount=!ReducedShake&&age>=0&&age<.09f?3*(1-age/.09f):0;
            var offset=new Vector3(Mathf.Sin(age*280),Mathf.Cos(age*230)*.55f)*amount*(camera.orthographicSize*2/Mathf.Max(1,Screen.height));
            if(amount>0)WorldRoot.localPosition=worldOrigin+offset;
            else if(shake<=0||ReducedShake)WorldRoot.localPosition=worldOrigin;
        }
        public Vector3 WorldOffset=>WorldRoot?WorldRoot.localPosition-worldOrigin:Vector3.zero;
        public void Hit(int amount,bool critical,Vector2 numberPosition,Vector2 targetPosition,bool lethal=false,bool companion=false)
            =>HitMilli((long)amount*1000,critical,numberPosition,targetPosition,lethal,companion);
        public void HitMilli(long damageMilli,bool critical,Vector2 numberPosition,Vector2 targetPosition,bool lethal=false,bool companion=false)
        {
            if(numberLife==null)return;
            int n=-1;
            float scale=Root?Root.rect.height/720:.75f;
            // A party volley has one accurate total; each contact still emits its own fragments and hit flash.
            if(companion)for(int i=0;i<numberLife.Length;i++)
                if(Numbers[i].Companion&&numberLife[i]>PopupDuration-.08f&&Mathf.Abs(origins[i].x-numberPosition.x)<100*scale){n=i;break;}
            bool merged=n>=0;
            if(!merged)
            {
                n=0;for(int i=1;i<numberLife.Length;i++)if(numberLife[i]<numberLife[n])n=i;
                int lane=0;
                for(int candidate=0;candidate<3;candidate++)
                {
                    bool occupied=false;
                    for(int i=0;i<numberLife.Length;i++)if(i!=n&&numberLife[i]>PopupDuration-.3f&&numberLanes[i]==candidate&&Mathf.Abs(origins[i].x-numberPosition.x)<100*scale){occupied=true;break;}
                    lane=candidate;if(!occupied)break;
                }
                numberLanes[n]=lane;numberPosition+=new Vector2(lane==1?-24:lane==2?24:0,lane*62)*scale;
                origins[n]=numberPosition;numberLife[n]=PopupDuration;numberDamageMilli[n]=0;
            }
            numberDamageMilli[n]+=damageMilli;
            var popup=Numbers[n];popup.SetValue((int)System.Math.Min(99999,System.Math.Round(numberDamageMilli[n]/1000d,System.MidpointRounding.AwayFromZero)),critical);
            popup.Companion=companion;popup.color=new Color(1,.77f,.32f);
            popup.rectTransform.sizeDelta=new Vector2(critical?336:264,critical?88:64)*scale;
            if(!merged)popup.rectTransform.anchoredPosition=numberPosition;
            popup.gameObject.SetActive(true);
            int count=ReducedShake?2:critical?8:5;
            for(int i=0;i<Fragments.Length&&count>0;i++)if(fragmentLife[i]<=0)
            {
                fragmentLife[i]=.3f;fragmentOrigins[i]=targetPosition;velocities[i]=new Vector2(Random.Range(-85f,85f),Random.Range(30f,100f));
                Fragments[i].rectTransform.anchoredPosition=targetPosition;Fragments[i].gameObject.SetActive(true);count--;
            }
            // A critical killing blow upgrades one shake instead of stacking two offsets.
            if((critical||lethal)&&!ReducedShake)
            {
                float strength=lethal?5f/3f:1f;
                shakeStrength=shake>0?Mathf.Max(shakeStrength,strength):strength;
                shake=Mathf.Max(shake,lethal?.12f:.08f);
            }
        }
        void Update()
        {
            if(numberLife==null||Paused)return;float dt=Time.unscaledDeltaTime;effectClock+=dt;
            for(int i=0;i<Numbers.Length;i++)if(numberLife[i]>0)
            {
                numberLife[i]-=dt;float t=Mathf.Clamp01((PopupDuration-numberLife[i])/PopupDuration);
                var position=origins[i]+(ReducedShake?Vector2.zero:PopupTravel*t);
                var canvas=Numbers[i].canvas;float pixelScale=canvas?canvas.scaleFactor:1;
                Numbers[i].rectTransform.anchoredPosition=new Vector2(Mathf.Round(position.x*pixelScale),Mathf.Round(position.y*pixelScale))/pixelScale;
                Color c=Numbers[i].color;c.a=Mathf.Clamp01(numberLife[i]/.25f);Numbers[i].color=c;
                if(numberLife[i]<=0)Numbers[i].gameObject.SetActive(false);
            }
            for(int i=0;i<Fragments.Length;i++)if(fragmentLife[i]>0)
            {
                fragmentLife[i]-=dt;float t=.3f-fragmentLife[i];Fragments[i].rectTransform.anchoredPosition=fragmentOrigins[i]+velocities[i]*t+Vector2.down*500*t*t;
                if(fragmentLife[i]<=0)Fragments[i].gameObject.SetActive(false);
            }
            if(WorldRoot)
            {
                shake=Mathf.Max(0,shake-dt);
                WorldRoot.localPosition=worldOrigin+(shake>0&&!ReducedShake?new Vector3(Mathf.Sin(effectClock*230)*.035f,Mathf.Cos(effectClock*180)*.02f)*shakeStrength:Vector3.zero);
            }
        }
    }
}
