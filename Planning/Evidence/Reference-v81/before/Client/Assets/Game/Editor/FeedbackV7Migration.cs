using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public static class FeedbackV7Migration
    {
        [MenuItem("Last Bell/Feedback v7/Apply")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isDirty)
                throw new InvalidOperationException("Stop Play mode and save scene changes first.");
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{Configure(root.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var r in scene.GetRootGameObjects())foreach(var p in r.GetComponentsInChildren<MvpPresenter>(true))Configure(p.View);
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_FEEDBACK_V7_APPLIED");
        }
        static RectTransform Rect(string name,Transform parent)
        {
            var child=parent.Find(name);var go=child?child.gameObject:new GameObject(name,typeof(RectTransform));
            go.layer=5;go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static T Component<T>(RectTransform r) where T:Component{return r.GetComponent<T>()??r.gameObject.AddComponent<T>();}
        static Text Label(string name,Transform parent,Font font,string content)
        {
            var t=Component<Text>(Rect(name,parent));t.font=font;t.text=content;t.color=new Color32(243,220,173,255);
            t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        static void Configure(MvpView v)
        {
            var font=v.Stage.font;var overlay=v.RewardPanel.GetComponent<Image>();overlay.color=new Color(0,0,0,.22f);
            v.RewardDialog=Rect("CompactDialog",v.RewardPanel.transform);
            Component<Image>(v.RewardDialog).color=new Color32(17,31,43,250);
            var oldTitle=v.RewardPanel.transform.Find("Title");if(oldTitle)oldTitle.gameObject.SetActive(false);
            v.RewardNote.transform.SetParent(v.RewardDialog,false);
            foreach(var c in v.Rewards)
            {
                var card=(RectTransform)c.Receive.transform.parent;card.SetParent(v.RewardDialog,false);
                card.GetComponent<Image>().color=new Color32(35,53,65,255);
                foreach(var t in new[]{c.Level,c.Before,c.After,c.Equipment})t.gameObject.SetActive(false);
                foreach(var t in c.Receive.GetComponentsInChildren<Text>(true))t.gameObject.SetActive(false);
                c.Receive.targetGraphic.color=Color.clear;
                if(c.Receive.targetGraphic is PlateGraphic)c.Receive.targetGraphic.enabled=false;
                var hit=Component<Image>(Rect("HitArea",c.Receive.transform));Stretch(hit.rectTransform);hit.color=Color.clear;hit.raycastTarget=true;c.Receive.targetGraphic=hit;
                var selection=Rect("Selection",card);c.Selection=Component<Image>(selection);c.Selection.color=new Color(1,.72f,.25f,.25f);c.Selection.raycastTarget=false;
                c.Icon=Component<RelicIconGraphic>(Rect("RelicIcon",card));c.Icon.raycastTarget=false;
                c.Title.raycastTarget=false;c.Title.alignment=TextAnchor.MiddleCenter;
                c.Receive.transform.SetAsLastSibling();
            }
            v.RewardDescription=Label("Description",v.RewardDialog,font,"");
            var confirm=Rect("Confirm",v.RewardDialog);var plate=Component<Image>(confirm);plate.color=new Color32(167,120,49,255);
            v.RewardConfirm=Component<Button>(confirm);v.RewardConfirm.targetGraphic=plate;
            var caption=Label("Label",confirm,font,"선택");Stretch(caption.rectTransform);
            var p=v.Prologue;var background=Rect("Backdrop",p.transform);background.SetAsFirstSibling();Stretch(background);
            var backdrop=Component<Image>(background);backdrop.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Source/gate-background-v1.png");backdrop.color=new Color(.55f,.6f,.65f,1);backdrop.raycastTarget=false;
            // The existing game panorama is a saved art asset, shared with the opening scene.
            if(!backdrop.sprite){var art=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath);backdrop.sprite=art.gateBackground;}
            p.Scroll=Rect("Parchment",p.transform);p.Scroll.SetSiblingIndex(1);Component<StoryDecorGraphic>(p.Scroll).raycastTarget=false;
            var oldLamp=p.transform.Find("Lantern");if(oldLamp)oldLamp.gameObject.SetActive(false);
            p.Lantern=Rect("UpperLeftLamp",p.transform);p.LampGlow=Component<StoryDecorGraphic>(p.Lantern);p.LampGlow.Lamp=true;p.LampGlow.raycastTarget=false;
            p.Title.color=new Color32(92,62,34,255);
            foreach(var t in p.Paragraphs){t.color=new Color32(62,43,29,255);t.horizontalOverflow=HorizontalWrapMode.Wrap;t.lineSpacing=1.3f;}
            var layout=v.GetComponent<FeedbackLayout>();if(layout)layout.Apply(960,540,false);
            // Rewards must stay readable while the last boss's gold flies toward the HUD.
            v.RewardPanel.transform.SetAsLastSibling();v.PausePanel.transform.SetAsLastSibling();v.ModalPanel.transform.SetAsLastSibling();
            // Store actual renderers as part of the authored UI, including custom Graphic subclasses.
            foreach(var g in v.GetComponentsInChildren<Graphic>(true))if(!g.GetComponent<CanvasRenderer>())g.gameObject.AddComponent<CanvasRenderer>();
            foreach(var component in v.GetComponentsInChildren<Component>(true))
            {
                EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
        public static void Validate()
        {
            var v=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            if(!v.RewardDialog||!v.RewardConfirm||!v.RewardDescription||!v.Prologue.Scroll||!v.Prologue.LampGlow)throw new InvalidOperationException("Missing saved v7 UI bindings.");
            foreach(var c in v.Rewards)if(!c.Icon||!c.Selection)throw new InvalidOperationException("Missing relic image/selection.");
            foreach(var g in v.GetComponentsInChildren<Graphic>(true))if(!g.GetComponent<CanvasRenderer>())throw new InvalidOperationException("Missing CanvasRenderer: "+g.name);
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
