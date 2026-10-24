using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // The caller owns loading/saving prefab and scene objects. Never rebuilds a screen.
    public static class DragonUiMigration
    {
        // Preserve the existing Down/parry input; the request changes positions only.
        public static string ConfirmedThirdIcon="down";
        public const string AssetRoot="Assets/Game/Art/UI/Dragon-v1/";
        static readonly string[] Icons={"up","left","right","down","sword","shield","pause","coin","gem","heart","star","plus","bag","check","cross"};
        static readonly string[] States={"normal","pressed","disabled"};
        static string importedFor;

        static void PrepareSprites()
        {
            if(importedFor==ConfirmedThirdIcon)return;
            var icons=Icons;
            // Validate every source before changing any importer or loaded UI object.
            foreach(string icon in icons)foreach(string state in States)
                if(!File.Exists(AssetRoot+icon+"-"+state+".png"))throw new FileNotFoundException("Dragon UI image missing",AssetRoot+icon+"-"+state+".png");
            foreach(string icon in icons)foreach(string state in States)
            {
                string path=AssetRoot+icon+"-"+state+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            importedFor=ConfirmedThirdIcon;
        }
        public static void Apply(MvpPresenter presenter)
        {
            if(!presenter||!presenter.View)throw new ArgumentException("A loaded presenter with its saved view is required.");
            Apply(presenter.View);
            if(presenter.Presentation&&presenter.Presentation.Telegraphs)
            {presenter.Presentation.Telegraphs.DragonTheme=true;Dirty(presenter.Presentation.Telegraphs);}
            if(presenter.Effects)foreach(var number in presenter.Effects.Numbers){number.DragonTheme=true;Dirty(number);}
            if(presenter.Presentation&&presenter.Presentation.Overlay)
            {
                var overlay=presenter.Presentation.Overlay;
                overlay.DragonTheme=true;Dirty(overlay);
                if(overlay.StoryText){overlay.StoryText.color=DragonUiPalette.Ink;Dirty(overlay.StoryText);}
                if(overlay.StorySpeaker){overlay.StorySpeaker.color=DragonUiPalette.Gold;Dirty(overlay.StorySpeaker);}
            }
            Validate(presenter);
        }
        public static void Apply(MvpView view)
        {
            if(ConfirmedThirdIcon!="right"&&ConfirmedThirdIcon!="down")
                throw new InvalidOperationException("Confirm the third control semantics before applying Dragon UI. Set ConfirmedThirdIcon to right or down.");
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play mode before applying the saved UI theme.");
            RequireSavedView(view);PrepareSprites();
            bool alreadyThemed=view.Dragon;
            var theme=view.Dragon?view.Dragon:view.GetComponent<DragonUiTheme>()??view.gameObject.AddComponent<DragonUiTheme>();
            view.Dragon=theme;theme.View=view;
            theme.BattleShelf=Panel("DragonCommandShelf",view.BattlePanel.transform);theme.BattleShelf.Shelf=true;theme.BattleShelf.transform.SetAsFirstSibling();
            theme.StagePanel=Panel("DragonStagePanel",view.BattlePanel.transform);theme.StagePanel.transform.SetSiblingIndex(1);
            theme.StageCoin=Symbol("DragonGold",view.BattlePanel.transform,"coin",UiSymbol.Coin);
            theme.WalletGem=Symbol("DragonWalletGem",view.LobbyPanel.transform,"gem",UiSymbol.Gem);
            theme.PauseSymbol=Symbol("DragonPause",view.Pause.transform,"pause",UiSymbol.Pause);
            theme.RelicBag=Symbol("DragonRelicBag",view.OpenRelics.transform,"bag",UiSymbol.Bag);
            for(int i=0;i<view.DodgeSymbols.Length;i++)
            {
                Bind(view.DodgeSymbols[i],i==0?"up":i==1?"left":i==2?ConfirmedThirdIcon:"right");
                view.DodgeSymbols[i].Command=view.Dodge[i];view.Dodge[i].PixelPress=true;
            }
            Bind(view.AttackSymbol,"sword");view.AttackSymbol.Command=view.Attack;view.Attack.PixelPress=true;
            foreach(var command in new[]{view.Dodge[0],view.Dodge[1],view.Dodge[2],view.Attack})
            {
                foreach(var plate in command.GetComponentsInChildren<PlateGraphic>(true))plate.enabled=false;
                foreach(var panel in command.GetComponentsInChildren<ReferencePanelGraphic>(true))panel.enabled=false;
                var hit=command.GetComponent<Image>();if(hit){hit.enabled=true;hit.color=Color.clear;hit.raycastTarget=true;}
            }
            view.Dodge[3].gameObject.SetActive(false);
            foreach(var symbol in view.GetComponentsInChildren<SymbolGraphic>(true))
                if(!symbol.NormalSprite)Bind(symbol,IconName(symbol.Symbol));
            foreach(var icon in view.GetComponentsInChildren<RelicIconGraphic>(true))icon.DragonTheme=true;
            view.PlayerHealthBar.DragonTheme=view.EnemyHealthBar.DragonTheme=true;
            view.Rhythm.DragonTheme=true;view.DodgeResult.DragonTheme=true;
            foreach(var panel in view.GetComponentsInChildren<ReferencePanelGraphic>(true))
            {panel.DragonTheme=true;panel.color=Color.white;panel.SetVerticesDirty();}
            foreach(var button in view.GetComponentsInChildren<Button>(true))StyleButton(button);
            // Receive used to be a transparent overlay above sibling artwork/text.
            // Its opaque Dragon face must render behind the reward card contents.
            foreach(var card in view.Rewards)card.Receive.transform.SetAsFirstSibling();
            theme.PauseSymbol.ButtonFeedback=view.Pause.GetComponent<DragonButtonFeedback>();
            theme.RelicBag.ButtonFeedback=view.OpenRelics.GetComponent<DragonButtonFeedback>();

            // Reuse existing screen backdrops. The coordinator assigns the new inn sprite.
            var backdrop=view.LobbyPanel.transform.Find("ReferenceBackdrop");
            if(backdrop)
            {
                theme.LobbyBackdrop=backdrop.GetComponent<Image>();
                // Do not leave the old blue castle under the new paper UI. The art
                // coordinator can assign its final inn sprite after this helper.
                PrepareBackdrop(theme.LobbyBackdrop,alreadyThemed);
                var shade=backdrop.Find("Shade");if(shade){var image=shade.GetComponent<Image>();if(image)image.color=new Color(1,.95f,.82f,.16f);}
                var lamp=backdrop.Find("LobbyLantern");if(lamp)lamp.gameObject.SetActive(false);
            }
            foreach(var panel in new[]{view.RelicPanel,view.RewardPanel,view.ResultPanel})
            {var image=panel.GetComponent<Image>();if(image){image.color=DragonUiPalette.Paper;image.sprite=null;}}
            foreach(var parent in new[]{view.ModalPanel,view.PausePanel})
            {
                var veil=parent.GetComponent<Image>();if(veil)veil.color=new Color(.15f,.24f,.27f,.35f);
                var scrim=parent.transform.Find("Scrim");if(scrim){var image=scrim.GetComponent<Image>();if(image)image.color=new Color(.15f,.24f,.27f,.35f);}
                foreach(var plate in parent.GetComponentsInChildren<PlateGraphic>(true))
                {
                    if(!plate.enabled)continue;plate.enabled=false;
                    var panel=Panel("DragonPlateFace",plate.transform);panel.raycastTarget=plate.raycastTarget;
                    panel.rectTransform.anchorMin=Vector2.zero;panel.rectTransform.anchorMax=Vector2.one;panel.rectTransform.offsetMin=panel.rectTransform.offsetMax=Vector2.zero;
                }
            }
            foreach(var text in view.GetComponentsInChildren<Text>(true))
            {text.color=DragonUiPalette.Ink;text.text=DragonUiPalette.Copy(text.text);}
            if(view.AudioCredits)
            {
                PaperBackground(view.AudioCredits.Dialog);
                var veil=view.AudioCredits.Panel.GetComponent<Image>();if(veil)veil.color=new Color(.15f,.24f,.27f,.35f);
                view.AudioCredits.Body.color=DragonUiPalette.Ink;
                view.AudioCredits.Heading.color=DragonUiPalette.Ink;
            }
            PaperBackground(view.ModalTitle.transform.parent);
            PaperBackground(view.Resume.transform.parent);
            foreach(var toggle in view.PausePanel.GetComponentsInChildren<Toggle>(true))
            {if(toggle.targetGraphic)toggle.targetGraphic.color=DragonUiPalette.Empty;if(toggle.graphic)toggle.graphic.color=DragonUiPalette.SuccessLight;}
            if(view.Prologue)
            {
                var background=view.Prologue.transform.Find("Backdrop");
                if(background){theme.PrologueBackdrop=background.GetComponent<Image>();PrepareBackdrop(theme.PrologueBackdrop,alreadyThemed);}
            }
            view.Reference.Exchange.GetComponentInChildren<Text>(true).text="보석으로 교환";
            view.Reference.Purchase.GetComponentInChildren<Text>(true).text="강화";
            view.ReplayPrologue.GetComponentInChildren<Text>(true).text="이야기";
            view.ReturnHome.GetComponentInChildren<Text>(true).text="여관으로";
            view.ResultLobby.GetComponentInChildren<Text>(true).text="여관";
            view.GetComponent<FeedbackLayout>().Apply(960,540,false);
            // Persist only through the coordinator. Dirtying these objects is necessary
            // for scene overrides and does not write a prefab or scene here.
            foreach(var component in view.GetComponentsInChildren<Component>(true))Dirty(component);
            Validate(view);
        }
        static void RequireSavedView(MvpView view)
        {
            if(!view||!view.Reference||!view.GetComponent<FeedbackLayout>()||!view.PlayerHealthBar||!view.EnemyHealthBar||!view.Rhythm||!view.DodgeResult||view.Dodge.Length!=4||view.DodgeSymbols.Length!=4)
                throw new InvalidDataException("The existing v8.6 saved UI is required; this helper will not rebuild it.");
        }
        static string IconName(UiSymbol symbol)
        {
            switch(symbol)
            {
                case UiSymbol.Up:return "up";case UiSymbol.Back:return "left";case UiSymbol.Down:return "down";case UiSymbol.Forward:return "right";
                case UiSymbol.Sword:return "sword";case UiSymbol.Star:return "star";case UiSymbol.Pause:return "pause";case UiSymbol.Coin:return "coin";
                case UiSymbol.Gem:return "gem";case UiSymbol.Heart:return "heart";case UiSymbol.Plus:return "plus";case UiSymbol.Bag:return "bag";
                case UiSymbol.Check:return "check";case UiSymbol.Cross:return "cross";default:return "shield";
            }
        }
        static void Bind(SymbolGraphic symbol,string icon)
        {
            symbol.NormalSprite=AssetDatabase.LoadAssetAtPath<Sprite>(AssetRoot+icon+"-normal.png");
            symbol.PressedSprite=AssetDatabase.LoadAssetAtPath<Sprite>(AssetRoot+icon+"-pressed.png");
            symbol.DisabledSprite=AssetDatabase.LoadAssetAtPath<Sprite>(AssetRoot+icon+"-disabled.png");
            symbol.color=Color.white;symbol.raycastTarget=false;symbol.SetAllDirty();
        }
        static void PrepareBackdrop(Image image,bool alreadyThemed)
        {
            if(!image)return;
            bool dragonArt=image.sprite&&AssetDatabase.GetAssetPath(image.sprite).Contains("/Dragon-v1/");
            if(!alreadyThemed&&!dragonArt)image.sprite=null;
            image.color=image.sprite?Color.white:DragonUiPalette.Paper;
        }
        static RectTransform Child(string name,Transform parent)
        {
            var existing=parent.Find(name);if(existing)return (RectTransform)existing;
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        static ReferencePanelGraphic Panel(string name,Transform parent)
        {
            var rect=Child(name,parent);var panel=rect.GetComponent<ReferencePanelGraphic>()??rect.gameObject.AddComponent<ReferencePanelGraphic>();
            panel.DragonTheme=true;panel.color=Color.white;panel.raycastTarget=false;return panel;
        }
        static void PaperBackground(Transform parent)
        {
            var panel=Panel("DragonPaperFace",parent);panel.transform.SetAsFirstSibling();
            var rect=panel.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var old=parent.GetComponent<Image>();if(old)old.color=Color.clear;
        }
        static SymbolGraphic Symbol(string name,Transform parent,string icon,UiSymbol type)
        {
            var rect=Child(name,parent);var symbol=rect.GetComponent<SymbolGraphic>()??rect.gameObject.AddComponent<SymbolGraphic>();
            symbol.Symbol=type;Bind(symbol,icon);return symbol;
        }
        static void StyleButton(Button button)
        {
            var panels=button.GetComponentsInChildren<ReferencePanelGraphic>(true);
            if(panels.Length==0)
            {
                var panel=Panel("DragonButtonFace",button.transform);panel.transform.SetAsFirstSibling();
                var rect=panel.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                foreach(var old in button.GetComponentsInChildren<PlateGraphic>(true))old.enabled=false;
                panels=new[]{panel};
            }
            foreach(var panel in panels){panel.DragonTheme=true;panel.Control=button;panel.enabled=true;panel.raycastTarget=true;panel.color=Color.white;}
            var image=button.GetComponent<Image>();if(image){image.color=Color.clear;image.raycastTarget=true;}
            button.targetGraphic=panels[0];button.transition=Selectable.Transition.None;
            var feedback=button.GetComponent<DragonButtonFeedback>()??button.gameObject.AddComponent<DragonButtonFeedback>();
            feedback.Button=button;feedback.Panels=panels;feedback.Labels=button.GetComponentsInChildren<Text>(true);
        }
        static void Dirty(UnityEngine.Object value)
        {if(!value)return;EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
        public static void Validate(MvpPresenter presenter)
        {
            Validate(presenter.View);
            if(presenter.Presentation&&presenter.Presentation.Telegraphs&&!presenter.Presentation.Telegraphs.DragonTheme)throw new InvalidDataException("Dragon telegraph skin is not connected.");
        }
        public static void Validate(MvpView view)
        {
            RequireSavedView(view);var theme=view.Dragon;
            if(!theme||!theme.BattleShelf||!theme.StagePanel||!theme.StageCoin||!theme.WalletGem||!theme.PauseSymbol)throw new InvalidDataException("Missing saved Dragon UI decorations.");
            foreach(var symbol in view.GetComponentsInChildren<SymbolGraphic>(true))
                if(!symbol.NormalSprite||!symbol.PressedSprite||!symbol.DisabledSprite||symbol.NormalSprite.texture.filterMode!=FilterMode.Point)throw new InvalidDataException("Missing pixel icon states: "+symbol.name);
            if(!view.PlayerHealthBar.DragonTheme||!view.EnemyHealthBar.DragonTheme||!view.Rhythm.DragonTheme||view.Dodge[3].gameObject.activeSelf)
                throw new InvalidDataException("Dragon HUD or existing three-direction contract missing.");
            foreach(var card in view.Rewards)
                if(card.Receive.transform.GetSiblingIndex()>=card.Icon.transform.GetSiblingIndex()||card.Receive.transform.GetSiblingIndex()>=card.Title.transform.GetSiblingIndex())
                    throw new InvalidDataException("Reward button face must render behind its sibling icon and title.");
            if(HealthBarGraphic.FilledCells(0,38)!=0||HealthBarGraphic.FilledCells(.00001f,38)!=1||HealthBarGraphic.FilledCells(1,38)!=38)
                throw new InvalidDataException("Positive HP must retain one visible cell.");
            Debug.Log("LAST_BELL_DRAGON_UI_VALID: saved pixel states, paper panels, three reference controls and positive HP visibility");
        }
    }
}
