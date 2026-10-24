using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Adds only v6 bindings to the existing authored assets; safe to apply twice.
    public static class FeedbackV6Migration
    {
        const string AudioPath="Assets/Game/Prefabs/PresentationAudio.prefab";
        const string PickupPath="Assets/Game/Audio/Reviewed-v4/gold-pickup.wav";
        [MenuItem("Last Bell/Feedback v6/Apply")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isDirty)
                throw new InvalidOperationException("Save scene changes and stop Play mode first.");
            AssetDatabase.ImportAsset(PickupPath,ImportAssetOptions.ForceSynchronousImport);
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{Configure(root.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            root=PrefabUtility.LoadPrefabContents(AudioPath);
            try{Configure(root.GetComponent<BattleAudio>());PrefabUtility.SaveAsPrefabAsset(root,AudioPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var r in scene.GetRootGameObjects())foreach(var p in r.GetComponentsInChildren<MvpPresenter>(true))
                {Configure(p.View);Configure(p.Presentation.Audio);}
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_FEEDBACK_V6_APPLIED");
        }
        static void Dirty(UnityEngine.Object obj)
        {EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
        static Text Label(string name,Transform parent,Font font,TextAnchor anchor)
        {
            var child=parent.Find(name);var go=child?child.gameObject:new GameObject(name,typeof(RectTransform),typeof(Text));
            go.layer=5;go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();
            text.font=font;text.fontSize=22;text.color=new Color(.94f,.83f,.62f);text.alignment=anchor;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;Dirty(text);return text;
        }
        static void Configure(MvpView v)
        {
            if(!v.EnemyName)v.EnemyName=Label("EnemyName",v.BattlePanel.transform,v.Stage.font,TextAnchor.MiddleCenter);
            if(!v.HitCombo)v.HitCombo=Label("HitCombo",v.BattlePanel.transform,v.Stage.font,TextAnchor.MiddleRight);
            if(!v.BattleGold)v.BattleGold=Label("BattleGold",v.SafeRoot,v.Stage.font,TextAnchor.MiddleRight);
            if(!v.GoldPickup)
            {
                var go=new GameObject("GoldPickup",typeof(RectTransform),typeof(GoldPickupGraphic));go.layer=5;go.transform.SetParent(v.SafeRoot,false);
                v.GoldPickup=go.GetComponent<GoldPickupGraphic>();var rt=(RectTransform)go.transform;
                rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            }
            v.GoldPickup.Label=v.BattleGold;v.GoldPickup.raycastTarget=false;
            // Effects below the readable counter; modals remain above both.
            v.GoldPickup.transform.SetSiblingIndex(v.BattleGold.transform.GetSiblingIndex());
            v.PausePanel.transform.SetAsLastSibling();v.ModalPanel.transform.SetAsLastSibling();
            var overlay=v.GetComponentInChildren<PresentationOverlay>(true);
            if(overlay&&overlay.ButtonSkip)
            {
                // Keep the story action above its panel, clear of gold/combo/pause.
                var skip=(RectTransform)overlay.ButtonSkip.transform;
                skip.anchorMin=skip.anchorMax=new Vector2(1,0);skip.pivot=new Vector2(.5f,.5f);
                skip.anchoredPosition=new Vector2(-94,178);skip.sizeDelta=new Vector2(132,48);Dirty(skip);
            }
            var layout=v.GetComponentInChildren<FeedbackLayout>(true);if(!layout)layout=v.GetComponent<FeedbackLayout>();
            if(layout)layout.Apply(960,540,false);
            Dirty(v);Dirty(v.GoldPickup);
        }
        static AudioSource Source(BattleAudio audio,string name)
        {
            var child=audio.transform.Find(name);var go=child?child.gameObject:new GameObject(name);
            go.transform.SetParent(audio.transform,false);var s=go.GetComponent<AudioSource>();if(!s)s=go.AddComponent<AudioSource>();
            s.playOnAwake=false;s.loop=false;s.spatialBlend=0;Dirty(s);return s;
        }
        static void Configure(BattleAudio audio)
        {
            if(!audio.BellA)audio.BellA=Source(audio,"PerfectBellA");
            if(!audio.BellB)audio.BellB=Source(audio,"PerfectBellB");
            audio.GoldPickup=AssetDatabase.LoadAssetAtPath<AudioClip>(PickupPath);Dirty(audio);
        }
        [MenuItem("Last Bell/Feedback v6/Validate")]
        public static void Validate()
        {
            var v=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            var a=AssetDatabase.LoadAssetAtPath<GameObject>(AudioPath).GetComponent<BattleAudio>();
            if(!v.EnemyName||!v.BattleGold||!v.HitCombo||!v.GoldPickup||v.GoldPickup.Label!=v.BattleGold||!a.GoldPickup||!a.BellA||!a.BellB)
                throw new InvalidDataException("Missing v6 saved HUD/audio bindings.");
            var data=JsonUtility.FromJson<GameCatalog>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Generated/mvp-catalog.json").text);data.ValidateOrThrow();
            var art=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath);
            for(int stage=1;stage<=data.mvpLastStage;stage++)
            {
                var actor=art.Find(data.GetEnemy(data.GetStage(stage).enemyId).artId);
                if(actor==null||actor.FindClip(ActorMotion.Attack)==null||actor.FindClip(ActorMotion.Stun)==null)
                    throw new InvalidDataException("Missing stage actor clips: "+stage);
            }
            Debug.Log("LAST_BELL_FEEDBACK_V6_VALID");
        }
    }
}
