using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Targeted, idempotent saved-asset migration; no actor, economy, reward or run-art rebuild.
    public static class CombatV55Migration
    {
        const string Folder="Assets/Game/Prefabs/CombatV55";
        [MenuItem("Last Bell/Combat/Apply v5.5")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isLoaded&&loaded.isDirty)
                throw new InvalidOperationException("Save scene edits and stop Play mode before migration.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            EnsureEffectsPrefab();EnsureWavePrefab();TuneReleaseTrails();
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{ConfigureView(root.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var r in scene.GetRootGameObjects())foreach(var p in r.GetComponentsInChildren<MvpPresenter>(true))
                {
                    ConfigureView(p.View);
                    var effects=p.WorldRoot.GetComponentInChildren<EnemyAttackEffects>(true);
                    if(!effects)effects=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/EnemyAttackPool.prefab"),p.WorldRoot)).GetComponent<EnemyAttackEffects>();
                    p.Presentation.EnemyEffects=effects;p.Presentation.Telegraphs=effects.GetComponentInChildren<EnemyTelegraphGraphic>(true);
                    p.Presentation.ImpactDanger.gameObject.SetActive(false);
                    if(p.Presentation.Audio)
                    {
                        p.Presentation.Audio.Windup=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Presentation/enemy-windup-v55.wav");
                        Record(p.Presentation.Audio);
                    }
                    Record(p.Presentation);Record(p.View);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_COMBAT_V55_APPLIED");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
        static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
        static void ConfigureView(MvpView view)
        {
            view.Timing.gameObject.SetActive(false);view.Danger.gameObject.SetActive(false);view.NextDanger.gameObject.SetActive(false);
            var gain=view.DodgeResult.StaggerGain;
            var wave=view.transform.Find("PerfectWave");
            if(!wave)
            {
                view.DodgeResult.Clear();view.DodgeResult.gameObject.SetActive(false);
                wave=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PerfectWave.prefab"),view.transform)).transform;
                wave.name="PerfectWave";
            }
            view.DodgeResult=wave.GetComponent<DodgeFeedback>();view.DodgeResult.StaggerGain=gain;
            Stretch((RectTransform)wave);wave.SetAsFirstSibling();view.DodgeResult.Clear();
            view.GetComponent<FeedbackLayout>().Apply(960,540,false);
            Record(view);Record(view.DodgeResult);
        }
        static void EnsureWavePrefab()
        {
            string path=Folder+"/PerfectWave.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path))return;
            var root=new GameObject("PerfectWave",typeof(RectTransform),typeof(CanvasRenderer),typeof(DodgeFeedback));root.layer=5;
            try
            {
                var wave=root.GetComponent<DodgeFeedback>();wave.raycastTarget=false;Stretch(wave.rectTransform);
                var label=new GameObject("PerfectLabel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));label.layer=5;label.transform.SetParent(root.transform,false);
                wave.Label=label.GetComponent<Text>();wave.Label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NanumGothic-Regular.ttf");wave.Label.fontSize=26;wave.Label.alignment=TextAnchor.MiddleCenter;wave.Label.raycastTarget=false;
                var rect=wave.Label.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(140,44);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static void EnsureEffectsPrefab()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/GoldParticles.mat");
            if(!material){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,Folder+"/GoldParticles.mat");}
            var wind=EnsureBurst("EnemyWindup",EnemyEffectKind.Windup,8,material);
            var release=EnsureBurst("EnemyRelease",EnemyEffectKind.Release,8,material);
            var impact=EnsureBurst("EnemyImpact",EnemyEffectKind.Impact,28,material);
            var counter=EnsureBurst("CounterSnap",EnemyEffectKind.Counter,8,material);
            string path=Folder+"/EnemyAttackPool.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path))return;
            var root=new GameObject("EnemyAttackPool");
            try
            {
                var pool=root.AddComponent<EnemyAttackEffects>();
                pool.Windups=Slots(root.transform,wind,3);pool.Releases=Slots(root.transform,release,1);pool.Impacts=Slots(root.transform,impact,2);pool.Counters=Slots(root.transform,counter,1);
                WorldGraphic<EnemyTelegraphGraphic>(root.transform,"GoldHitboxes",0);
                pool.GroundRings=WorldGraphic<EnemyImpactGraphic>(root.transform,"GroundRings",5);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static void TuneReleaseTrails()
        {
            string path=Folder+"/EnemyRelease.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var burst=root.GetComponent<EnemyEffectBurst>();var trails=burst.Trails;
                burst.WeaponRibbons=new LineRenderer[3];
                for(int i=0;i<trails.Length;i++)
                {
                    // Manual points share the same timestamp: nonzero width at age zero is required.
                    trails[i].widthMultiplier=i==1?.095f:.035f;
                    trails[i].alignment=LineAlignment.TransformZ;
                    trails[i].widthCurve=AnimationCurve.Constant(0,1,1);
                    trails[i].startColor=new Color(1,.94f,.77f,.95f);trails[i].endColor=new Color(.91f,.62f,.2f,.45f);
                    var child=root.transform.Find("WeaponRibbon"+i);
                    if(!child){var go=new GameObject("WeaponRibbon"+i);go.transform.SetParent(root.transform,false);child=go.transform;}
                    var line=child.GetComponent<LineRenderer>();if(!line)line=child.gameObject.AddComponent<LineRenderer>();burst.WeaponRibbons[i]=line;
                    line.sharedMaterial=trails[i].sharedMaterial;line.useWorldSpace=true;line.positionCount=24;
                    line.alignment=LineAlignment.TransformZ;line.sortingOrder=5;line.widthMultiplier=i==1?.095f:.035f;
                    line.widthCurve=AnimationCurve.Linear(0,.15f,1,1);line.startColor=new Color(.91f,.62f,.2f,.4f);line.endColor=new Color(1,.94f,.77f,.95f);line.numCapVertices=2;line.enabled=false;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static GameObject EnsureBurst(string name,EnemyEffectKind kind,int capacity,Material material)
        {
            string path=Folder+"/"+name+".prefab";var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(old)return old;
            var root=new GameObject(name);
            try
            {
                var fx=root.AddComponent<EnemyEffectBurst>();fx.Kind=kind;fx.Capacity=capacity;
                fx.Particles=root.AddComponent<ParticleSystem>();fx.Particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=fx.Particles.main;main.loop=false;main.playOnAwake=false;main.duration=1;main.startLifetime=.34f;main.startSpeed=0;main.startSize=.03f;main.maxParticles=capacity;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
                var emission=fx.Particles.emission;emission.enabled=false;
                var shape=fx.Particles.shape;shape.enabled=false;
                var color=fx.Particles.colorOverLifetime;color.enabled=true;
                var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.5f),new GradientAlphaKey(0,1)});color.color=gradient;
                var renderer=fx.Particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=6;renderer.renderMode=ParticleSystemRenderMode.Billboard;
                if(kind==EnemyEffectKind.Release)
                {
                    fx.Trails=new TrailRenderer[3];
                    for(int i=0;i<3;i++)
                    {
                        var go=new GameObject("WeaponTrail"+i);go.transform.SetParent(root.transform,false);var t=go.AddComponent<TrailRenderer>();fx.Trails[i]=t;
                        t.sharedMaterial=material;t.sortingOrder=4;t.emitting=false;t.autodestruct=false;t.time=10000;t.minVertexDistance=.005f;t.widthMultiplier=i==1?.055f:.018f;
                        t.widthCurve=AnimationCurve.Linear(0,0,1,1);t.startColor=new Color(1,.94f,.77f,.95f);t.endColor=new Color(.91f,.62f,.2f,.08f);t.numCapVertices=2;t.enabled=false;
                    }
                }
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static EnemyEffectBurst[] Slots(Transform parent,GameObject prefab,int count)
        {
            var slots=new EnemyEffectBurst[count];for(int i=0;i<count;i++)slots[i]=((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).GetComponent<EnemyEffectBurst>();return slots;
        }
        static T WorldGraphic<T>(Transform parent,string name,int order) where T:MaskableGraphic
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasRenderer),typeof(T));root.transform.SetParent(parent,false);
            var c=root.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.sortingOrder=order;c.overrideSorting=true;
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(960,540);rect.localScale=Vector3.one*(6.2f/540);
            var graphic=root.GetComponent<T>();graphic.raycastTarget=false;return graphic;
        }
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;}
        public static void Validate()
        {
            var view=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            if(view.Timing.gameObject.activeSelf||view.Danger.gameObject.activeSelf||view.DodgeResult.transform.parent!=view.transform)throw new InvalidDataException("v5.5 HUD/wave layout missing");
            var pool=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/EnemyAttackPool.prefab").GetComponent<EnemyAttackEffects>();
            if(pool.ParticleCapacity!=96||!pool.GroundRings||!pool.GetComponentInChildren<EnemyTelegraphGraphic>())throw new InvalidDataException("v5.5 saved pool incomplete");
            foreach(string name in new[]{"EnemyWindup","EnemyRelease","EnemyImpact","PerfectWave","CounterSnap"})if(!AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+name+".prefab"))throw new InvalidDataException(name);
            Debug.Log("LAST_BELL_COMBAT_V55_VALIDATED");
        }
    }
}
