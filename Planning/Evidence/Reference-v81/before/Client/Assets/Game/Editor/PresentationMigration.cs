using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Explicit saved-asset migration. Never constructs the game hierarchy at runtime.
    public static class PresentationMigration
    {
        const string AudioFolder="Assets/Game/Audio/Presentation/";
        const string AudioPrefab="Assets/Game/Prefabs/PresentationAudio.prefab";

        [MenuItem("Last Bell/Presentation/Apply Approved MVP")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before applying saved presentation assets.");
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(loaded.IsValid()&&loaded.isLoaded&&loaded.isDirty)throw new InvalidOperationException("Mvp scene has unsaved changes; preserve/save those before explicit migration.");
            PresentationArtImporter.MigrateExisting();
            var catalog=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath);
            var worldPrefab=ParallaxWorldBuilder.EnsurePrefab();
            var audioPrefab=EnsureAudioPrefab();
            var viewRoot=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try
            {
                var view=viewRoot.GetComponent<MvpView>();
                view.LobbyInput=view.LobbyPanel.GetComponent<CanvasGroup>()??view.LobbyPanel.AddComponent<CanvasGroup>();
                var effects=PresentationVfxBuilder.Ensure(view);effects.CombatVfx.SlashFrames=catalog.diagonalSlash;
                var bell=view.BattlePanel.transform.Find("Bell");view.StaggerIcon=bell?bell.gameObject:null;
                EnsureMuteToggle(view);
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(viewRoot,MvpSceneBuilder.ViewPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(viewRoot);}

            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var root in scene.GetRootGameObjects())foreach(var p in root.GetComponentsInChildren<MvpPresenter>(true))
                {
                    var bindings=PresentationVfxBuilder.Ensure(p.View);
                    p.View.LobbyInput=p.View.LobbyPanel.GetComponent<CanvasGroup>()??p.View.LobbyPanel.AddComponent<CanvasGroup>();
                    var director=p.GetComponent<BattlePresentation>()??p.gameObject.AddComponent<BattlePresentation>();
                    p.Presentation=director;p.Art=catalog;
                    director.Vfx=bindings.CombatVfx;director.Vfx.SlashFrames=catalog.diagonalSlash;
                    director.Banner=bindings.StageBanner;director.Overlay=bindings.Overlay;director.ImpactDanger=bindings.ImpactDanger;
                    var world=p.WorldRoot.GetComponentInChildren<ParallaxWorld>(true);
                    if(!world)world=((GameObject)PrefabUtility.InstantiatePrefab(worldPrefab,p.WorldRoot)).GetComponent<ParallaxWorld>();
                    director.World=world;world.ShowTravel(0,false);
                    var audio=p.GetComponentInChildren<BattleAudio>(true);
                    if(!audio)audio=((GameObject)PrefabUtility.InstantiatePrefab(audioPrefab,p.transform)).GetComponent<BattleAudio>();
                    director.Audio=audio;p.MuteAudio=EnsureMuteToggle(p.View);
                    var bell=p.View.BattlePanel.transform.Find("Bell");p.View.StaggerIcon=bell?bell.gameObject:null;
                    ConfigureActor(p.PlayerActor,catalog,"hero_on",true,500);
                    ConfigureActor(p.EnemyActor,catalog,"gate_sentinel",false,900);
                    if(p.Background)p.Background.gameObject.SetActive(false);
                    var ground=p.WorldRoot.Find("Ground");if(ground)ground.gameObject.SetActive(false);
                    p.WorldCamera.orthographic=true;p.WorldCamera.orthographicSize=3.1f;p.WorldCamera.transform.position=new Vector3(0,0,-10);
                    if(!p.WorldCamera.GetComponent<AudioListener>())p.WorldCamera.gameObject.AddComponent<AudioListener>();
                    p.Effects.WorldRoot=p.WorldRoot;
                    foreach(var obj in new UnityEngine.Object[]{p,p.View,director,p.PlayerActor,p.EnemyActor})
                    {EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
                    MvpSceneBuilder.AttachBootstrap(root,p);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_PRESENTATION_APPLIED");
        }

        static void ConfigureActor(ActorView actor,ArtCatalog catalog,string id,bool right,float x)
        {
            actor.DefaultCatalog=catalog;actor.DefaultActorId=id;actor.DefaultFacesRight=right;
            actor.transform.localPosition=ParallaxWorld.PlanPoint(x,590);actor.transform.localScale=Vector3.one;
            actor.ApplyArt(catalog.Find(id),right);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.Body);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.Body.transform);
        }
        static Toggle EnsureMuteToggle(MvpView view)
        {
            var parent=view.ReducedShake.transform.parent;var old=parent.Find("MuteAudio");
            if(old)return old.GetComponent<Toggle>();
            var box=(RectTransform)parent;if(box.sizeDelta==new Vector2(550,340))box.sizeDelta=new Vector2(550,420);
            ((RectTransform)view.ReducedShake.transform).anchoredPosition=new Vector2(0,54);
            var reduceText=view.ReducedShake.GetComponentInChildren<Text>(true);if(reduceText)reduceText.text="움직임 줄이기";
            var clone=UnityEngine.Object.Instantiate(view.ReducedShake.gameObject,parent,false);clone.name="MuteAudio";
            ((RectTransform)clone.transform).anchoredPosition=new Vector2(0,-22);
            var toggle=clone.GetComponent<Toggle>();toggle.isOn=false;
            clone.GetComponentInChildren<Text>(true).text="소리 끄기";return toggle;
        }
        static GameObject EnsureAudioPrefab()
        {
            var old=AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab);if(old)return old;
            var root=new GameObject("PresentationAudio");
            try
            {
                var audio=root.AddComponent<BattleAudio>();audio.Music=root.AddComponent<AudioSource>();audio.Sfx=root.AddComponent<AudioSource>();
                audio.Music.playOnAwake=audio.Sfx.playOnAwake=false;audio.Music.spatialBlend=audio.Sfx.spatialBlend=0;
                audio.Music.loop=true;audio.Music.volume=0;
                audio.BattleMusic=Clip("battle-bells.wav",true);audio.BossMusic=Clip("boss-bells.wav",true);audio.LobbyMusic=Clip("lobby-bells.wav",true);
                audio.Swing=Clip("knifeSlice.ogg");audio.Hit=Clip("hit.ogg");audio.Spike=Clip("spike.ogg");audio.Arrow=audio.Swing;
                audio.Critical=Clip("critical-bell.wav");audio.Footstep=Clip("footstep03.ogg");
                audio.MiniRoar=Clip("mini-roar.wav");audio.BossRoar=Clip("boss-roar.wav");audio.Lantern=Clip("lantern-ignite.wav");audio.Dodge=Clip("perfect-dodge.wav");
                return PrefabUtility.SaveAsPrefabAsset(root,AudioPrefab);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static AudioClip Clip(string name,bool music=false)
        {
            string path=AudioFolder+name;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);if(!importer)throw new FileNotFoundException(path);
            var settings=importer.defaultSampleSettings;settings.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        [MenuItem("Last Bell/Presentation/Validate Saved MVP")]
        public static void Validate()
        {
            PresentationArtImporter.Validate();MvpSceneBuilder.Validate();
            var view=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath);
            if(!view.GetComponent<MvpView>().LobbyInput)throw new InvalidDataException("Saved lobby input lock is missing.");
            if(!view.GetComponentInChildren<CombatVfx>(true)||!view.GetComponentInChildren<PresentationOverlay>(true)||!view.GetComponentInChildren<StageBannerGraphic>(true))
                throw new InvalidDataException("Saved presentation UI is incomplete.");
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(ParallaxWorldBuilder.PrefabPath)||!AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab))
                throw new InvalidDataException("Saved presentation world/audio is incomplete.");
            var scene=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive);
            try
            {
                int count=0;
                foreach(var root in scene.GetRootGameObjects())foreach(var p in root.GetComponentsInChildren<MvpPresenter>(true))
                {
                    count++;var d=p.Presentation;
                    if(p.ValidateReferences().Length>0||!p.View.LobbyInput||!p.MuteAudio||!d||!d.World||!d.Vfx||!d.Banner||!d.Overlay||!d.ImpactDanger||!d.Audio)
                        throw new InvalidDataException("Saved Mvp scene presentation references are incomplete.");
                    if(!d.Audio.Music||!d.Audio.Sfx||!d.Audio.BattleMusic||!d.Audio.BossMusic||!d.Audio.LobbyMusic||d.Vfx.SlashFrames.Length!=10)
                        throw new InvalidDataException("Saved Mvp audio/slash references are incomplete.");
                }
                if(count!=1)throw new InvalidDataException("Mvp scene must have exactly one presenter.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            Debug.Log("LAST_BELL_PRESENTATION_VALIDATED");
        }
    }
}
