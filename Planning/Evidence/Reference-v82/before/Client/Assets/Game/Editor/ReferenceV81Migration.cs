using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace LastBell.Editor
{
    public static class ReferenceV81Migration
    {
        const string FontPath="Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf";
        static Font font;
        static readonly Color Ink=new Color32(18,29,36,246),Cream=new Color32(239,218,176,255);
        [MenuItem("Last Bell/Reference v8.1/Apply")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isDirty)throw new InvalidOperationException("Save scene changes first.");
            AssetDatabase.ImportAsset(FontPath,ImportAssetOptions.ForceSynchronousImport);
            font=AssetDatabase.LoadAssetAtPath<Font>(FontPath);if(!font)throw new InvalidOperationException("Missing NeoDunggeunmo Pro.");
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
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_REFERENCE_V81_APPLIED");
        }
        static RectTransform Rect(string name,Transform parent)
        {
            var child=parent.Find(name);if(child)return (RectTransform)child;
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        static T Get<T>(RectTransform r)where T:Component=>r.GetComponent<T>()??r.gameObject.AddComponent<T>();
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static Text Label(string name,Transform parent,string content,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=Get<Text>(Rect(name,parent));t.font=font;t.fontSize=24;t.fontStyle=FontStyle.Normal;t.text=content;t.color=Cream;
            t.raycastTarget=false;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.resizeTextForBestFit=false;return t;
        }
        static Button Button(string name,Transform parent,string content)
        {
            var r=Rect(name,parent);var plate=Get<ReferencePanelGraphic>(r);plate.color=Ink;plate.raycastTarget=true;
            var b=Get<Button>(r);b.targetGraphic=plate;var t=Label("Caption",r,content,TextAnchor.MiddleCenter);Stretch(t.rectTransform);return b;
        }
        static void Ornament(Transform parent)
        {
            var r=Rect("ReferenceFrame",parent);Stretch(r);r.SetAsFirstSibling();var g=Get<ReferencePanelGraphic>(r);g.color=Ink;g.raycastTarget=false;
            var im=parent.GetComponent<Image>();if(im)im.color=Color.clear;
        }
        static void Configure(MvpView v)
        {
            v.Reference=v.GetComponent<ReferenceLobbyView>()??v.gameObject.AddComponent<ReferenceLobbyView>();var r=v.Reference;
            var lobby=v.LobbyPanel.transform;
            var bg=Rect("ReferenceBackdrop",lobby);Stretch(bg);bg.SetAsFirstSibling();var image=Get<Image>(bg);
            image.sprite=AssetDatabase.LoadAssetAtPath<ArtCatalog>(PresentationArtImporter.CatalogPath).gateBackground;
            image.color=new Color(.35f,.40f,.43f,1);image.raycastTarget=false;
            var shade=Rect("Shade",bg);Stretch(shade);Get<Image>(shade).color=new Color(.025f,.04f,.055f,.38f);shade.GetComponent<Image>().raycastTarget=false;
            var lamp=Rect("LobbyLantern",bg);FeedbackLayout.Box(lamp,28,140,176,176);var glow=Get<StoryDecorGraphic>(lamp);glow.Lamp=true;glow.raycastTarget=false;
            r.Selection=Rect("ReferenceSelection",lobby).gameObject;Stretch((RectTransform)r.Selection.transform);
            r.Upgrade=Rect("ReferenceUpgrade",lobby).gameObject;Stretch((RectTransform)r.Upgrade.transform);
            v.Characters[0].transform.parent.SetParent(r.Selection.transform,false);
            v.LobbyPortrait.transform.parent.SetParent(r.Selection.transform,false);
            v.Stats[0].Upgrade.transform.parent.parent.SetParent(r.Selection.transform,false);
            Ornament(v.Characters[0].transform.parent);Ornament(v.LobbyPortrait.transform.parent);
            foreach(var stat in v.Stats)Ornament(stat.Title.transform.parent);
            r.Heading=Label("ReferenceHeading",lobby,"종지기 선택");r.Summary=Label("Summary",r.Selection.transform,"");
            r.Back=Button("SelectionBack",lobby,"뒤로");
            var detail=Rect("UpgradeDetail",r.Upgrade.transform);Ornament(detail);
            r.UpgradeTitle=Label("StatTitle",detail,"공격력");r.UpgradeLevel=Label("Level",detail,"Lv.0 / 50");
            r.Before=Label("Before",detail,"");r.After=Label("After",detail,"");r.Price=Label("Price",detail,"");r.Note=Label("Note",detail,"");
            r.UpgradePortrait=Get<Image>(Rect("Portrait",detail));r.UpgradePortrait.preserveAspect=true;r.UpgradePortrait.raycastTarget=false;
            r.OneLevel=Button("OneLevel",detail,"1Lv.");r.Maximum=Button("Maximum",detail,"MAX");
            r.Purchase=Button("Purchase",r.Upgrade.transform,"강화");r.Exchange=Button("Exchange",r.Upgrade.transform,"파편으로 교환");
            r.StatButtons=new Button[4];r.StatLabels=new Text[4];
            for(int i=0;i<4;i++){r.StatButtons[i]=Button("SelectStat"+i,r.Upgrade.transform,"");r.StatLabels[i]=r.StatButtons[i].GetComponentInChildren<Text>();}
            r.PlayerHealth=Label("PlayerHealthValue",v.BattlePanel.transform,"",TextAnchor.MiddleCenter);
            r.EnemyHealth=Label("EnemyHealthValue",v.BattlePanel.transform,"",TextAnchor.MiddleCenter);
            r.PlayerName=Label("PlayerName",v.BattlePanel.transform,"온",TextAnchor.MiddleCenter);
            r.BattleFragments=Label("EarnedFragments",v.BattlePanel.transform,"");
            r.SettlementContinue=Button("SettlementContinue",v.ResultPanel.transform,"계속");
            foreach(var command in new[]{v.Dodge[0],v.Dodge[1],v.Dodge[2],v.Attack})
            {
                foreach(var plate in command.GetComponentsInChildren<PlateGraphic>(true))plate.enabled=false;
                foreach(var im in command.GetComponentsInChildren<Image>(true))if(im.gameObject!=command.gameObject)im.enabled=false;
                var old=command.GetComponent<Image>();if(old){old.enabled=true;old.color=Color.clear;old.raycastTarget=true;}
                var diamond=Rect("ReferenceDiamond",command.transform);Stretch(diamond);diamond.SetAsFirstSibling();var g=Get<ReferencePanelGraphic>(diamond);g.Diamond=true;g.color=new Color32(25,40,47,238);g.raycastTarget=false;
            }
            // Archive the fourth binding for save/editor compatibility; it never receives input.
            v.Dodge[3].gameObject.SetActive(false);
            if(v.AudioCredits)
            {
                v.AudioCredits.Heading.text="음원 · 글꼴 출처";
                v.AudioCredits.Body.text=v.AudioCredits.Credits.text+"\n\nNeoDunggeunmo Pro · Eunbin Jeong (Dalgona.)\nSIL Open Font License 1.1\nhttps://neodgm.dalgona.dev\n배포 파일: StreamingAssets/FontLicenses";
            }
            foreach(var t in v.GetComponentsInChildren<Text>(true)){t.font=font;t.fontSize=24;t.fontStyle=FontStyle.Normal;t.resizeTextForBestFit=false;}
            foreach(var b in v.GetComponentsInChildren<Button>(true))
            {
                var colors=b.colors;colors.normalColor=new Color32(28,43,51,255);colors.highlightedColor=new Color32(58,69,67,255);
                colors.selectedColor=colors.normalColor;colors.pressedColor=new Color32(89,74,48,255);colors.disabledColor=new Color32(20,28,34,170);colors.colorMultiplier=1;b.colors=colors;
            }
            v.Prologue.Pause.gameObject.SetActive(false);v.Prologue.ReadingMode.gameObject.SetActive(false);v.Prologue.Next.gameObject.SetActive(false);
            v.GetComponent<FeedbackLayout>().Apply(960,540,false);
            v.RewardPanel.transform.SetAsLastSibling();v.ResultPanel.transform.SetAsLastSibling();v.PausePanel.transform.SetAsLastSibling();v.ModalPanel.transform.SetAsLastSibling();
            foreach(var graphic in v.GetComponentsInChildren<Graphic>(true))if(!graphic.GetComponent<CanvasRenderer>())graphic.gameObject.AddComponent<CanvasRenderer>();
            foreach(var c in v.GetComponentsInChildren<Component>(true)){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        }
        public static void Validate()
        {
            var v=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            if(!v.Reference||!v.Reference.Purchase||!v.Reference.SettlementContinue||v.Dodge[3].gameObject.activeSelf)throw new InvalidOperationException("Missing v8.1 saved UI.");
            foreach(var t in v.GetComponentsInChildren<Text>(true))if(!t.font||t.font.name!="NeoDunggeunmoPro-Regular"||t.fontSize!=24||t.resizeTextForBestFit)throw new InvalidOperationException("Inconsistent common font: "+t.name);
            if(!File.Exists("Assets/StreamingAssets/FontLicenses/NeoDunggeunmoPro-OFL.txt"))throw new InvalidOperationException("Missing distributed font license.");
            Debug.Log("LAST_BELL_REFERENCE_V81_VALID");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
