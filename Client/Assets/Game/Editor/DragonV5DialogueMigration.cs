using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public static class DragonV5DialogueMigration
    {
        public const string PortraitRoot="Assets/Game/Art/Dialogue/Dragon-v5/";
        static readonly string[] Ids={"on","dam","rin","tori","rina","barun","doto","gib","unknown","momo"};
        public static void Apply(MvpView view)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before the saved dialogue migration.");
            var overlay=view.GetComponentInChildren<PresentationOverlay>(true);
            if(!overlay||!overlay.StoryPortrait||!overlay.ButtonNext||!overlay.ButtonSkip)throw new InvalidOperationException("Existing saved dialogue is required.");
            foreach(string id in Ids)if(!File.Exists(PortraitRoot+id+".png"))throw new FileNotFoundException("24px portrait missing",id);
            overlay.Portraits=Ids.Select(id=>new DialoguePortraitEntry{Id=id,Sprite=Import(id)}).ToArray();
            overlay.StoryV5=overlay.DragonTheme=true;overlay.StoryPortrait.preserveAspect=true;overlay.StoryPortrait.raycastTarget=false;
            overlay.ButtonJoin=CloneButton(overlay.ButtonNext,overlay.transform,"CompanionJoin","함께 가기");
            overlay.ButtonLater=CloneButton(overlay.ButtonNext,overlay.transform,"CompanionLater","여관에서");
            overlay.LayoutV5();overlay.Hide();
            view.ComboSkip=CloneButton(overlay.ButtonSkip,view.BattlePanel.transform,"ComboPracticeSkip","건너뛰기");
            var skip=(RectTransform)view.ComboSkip.transform;
            skip.anchorMin=skip.anchorMax=new Vector2(1,1);skip.pivot=new Vector2(1,1);skip.anchoredPosition=new Vector2(-24,-24);skip.sizeDelta=new Vector2(152,96);
            view.ComboSkip.gameObject.SetActive(false);
            // Help lives on the pause scrim, outside the existing compact menu card.
            view.ComboReplay=CloneButton(view.Resume,view.PausePanel.transform,"ComboPracticeHelp","연속 공격 연습");
            var help=(RectTransform)view.ComboReplay.transform;
            help.anchorMin=help.anchorMax=help.pivot=new Vector2(.5f,0);help.anchoredPosition=new Vector2(0,24);help.sizeDelta=new Vector2(320,96);
            view.ComboReplay.gameObject.SetActive(false);
            foreach(var component in overlay.GetComponentsInChildren<Component>(true))Dirty(component);
            foreach(var button in new[]{view.ComboSkip,view.ComboReplay})foreach(var component in button.GetComponentsInChildren<Component>(true))Dirty(component);
            Dirty(view);Validate(overlay);
        }
        static Sprite Import(string id)
        {
            string path=PortraitRoot+id+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;
            i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.alphaIsTransparency=true;
            i.wrapMode=TextureWrapMode.Clamp;i.npotScale=TextureImporterNPOTScale.None;i.spritePixelsPerUnit=24;i.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(!sprite||sprite.rect.width!=24||sprite.rect.height!=24)throw new InvalidDataException("Expected native 24px portrait: "+id);
            return sprite;
        }
        static Button CloneButton(Button source,Transform parent,string name,string label)
        {
            var node=parent.Find(name);
            var button=node?node.GetComponent<Button>():UnityEngine.Object.Instantiate(source,parent,false);
            button.name=name;button.onClick=new Button.ButtonClickedEvent();
            var text=button.GetComponentInChildren<Text>(true);text.text=label;text.resizeTextForBestFit=false;text.fontSize=28;
            foreach(var panel in button.GetComponentsInChildren<ReferencePanelGraphic>(true))panel.Control=button;
            return button;
        }
        public static void Validate(PresentationOverlay overlay)
        {
            if(!overlay||!overlay.StoryV5||!overlay.StoryPortrait||!overlay.ButtonJoin||!overlay.ButtonLater)throw new InvalidDataException("Saved v5 dialogue bindings missing.");
            foreach(string id in Ids)
            {
                var portrait=Array.Find(overlay.Portraits,x=>x.Id==id)?.Sprite;
                if(!portrait||portrait.rect.width!=24||portrait.rect.height!=24||portrait.texture.filterMode!=FilterMode.Point)throw new InvalidDataException("Invalid v5 portrait: "+id);
            }
        }
        static void Dirty(UnityEngine.Object value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
    }
}
