using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public static class FeedbackV86Migration
    {
        const string AudioFolder="Assets/Game/Audio/Combat-v86/";
        static void Credits(MvpView view)
        {
            var c=view.AudioCredits;
            c.Credits=AssetDatabase.LoadAssetAtPath<TextAsset>(AudioFolder+"UI_CREDITS.txt");
            c.Body.text=c.Credits.text;
            c.SourceUrl="https://opengameart.org/content/4-chiptunes-adventure";
            c.LicenseUrl="https://creativecommons.org/publicdomain/zero/1.0/";
            c.SourceButton.GetComponentInChildren<Text>(true).text="음악 원본";
            c.LicenseButton.GetComponentInChildren<Text>(true).text="CC0 1.0";
            foreach(var obj in new UnityEngine.Object[]{c,c.Body,c.SourceButton.GetComponentInChildren<Text>(true),c.LicenseButton.GetComponentInChildren<Text>(true)})
            {EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
        }
        [MenuItem("Last Bell/Feedback v8.6/Apply")]
        public static void Apply()
        {
            PresentationArtImporter.MigrateExisting();ApplySound();Validate();
        }
        public static void ApplySound()
        {
            ReviewedMotionAudioMigration.ApplyAudio();
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{Credits(root.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var obj in scene.GetRootGameObjects())foreach(var p in obj.GetComponentsInChildren<MvpPresenter>(true))Credits(p.View);
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();
        }
        public static void Validate()
        {
            var data=JsonUtility.FromJson<GameCatalog>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Generated/mvp-catalog.json").text);
            var catalog=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath);
            var ids=new HashSet<string>();var arts=new HashSet<string>();
            for(int stage=1;stage<=data.mvpLastStage;stage++)
            {
                var enemy=data.GetEnemy(data.GetStage(stage).enemyId);
                if(!ids.Add(enemy.id)||!arts.Add(enemy.artId))throw new InvalidDataException("Duplicate stage enemy/design at "+stage);
                var art=catalog.Find(enemy.artId);var idle=art?.FindClip(ActorMotion.Idle);
                if(idle==null||idle.frames.Length<2)throw new InvalidDataException("Animated idle missing at "+stage);
            }
            var manifest=JsonUtility.FromJson<AudioManifest>(File.ReadAllText(AudioFolder+"manifest.json"));
            using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var item in manifest.files)
            {
                string actual=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(AudioFolder+item.file))).Replace("-","").ToLowerInvariant();
                if(actual!=item.sha256)throw new InvalidDataException("v8.6 audio hash mismatch: "+item.file);
            }
            if(File.ReadAllText("Assets/StreamingAssets/AudioLicenses/manifest.json")!=File.ReadAllText(AudioFolder+"manifest.json"))throw new InvalidDataException("Distributed audio manifest is stale");
            Debug.Log("LAST_BELL_V86_VALID: unique stage enemies, animated idle, audio/license hashes");
        }
        [Serializable] sealed class AudioManifest{public AudioFile[] files;}
        [Serializable] sealed class AudioFile{public string file,sha256;}
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
