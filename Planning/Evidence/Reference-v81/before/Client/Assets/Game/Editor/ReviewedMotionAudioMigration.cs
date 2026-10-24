using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBell.Editor
{
    public static class ReviewedMotionAudioMigration
    {
        const string AudioRoot="Assets/Game/Audio/";
        const string AudioPrefab="Assets/Game/Prefabs/PresentationAudio.prefab";
        static readonly Dictionary<string,string> Slots=new Dictionary<string,string>
        {
            {nameof(BattleAudio.Swing),"Combat-v72/sword-fire.wav"},
            {nameof(BattleAudio.Hit),"Combat-v72/player-body-hit.wav"},
            {nameof(BattleAudio.EnemySwing),"Reviewed-v4/sword-clash.wav"},
            {nameof(BattleAudio.EnemyImpact),"Reviewed-v4/hit-sharp.wav"},
            {nameof(BattleAudio.Windup),"Reviewed-v4/cue-low.wav"},
            {nameof(BattleAudio.Critical),"Reviewed-v4/cue-low.wav"},
            {nameof(BattleAudio.LongBell),"Reviewed-v4/bell-low-long.wav"},
            {nameof(BattleAudio.Dodge),"Combat-v72/dodge-whoosh.wav"},
            {nameof(BattleAudio.PerfectDodge),"Reviewed-v4/perfect-parry.wav"},
            {nameof(BattleAudio.Footstep),"Reviewed-v4/footstep-stone.wav"},
            {nameof(BattleAudio.Lantern),"Reviewed-v4/lantern-warm.wav"},
            {nameof(BattleAudio.BattleMusic),"Reviewed-v4/bgm-road.ogg"},
            {nameof(BattleAudio.BossMusic),"Reviewed-v4/bgm-boss.ogg"},
            {nameof(BattleAudio.LobbyMusic),"Reviewed-v4/bgm-sanctuary.ogg"},
            {nameof(BattleAudio.QuarterMusic),"Reviewed-v4/bgm-quarter.ogg"}
        };

        [MenuItem("Last Bell/Reviewed Motion and Audio/Apply")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isDirty)
                throw new InvalidOperationException("Save scene changes and leave Play mode first.");
            PresentationArtImporter.MigrateExisting();
            foreach(string file in new HashSet<string>(Slots.Values))
            {
                string path=AudioRoot+file;
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(!importer)throw new FileNotFoundException("Reviewed audio missing",path);
                bool music=file.EndsWith(".ogg",StringComparison.Ordinal);
                importer.forceToMono=false;importer.loadInBackground=music;
                var settings=importer.defaultSampleSettings;
                settings.preloadAudioData=!music;
                settings.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=music?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
                settings.quality=.9f;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            var root=PrefabUtility.LoadPrefabContents(AudioPrefab);
            try{Connect(root.GetComponent<BattleAudio>());PrefabUtility.SaveAsPrefabAsset(root,AudioPrefab);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var obj in scene.GetRootGameObjects())foreach(var presenter in obj.GetComponentsInChildren<MvpPresenter>(true))
                    Connect(presenter.Presentation.Audio);
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_REVIEWED_MOTION_AUDIO_APPLIED");
        }

        static void Connect(BattleAudio audio)
        {
            if(!audio)throw new InvalidDataException("Saved BattleAudio missing");
            var serialized=new SerializedObject(audio);
            foreach(var slot in Slots)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot+slot.Value);
                if(!clip)throw new InvalidDataException("Missing clip "+slot.Value);
                serialized.FindProperty(slot.Key).objectReferenceValue=clip;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(audio);
            if(PrefabUtility.IsPartOfPrefabInstance(audio))PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
        }

        static void ValidateAudio(BattleAudio audio)
        {
            if(!audio||!audio.Music||!audio.Sfx)throw new InvalidDataException("Audio sources missing");
            var serialized=new SerializedObject(audio);
            foreach(var slot in Slots)
            {
                var clip=serialized.FindProperty(slot.Key).objectReferenceValue as AudioClip;
                if(!clip||AssetDatabase.GetAssetPath(clip)!=AudioRoot+slot.Value||clip.length<=0)
                    throw new InvalidDataException("Reviewed audio binding mismatch: "+slot.Key);
                if(slot.Value.EndsWith(".ogg",StringComparison.Ordinal)&&clip.channels!=2)
                    throw new InvalidDataException("Music must preserve stereo: "+slot.Value);
            }
            if(!audio.Arrow||!audio.Spike||!audio.MiniRoar||!audio.BossRoar)
                throw new InvalidDataException("Existing effects must remain linked");
        }

        [MenuItem("Last Bell/Reviewed Motion and Audio/Validate")]
        public static void Validate()
        {
            PresentationArtImporter.Validate();
            ValidateAudio(AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab).GetComponent<BattleAudio>());
            var catalog=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath);
            var data=JsonUtility.FromJson<GameCatalog>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Generated/mvp-catalog.json").text);
            for(int stage=1;stage<=data.mvpLastStage;stage++)
            {
                var art=catalog.Find(data.GetEnemy(data.GetStage(stage).enemyId).artId);
                if(art==null||art.FindClip(ActorMotion.Attack)==null||art.FindClip(ActorMotion.Stun)==null)
                    throw new InvalidDataException("Reviewed stage actor missing: "+stage);
            }
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                int count=0;
                foreach(var obj in scene.GetRootGameObjects())foreach(var p in obj.GetComponentsInChildren<MvpPresenter>(true))
                {ValidateAudio(p.Presentation.Audio);count++;}
                if(count!=1)throw new InvalidDataException("Expected one saved presenter");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            Debug.Log("LAST_BELL_REVIEWED_MOTION_AUDIO_VALID");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
