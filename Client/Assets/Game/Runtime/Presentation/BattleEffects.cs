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
        Vector2[] origins,fragmentOrigins,velocities;
        public const float PopupDuration = .65f;
        // The canvas uses 960x540; this is 96x72 in the 1280x720 planning preview.
        static readonly Vector2 PopupTravel = new Vector2(72,54);
        float shake,shakeStrength,effectClock;
        Vector3 worldOrigin;

        void Awake()
        {
            numberLife=new float[Numbers.Length];origins=new Vector2[Numbers.Length];
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
        public void Hit(int amount,bool critical,Vector2 numberPosition,Vector2 targetPosition,bool lethal=false)
        {
            if(numberLife==null)return;
            int n=0;for(int i=1;i<numberLife.Length;i++)if(numberLife[i]<numberLife[n])n=i;
            var popup=Numbers[n];popup.SetValue(amount,critical);popup.color=new Color(1,.77f,.32f);
            popup.rectTransform.sizeDelta=new Vector2(critical?196:150,critical?50:35);
            origins[n]=numberPosition;popup.rectTransform.anchoredPosition=numberPosition;popup.gameObject.SetActive(true);numberLife[n]=PopupDuration;
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
                Numbers[i].rectTransform.anchoredPosition=origins[i]+(ReducedShake?Vector2.zero:PopupTravel*t);
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
