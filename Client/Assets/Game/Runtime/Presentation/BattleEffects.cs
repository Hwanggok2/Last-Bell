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
        float[] numberLife,fragmentLife;
        Vector2[] origins,fragmentOrigins,velocities;
        float shake;
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
        public Vector3 WorldOffset=>WorldRoot?WorldRoot.localPosition-worldOrigin:Vector3.zero;
        public void Hit(int amount,bool critical,Vector2 numberPosition,Vector2 targetPosition)
        {
            if(numberLife==null)return;
            int n=0;for(int i=1;i<numberLife.Length;i++)if(numberLife[i]<numberLife[n])n=i;
            var popup=Numbers[n];popup.SetValue(amount,critical);popup.color=new Color(1,.77f,.32f);
            popup.rectTransform.sizeDelta=new Vector2(critical?196:150,critical?50:35);
            origins[n]=numberPosition;popup.rectTransform.anchoredPosition=numberPosition;popup.gameObject.SetActive(true);numberLife[n]=.45f;
            int count=critical?8:5;
            for(int i=0;i<Fragments.Length&&count>0;i++)if(fragmentLife[i]<=0)
            {
                fragmentLife[i]=.3f;fragmentOrigins[i]=targetPosition;velocities[i]=new Vector2(Random.Range(-85f,85f),Random.Range(30f,100f));
                Fragments[i].rectTransform.anchoredPosition=targetPosition;Fragments[i].gameObject.SetActive(true);count--;
            }
            if(critical&&!ReducedShake)shake=.08f;
        }
        void Update()
        {
            if(numberLife==null)return;float dt=Time.unscaledDeltaTime;
            for(int i=0;i<Numbers.Length;i++)if(numberLife[i]>0)
            {
                numberLife[i]-=dt;float t=.45f-numberLife[i];Numbers[i].rectTransform.anchoredPosition=origins[i]+Vector2.up*t*40;
                Color c=Numbers[i].color;c.a=Mathf.Clamp01(numberLife[i]/.12f);Numbers[i].color=c;
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
                WorldRoot.localPosition=worldOrigin+(shake>0&&!ReducedShake?new Vector3(Mathf.Sin(Time.unscaledTime*230)*.035f,Mathf.Cos(Time.unscaledTime*180)*.02f):Vector3.zero);
            }
        }
    }
}
