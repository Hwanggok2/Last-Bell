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
        static void StyleButton(Button b,bool underline)
        {
            foreach(var old in b.GetComponentsInChildren<PlateGraphic>(true))old.enabled=false;
            foreach(var old in b.GetComponentsInChildren<Image>(true))old.enabled=false;
            var plate=b.GetComponent<ReferencePanelGraphic>();
            if(!plate){var face=Rect("ReferenceButtonStyle",b.transform);Stretch(face);face.SetAsFirstSibling();plate=Get<ReferencePanelGraphic>(face);}
            plate.Underline=underline;plate.color=Ink;plate.raycastTarget=true;b.targetGraphic=plate;
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
            r.SelectionStats=Label("SelectionStats",r.Selection.transform,"");
            r.RelicCount=Label("RelicCount",lobby,"",TextAnchor.MiddleCenter);
            r.StartStage=Label("StartStage",v.StartRun.transform,"",TextAnchor.MiddleCenter);
            r.Back=Button("SelectionBack",lobby,"뒤로");
            var detail=Rect("UpgradeDetail",r.Upgrade.transform);Ornament(detail);
            r.UpgradeTitle=Label("StatTitle",detail,"공격력");r.UpgradeLevel=Label("Level",detail,"Lv.0 / 50");
            r.Before=Label("Before",detail,"");r.After=Label("After",detail,"");r.Price=Label("Price",detail,"");r.Note=Label("Note",detail,"");
            r.UpgradeWallet=Label("UpgradeWallet",detail,"",TextAnchor.MiddleRight);
            r.UpgradePortrait=Get<Image>(Rect("Portrait",detail));r.UpgradePortrait.preserveAspect=true;r.UpgradePortrait.raycastTarget=false;
            r.OneLevel=Button("OneLevel",detail,"1Lv.");r.Maximum=Button("Maximum",detail,"MAX");
            r.Purchase=Button("Purchase",r.Upgrade.transform,"강화");r.Exchange=Button("Exchange",r.Upgrade.transform,"파편으로 교환");
            var oldButtons=r.StatButtons;var statMenu=Rect("StatMenu",r.Upgrade.transform);Ornament(statMenu);
            r.StatButtons=new Button[4];r.StatLabels=new Text[4];r.StatRatings=new Text[4];
            for(int i=0;i<4;i++)
            {
                r.StatButtons[i]=oldButtons!=null&&oldButtons.Length>i&&oldButtons[i]?oldButtons[i]:Button("SelectStat"+i,statMenu,"");
                r.StatButtons[i].transform.SetParent(statMenu,false);r.StatLabels[i]=r.StatButtons[i].transform.Find("Caption").GetComponent<Text>();
                r.StatRatings[i]=Label("Rating",r.StatButtons[i].transform,"",TextAnchor.MiddleRight);
            }
            r.RelicHeading=Label("ReferenceRelicHeading",v.RelicPanel.transform,"유물 목록",TextAnchor.MiddleCenter);
            var relicTitle=v.RelicPanel.transform.Find("Title");if(relicTitle)relicTitle.gameObject.SetActive(false);
            v.SlotPurchase.transform.SetParent(v.RelicPanel.transform,false);v.RelicEquip.transform.SetParent(v.RelicPanel.transform,false);
            r.CollectionIcon=Get<RelicIconGraphic>(Rect("ReferenceRelicIcon",v.RelicName.transform.parent));r.CollectionIcon.raycastTarget=false;
            Ornament(v.RelicName.transform.parent);Ornament(v.EquippedSlots.transform.parent);
            foreach(var b in v.Characters)StyleButton(b,true);
            foreach(var b in r.StatButtons)StyleButton(b,true);
            foreach(var b in v.RelicItems)StyleButton(b,true);
            foreach(var b in new[]{r.Back,v.ReplayPrologue,v.OpenRelics,r.Purchase,v.Pause,v.RelicBack,v.RewardConfirm,v.ResultRetry,v.ResultLobby,v.SlotPurchase,v.RelicEquip,v.Prologue.Skip})StyleButton(b,true);
            foreach(var b in new[]{v.StartRun,r.OneLevel,r.Maximum,r.Exchange,v.AudioCredits.OpenButton})StyleButton(b,false);
            r.Back.GetComponentInChildren<Text>().text="뒤로가기";v.ReplayPrologue.GetComponentInChildren<Text>().text="뒤로가기";
            r.Purchase.GetComponentInChildren<Text>().text="업그레이드";v.AudioCredits.OpenButton.GetComponentInChildren<Text>().text="출처 보기";
            v.RewardConfirm.GetComponentInChildren<Text>().text="고르기";v.RelicBack.GetComponentInChildren<Text>().text="뒤로가기";
            Ornament(v.RewardDialog);
            v.RewardGold=Button("RewardGold",v.RewardDialog,"대신 골드 받기");StyleButton(v.RewardGold,true);
            v.StunReady=Label("StunReady",v.BattlePanel.transform,"공격하면 기절",TextAnchor.MiddleCenter);
            v.StunReady.gameObject.SetActive(false);
            v.RelicEquip.gameObject.SetActive(false);
            r.PlayerHealth=Label("PlayerHealthValue",v.BattlePanel.transform,"",TextAnchor.MiddleCenter);
            r.EnemyHealth=Label("EnemyHealthValue",v.BattlePanel.transform,"",TextAnchor.MiddleCenter);
            r.PlayerName=Label("PlayerName",v.BattlePanel.transform,"온",TextAnchor.MiddleCenter);
            r.BattleFragments=Label("EarnedFragments",v.BattlePanel.transform,"");
            v.PlayerHealthBar=HealthBar(v.PlayerHpRoot,false);
            v.EnemyHealthBar=HealthBar(v.EnemyHpRoot,true);
            v.GoldPickup.Reward=Label("DefeatedGold",v.BattlePanel.transform,"",TextAnchor.MiddleCenter);
            v.GoldPickup.Reward.gameObject.SetActive(false);
            v.HitCombo.gameObject.SetActive(false);
            r.SettlementContinue=Button("SettlementContinue",v.ResultPanel.transform,"계속");
            StyleButton(r.SettlementContinue,true);
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
                var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.82f);
                colors.selectedColor=colors.normalColor;colors.pressedColor=new Color(.75f,.65f,.48f);colors.disabledColor=new Color(.4f,.4f,.4f,.7f);colors.colorMultiplier=1;b.colors=colors;
            }
            v.Prologue.Pause.gameObject.SetActive(false);v.Prologue.ReadingMode.gameObject.SetActive(false);v.Prologue.Next.gameObject.SetActive(false);
            v.GetComponent<FeedbackLayout>().Apply(960,540,false);
            v.RewardPanel.transform.SetAsLastSibling();v.ResultPanel.transform.SetAsLastSibling();v.PausePanel.transform.SetAsLastSibling();v.ModalPanel.transform.SetAsLastSibling();
            foreach(var graphic in v.GetComponentsInChildren<Graphic>(true))if(!graphic.GetComponent<CanvasRenderer>())graphic.gameObject.AddComponent<CanvasRenderer>();
            foreach(var c in v.GetComponentsInChildren<Component>(true)){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        }
        static HealthBarGraphic HealthBar(RectTransform root,bool enemy)
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true))image.enabled=false;
            foreach(var plate in root.GetComponentsInChildren<PlateGraphic>(true))plate.enabled=false;
            var face=Rect("LanternHealthBar",root);Stretch(face);
            var bar=Get<HealthBarGraphic>(face);bar.Enemy=enemy;bar.raycastTarget=false;bar.SetValue(1);return bar;
        }
        public static void Validate()
        {
            var v=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            if(!v.Reference||!v.Reference.Purchase||!v.Reference.SettlementContinue||v.Dodge[3].gameObject.activeSelf)throw new InvalidOperationException("Missing v8.1 saved UI.");
            foreach(var t in v.GetComponentsInChildren<Text>(true))if(!t.font||t.font.name!="NeoDunggeunmoPro-Regular"||t.fontSize<14||t.resizeTextForBestFit)throw new InvalidOperationException("Invalid reference typography: "+t.name);
            if(!v.Reference.SelectionStats||!v.Reference.UpgradeWallet||v.Reference.Heading.fontSize!=39)throw new InvalidOperationException("Missing measured reference layout.");
            if(!v.PlayerHealthBar||!v.EnemyHealthBar||!v.GoldPickup.Reward||v.HitCombo.gameObject.activeSelf)throw new InvalidOperationException("Missing v8.3 health/gold HUD.");
            if(!File.Exists("Assets/StreamingAssets/FontLicenses/NeoDunggeunmoPro-OFL.txt"))throw new InvalidOperationException("Missing distributed font license.");
            if(!v.StunReady||!v.RewardGold||v.RelicEquip.gameObject.activeSelf)throw new InvalidOperationException("Missing v8.4 ready/storage UI.");
            Debug.Log("LAST_BELL_REFERENCE_V81_VALID");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
