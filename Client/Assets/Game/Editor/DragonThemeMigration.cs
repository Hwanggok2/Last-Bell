using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBell.Editor
{
    // Explicit saved-asset update. Product identity and save directory remain compatible.
    public static class DragonThemeMigration
    {
        const string DataPath="Assets/Game/Data/Generated/mvp-catalog.json";
        public const string Version="0.9.3";
        [Serializable] sealed class OperationReport { public string operation,status,utc,error; }
        static bool queued;
        public static void BeginApply()=>Queue("apply",Apply);
        public static void BeginObservedBuild()=>Queue("observed-build",BuildObservedWindows);
        public static void BeginReleaseBuild()=>Queue("release-build",BuildWindows);
        static void Queue(string name,Action action)
        {
            if(queued||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("A Dragon operation is already running.");
            queued=true;Report(name,"queued",null);
            EditorApplication.CallbackFunction callback=null;
            callback=()=>
            {
                EditorApplication.update-=callback;
                try{Report(name,"running",null);action();Report(name,"complete",null);}
                catch(Exception error){Report(name,"failed",error.ToString());Debug.LogException(error);}
                finally{queued=false;}
            };
            EditorApplication.update+=callback;EditorApplication.QueuePlayerLoopUpdate();
        }
        static void Report(string name,string status,string error)
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"dragon-v4-operation.json"),JsonUtility.ToJson(new OperationReport{operation=name,status=status,utc=DateTime.UtcNow.ToString("O"),error=error},true));
        }

        [MenuItem("Last Bell/Dragon/Apply Saved Theme")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before the saved theme migration.");
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(loaded.IsValid()&&loaded.isLoaded&&loaded.isDirty)throw new InvalidOperationException("Preserve unsaved Mvp scene edits before migration.");
            var data=ReadData();
            DragonUiMigration.ConfirmedThirdIcon="down";
            PresentationArtImporter.MigrateExisting();
            var art=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath);
            CheckStageArt(data,art);
            DragonWorldMigration.ApplyPrefab();
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try
            {
                var view=root.GetComponent<MvpView>();
                DragonUiMigration.Apply(view);
                DragonV093GameplayMigration.Configure(view);
                DragonV4FeedbackMigration.Configure(view);
                DragonDialogueMigration.Apply(view);
                PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}

            var audioRoot=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/PresentationAudio.prefab");
            try{DragonV4FeedbackMigration.Configure(audioRoot.GetComponent<BattleAudio>());PrefabUtility.SaveAsPrefabAsset(audioRoot,"Assets/Game/Prefabs/PresentationAudio.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(audioRoot);}

            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                var presenters=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MvpPresenter>(true)).ToArray();
                if(presenters.Length!=1)throw new InvalidDataException("Expected one saved MVP presenter.");
                var p=presenters[0];
                p.Art=art;DragonUiMigration.Apply(p);
                DragonV093GameplayMigration.Configure(p.View);
                DragonV4FeedbackMigration.Configure(p.View);DragonV4FeedbackMigration.Configure(p.Presentation.Audio);
                DragonDialogueMigration.Apply(p.Presentation.Overlay);
                p.Presentation.Telegraphs.WarningOpacity=.92f;p.Presentation.Telegraphs.WarningStroke=4;
                Dirty(p.Presentation.Telegraphs);
                DragonWorldMigration.Configure(p.Presentation.World);
                ConfigureActor(p.PlayerActor,art,"dragon_hero_on",true,500);
                ConfigureActor(p.EnemyActor,art,data.GetEnemy(data.GetStage(1).enemyId).artId,false,900);
                p.WorldCamera.backgroundColor=new Color32(112,183,202,255);
                p.WorldCamera.orthographic=true;p.WorldCamera.orthographicSize=3.1f;
                p.WorldCamera.transform.position=new Vector3(0,-1.04f,-10);
                if(p.Background)p.Background.gameObject.SetActive(false);
                var boot=p.GetComponent<GameBootstrap>();
                if(!boot)boot=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<GameBootstrap>(true)).Single();
                boot.CatalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
                foreach(var value in new UnityEngine.Object[]{p,boot,p.WorldCamera,p.WorldCamera.transform,p.Presentation,p.Presentation.World})Dirty(value);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();
            Debug.Log("DRAGON_V4_SAVED_THEME_APPLIED");
        }

        static void ConfigureActor(ActorView actor,ArtCatalog art,string id,bool right,float x)
        {
            actor.DefaultCatalog=art;actor.DefaultActorId=id;actor.DefaultFacesRight=right;
            actor.transform.localPosition=ParallaxWorld.GroundPoint(x,1);actor.transform.localScale=Vector3.one;
            actor.ApplyArt(art.Find(id),right);
            foreach(var value in new UnityEngine.Object[]{actor,actor.transform,actor.Body,actor.Body.transform})Dirty(value);
        }
        static void Dirty(UnityEngine.Object value)
        {EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
        static GameCatalog ReadData()
        {
            var asset=AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if(!asset)throw new FileNotFoundException(DataPath);
            var data=JsonUtility.FromJson<GameCatalog>(asset.text);data.ValidateOrThrow();
            if(data.dataVersion!="mvp1.16.0"||data.mvpLastStage!=20)throw new InvalidDataException("Dragon data must retain the existing twenty playable stages.");
            return data;
        }
        static void CheckStageArt(GameCatalog data,ArtCatalog art)
        {
            if(!art||art.Find("dragon_hero_on")==null)throw new InvalidDataException("Dragon player art is missing.");
            foreach(var stage in data.stages)
            {
                var enemy=art.Find(data.GetEnemy(stage.enemyId).artId);
                if(enemy==null)throw new InvalidDataException("Missing stage art: "+stage.enemyId);
                if(stage.number>=11&&stage.number<=20)
                    foreach(var motion in new[]{ActorMotion.Idle,ActorMotion.Attack,ActorMotion.Stun})
                        if(enemy.FindClip(motion)==null||enemy.FindClip(motion).frames.Length<6)
                            throw new InvalidDataException("Missing forest motion: "+stage.number+" / "+motion);
            }
            var hero=art.Find("dragon_hero_on");
            foreach(var motion in new[]{ActorMotion.Idle,ActorMotion.Run,ActorMotion.Attack,ActorMotion.DodgeUp,ActorMotion.DodgeBack,ActorMotion.DodgeDown,ActorMotion.Hit})
                if(hero.FindClip(motion)==null||hero.FindClip(motion).frames.Length<6)throw new InvalidDataException("Missing native hero motion: "+motion);
            foreach(var character in data.characters.Where(x=>x.availableInMvp))
            {
                var actor=art.Find(character.artId);
                if(actor==null)throw new InvalidDataException("Missing selectable character: "+character.id);
                foreach(var motion in new[]{ActorMotion.Idle,ActorMotion.Run,ActorMotion.Attack,ActorMotion.DodgeUp,ActorMotion.DodgeBack,ActorMotion.DodgeDown,ActorMotion.Hit})
                    if(actor.FindClip(motion)==null||actor.FindClip(motion).frames.Length<6)throw new InvalidDataException("Missing character motion: "+character.id+" / "+motion);
            }
        }

        [MenuItem("Last Bell/Dragon/Validate Saved Theme")]
        public static void Validate()
        {
            var data=ReadData();var art=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath);
            CheckStageArt(data,art);PresentationArtImporter.Validate();MvpSceneBuilder.Validate();
            DragonUiMigration.Validate(AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>());
            var scene=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive);
            try
            {
                var p=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MvpPresenter>(true)).Single();
                if(p.ValidateReferences().Length>0||p.Art!=art||p.PlayerActor.DefaultActorId!="dragon_hero_on")throw new InvalidDataException("Saved Dragon presenter references are incomplete.");
                DragonUiMigration.Validate(p);
                DragonV093GameplayMigration.Validate(p.View);
                DragonDialogueMigration.Validate(p.Presentation.Overlay);
                DragonV4FeedbackMigration.Validate(p.View,p.Presentation.Audio);
                var world=p.Presentation.World;
                if(!world.DragonTheme||world.Layers.Length!=2||world.Layers.Any(x=>!x.MirrorAlternating||x.Tiles.Length!=7||x.Tiles.Any(t=>!t||!t.GetComponent<SpriteRenderer>().sprite))
                    ||world.GroundTiles.Length!=7||world.GroundTiles.Any(x=>!x))
                    throw new InvalidDataException("Saved meadow parallax layers are incomplete.");
                if(!p.Presentation.Audio||!p.Presentation.Audio.BattleMusic||!p.Presentation.Audio.LobbyMusic)throw new InvalidDataException("Saved audio references are incomplete.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            Debug.Log("DRAGON_V4_SAVED_THEME_VALIDATED");
        }

        [MenuItem("Last Bell/Dragon/Build Windows")]
        public static void BuildWindows()=>Build(false);
        public static void BuildObservedWindows()=>Build(true);
        static void Build(bool observed)
        {
            Validate();
            // Keep companyName/productName: renaming them would silently change saves.
            PlayerSettings.bundleVersion=Version;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            string folder=observed?"Dragon-v4-QA":"Dragon-v4";
            string destination=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/",folder,"LastBellMvp.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{MvpSceneBuilder.ScenePath},locationPathName=destination,target=BuildTarget.StandaloneWindows64,
                options=observed?BuildOptions.Development:BuildOptions.None,
                // Continue the existing Dragon playtest profile; first launch starts at Stage 1.
                // The symbol names the save scope, not this build's version.
                extraScriptingDefines=new[]{"LASTBELL_PLAYTEST_092"}
            });
            Debug.Log($"DRAGON_V4_BUILD: {report.summary.result}; {report.summary.totalSize} bytes; {report.summary.totalErrors} errors; {destination}");
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Dragon v4 build failed.");
        }
    }
}
