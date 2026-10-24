using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBell.Editor
{
    public static class CombatV59Migration
    {
        [MenuItem("Last Bell/Combat/Apply v5.9 rhythm and parry")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isLoaded&&loaded.isDirty)
                throw new InvalidOperationException("Save scene edits and stop Play mode before migration.");
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{Configure(root.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var r in scene.GetRootGameObjects())foreach(var p in r.GetComponentsInChildren<MvpPresenter>(true))
                {
                    Configure(p.View);
                    if(p.Presentation.Audio){p.Presentation.Audio.Windup=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Presentation/enemy-beat-v59.wav");EditorUtility.SetDirty(p.Presentation.Audio);if(PrefabUtility.IsPartOfPrefabInstance(p.Presentation.Audio))PrefabUtility.RecordPrefabInstancePropertyModifications(p.Presentation.Audio);}
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_COMBAT_V59_APPLIED");
        }
        static void Configure(MvpView view)
        {
            var existing=view.transform.Find("CombatRhythm");
            if(!existing){var go=new GameObject("CombatRhythm",typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatRhythmGraphic));go.layer=5;go.transform.SetParent(view.transform,false);existing=go.transform;}
            var rect=(RectTransform)existing;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            view.Rhythm=existing.GetComponent<CombatRhythmGraphic>();view.Rhythm.raycastTarget=false;existing.SetSiblingIndex(1);
            view.Dodge[2].gameObject.SetActive(true);view.GetComponent<FeedbackLayout>().Apply(960,540,false);
            EditorUtility.SetDirty(view);EditorUtility.SetDirty(view.Rhythm);
            if(PrefabUtility.IsPartOfPrefabInstance(view)){PrefabUtility.RecordPrefabInstancePropertyModifications(view);PrefabUtility.RecordPrefabInstancePropertyModifications(view.Rhythm);}
        }
        public static void Validate()
        {
            var view=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            if(!view.Rhythm||view.Rhythm.raycastTarget||view.Rhythm.transform.parent!=view.transform||!view.Dodge[2].gameObject.activeSelf)
                throw new InvalidOperationException("Missing saved rhythm overlay or Down parry control.");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
