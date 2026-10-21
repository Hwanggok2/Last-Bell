using UnityEngine;
namespace LastBell
{
    public enum EnemyEffectKind { Windup, Release, Impact, Counter }
    // Authored pool instances. Manual simulation follows Core time, including pause.
    public sealed class EnemyEffectBurst : MonoBehaviour
    {
        public EnemyEffectKind Kind;
        public ParticleSystem Particles;
        public TrailRenderer[] Trails=System.Array.Empty<TrailRenderer>();
        public LineRenderer[] WeaponRibbons=System.Array.Empty<LineRenderer>();
        public int Capacity=8;
        public double StartedMs {get;private set;}=-10000;
        public string Pattern {get;private set;}
        public float Lifetime {get;private set;}
        public bool Active {get;private set;}
        float simulatedAge,trailProgress;Transform world;
        readonly Vector3[] trailPoints=new Vector3[24];
        static readonly Color Gold=new Color32(233,184,90,255),Cream=new Color32(255,242,202,255),Mint=new Color32(158,227,212,255);
        public static Vector3 Position(Transform root,float x,float y)=>root.TransformPoint(ParallaxWorld.PlanPoint(x/.75f,(y-CombatGeometry.HomeY)/.75f+590));
        public void Clear(){Active=false;if(Particles)Particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);foreach(var trail in Trails){trail.emitting=false;trail.Clear();trail.enabled=false;}foreach(var ribbon in WeaponRibbons)ribbon.enabled=false;}
        public void StartBurst(Transform worldRoot,string pattern,double timeMs,float lifetime,bool parried=false)
        {
            Clear();world=worldRoot;Pattern=pattern;StartedMs=timeMs;Lifetime=lifetime;simulatedAge=0;trailProgress=-1;Active=true;
            var at=parried?new Vector2(420,332):Contact(pattern);var source=Source(pattern);
            transform.position=Kind==EnemyEffectKind.Windup?Position(world,source.x,source.y):Position(world,at.x,at.y);
            var main=Particles.main;main.simulationSpace=ParticleSystemSimulationSpace.World;
            Particles.randomSeed=73;Particles.useAutoRandomSeed=false;
            for(int i=0;i<Capacity;i++)
            {
                float angle=i*2.399963f,speed=110+i%5*34;Vector2 d=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                Vector2 p=at,v=d*speed;float life=.26f,size=3+i%3;Color tint=i%3==0?Cream:Gold;
                if(Kind==EnemyEffectKind.Windup){p=source+d*(18+i*3);v=-d*(18+i*3)/lifetime;life=lifetime;size=1.8f;}
                if(Kind==EnemyEffectKind.Release){p=Vector2.Lerp(source,at,i/(float)Capacity);v=new Vector2(-80,-4);life=lifetime;size=1.5f;}
                if(Kind==EnemyEffectKind.Counter){p=new Vector2(375,388)+d*10;v=d*35;life=.12f;size=3;tint=Mint;}
                if(Kind==EnemyEffectKind.Impact)
                {
                    if(i==0){v=Vector2.zero;life=.045f;size=28;tint=Cream;}
                    else if(parried){v=new Vector2(50+i*5,-90-i*5);life=.18f;tint=Cream;size=2;}
                    else if(i>=18){p=new Vector2(at.x+(i-22)*3,438);v=new Vector2(d.x*45,-18-i%3*15);life=.34f;size=7+i%4;tint=new Color(.78f,.65f,.42f,.35f);}
                    else if(pattern=="outer_arc")v=new Vector2(d.x*70,-60-i*10);
                }
                var ep=new ParticleSystem.EmitParams{position=Position(world,p.x,p.y),velocity=new Vector3(v.x,-v.y,0)*(ParallaxWorld.UnitsPerPixel/.75f),
                    startLifetime=life,startSize=size*(ParallaxWorld.UnitsPerPixel/.75f),startColor=tint,rotation=i*37,applyShapeToPosition=false};
                Particles.Emit(ep,1);
            }
            Particles.Simulate(0,true,false,false);
        }
        public void Sample(double clock,bool reduced)
        {
            if(!Active)return;float age=(float)((clock-StartedMs)/1000);
            if(reduced||age>=Lifetime||age<0){Clear();return;}
            if(age>simulatedAge){Particles.Simulate(age-simulatedAge,true,false,false);simulatedAge=age;}
            if(Kind!=EnemyEffectKind.Release)return;
            float p=Mathf.Clamp01(age/Lifetime);p*=p;
            if(p<=trailProgress)return;
            for(int i=0;i<Trails.Length;i++)
            {
                var trail=Trails[i];var head=Path(Pattern,p,i);
                trail.transform.position=Position(world,head.x,head.y);trail.enabled=true;trail.emitting=true;
                if(trailProgress<0)trail.AddPosition(trail.transform.position);
                else
                {
                    for(int j=0;j<trailPoints.Length;j++){float q=Mathf.Lerp(trailProgress,p,(j+1)/24f);Vector2 a=Path(Pattern,q,i);trailPoints[j]=Position(world,a.x,a.y);}
                    trail.AddPositions(trailPoints);
                }
                // The weapon silhouette must exist even on the first sampled frame or after
                // a pause. A deterministic ribbon supplies that silhouette; TrailRenderer
                // supplies the motion history behind its head.
                if(i<WeaponRibbons.Length)
                {
                    var ribbon=WeaponRibbons[i];ribbon.enabled=true;
                    for(int j=0;j<trailPoints.Length;j++){float q=Mathf.Lerp(Mathf.Max(0,p-.55f),p,j/23f);var a=Path(Pattern,q,i);trailPoints[j]=Position(world,a.x,a.y);}
                    ribbon.SetPositions(trailPoints);
                }
            }
            trailProgress=p;
        }
        public static Vector2 Source(string pattern)=>pattern=="outer_arc"?new Vector2(624,270):pattern=="low_sweep"?new Vector2(624,414):pattern=="high_sweep"?new Vector2(624,312):new Vector2(624,358);
        public static Vector2 Contact(string pattern)=>pattern=="outer_arc"?new Vector2(330,433):pattern=="low_sweep"?new Vector2(375,424):pattern=="high_sweep"?new Vector2(375,318):new Vector2(375,358);
        static Vector2 Path(string pattern,float t,int lane)
        {
            var p=Vector2.Lerp(Source(pattern),Contact(pattern),t);
            if(pattern=="outer_arc"){p.y-=Mathf.Sin(t*Mathf.PI)*185;p.x+=(lane-1)*12;}
            else if(pattern=="low_sweep")p.y+=Mathf.Sin(t*Mathf.PI)*22+(lane-1)*4;
            else if(pattern=="high_sweep")p.y-=Mathf.Sin(t*Mathf.PI)*42+(lane-1)*4;
            else p.y+=(lane-1)*5;
            return p;
        }
    }
}
