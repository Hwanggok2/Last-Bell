using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Patches the existing saved dialogue only. Caller owns prefab/scene save and build.
    public static class DragonDialogueMigration
    {
        public const string PortraitRoot="Assets/Game/Art/Dialogue/Dragon-v4/";
        public static readonly string[] PortraitIds={"on","dam","rin","tori","rina","barun","doto","gib","unknown"};

        public static void Apply(MvpView view)
        {
            if(!view)throw new ArgumentException("Saved MVP view is required.");
            var overlay=view.GetComponentInChildren<PresentationOverlay>(true);
            Apply(overlay);
        }

        public static void Apply(PresentationOverlay overlay)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before updating saved dialogue.");
            if(!overlay||!overlay.StorySpeaker||!overlay.StoryText||!overlay.ButtonNext||!overlay.ButtonSkip)
                throw new InvalidOperationException("Existing saved dialogue bindings are required; this migration does not rebuild UI.");
            // Fail before changing UI if the authored portrait set is incomplete.
            foreach(var id in PortraitIds)
                if(!File.Exists(PortraitRoot+id+".png"))throw new FileNotFoundException("Authored dialogue portrait missing",PortraitRoot+id+".png");
            var portraits=PortraitIds.Select(id=>new DialoguePortraitEntry{Id=id,Sprite=Import(id)}).ToArray();
            var portrait=overlay.transform.Find("StoryPortrait");
            if(!portrait)
            {
                var go=new GameObject("StoryPortrait",typeof(RectTransform),typeof(Image));
                go.layer=overlay.gameObject.layer;go.transform.SetParent(overlay.transform,false);portrait=go.transform;
            }
            overlay.StoryPortrait=portrait.GetComponent<Image>()??portrait.gameObject.AddComponent<Image>();
            overlay.StoryPortrait.raycastTarget=false;overlay.StoryPortrait.preserveAspect=true;
            overlay.StoryPortrait.color=Color.white;overlay.StoryPortrait.sprite=portraits[3].Sprite;
            var r=overlay.StoryPortrait.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;
            r.anchoredPosition=new Vector2(28,18);r.sizeDelta=new Vector2(144,144);
            overlay.Portraits=portraits;overlay.DragonTheme=true;
            TextBox(overlay.StorySpeaker,28,138,174,FontStyle.Bold);
            TextBox(overlay.StoryText,32,24,132,FontStyle.Normal);
            overlay.StorySpeaker.color=DragonUiPalette.Ink;
            overlay.StoryText.color=DragonUiPalette.Ink;
            ButtonBox(overlay.ButtonNext,new Vector2(1,0),new Vector2(-92,76));
            ButtonBox(overlay.ButtonSkip,new Vector2(1,1),new Vector2(-92,-64));
            overlay.Hide();
            Dirty(overlay);Dirty(overlay.StoryPortrait);Dirty(overlay.StorySpeaker);Dirty(overlay.StoryText);
            Validate(overlay);
        }

        public static void Validate(PresentationOverlay overlay)
        {
            if(!overlay||!overlay.StoryPortrait||overlay.Portraits==null)
                throw new InvalidOperationException("Missing dialogue portrait binding.");
            foreach(var id in PortraitIds)
                if(overlay.Portraits.Count(x=>x!=null&&x.Id==id&&x.Sprite)==0)
                    throw new InvalidOperationException("Missing dialogue portrait: "+id);
            if(overlay.Portraits.Select(x=>x.Id).Distinct().Count()!=overlay.Portraits.Length)
                throw new InvalidOperationException("Duplicate dialogue portrait ID.");
            if(!overlay.StorySpeaker.font||!overlay.StoryText.font)
                throw new InvalidOperationException("Dialogue font is missing.");
        }

        static Sprite Import(string id)
        {
            string path=PortraitRoot+id+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None;importer.spritePixelsPerUnit=48;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path)??throw new InvalidOperationException("Portrait import failed: "+id);
        }

        static void TextBox(Text text,int fontSize,float bottom,float top,FontStyle style)
        {
            text.fontSize=fontSize;text.fontStyle=style;text.alignment=TextAnchor.UpperLeft;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.resizeTextForBestFit=false;text.lineSpacing=1.08f;text.raycastTarget=false;
            var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(.5f,.5f);
            r.offsetMin=new Vector2(204,bottom);r.offsetMax=new Vector2(-184,top);
        }

        static void ButtonBox(Button button,Vector2 anchor,Vector2 position)
        {
            var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);
            r.anchoredPosition=position;r.sizeDelta=new Vector2(144,96);
            var text=button.GetComponentInChildren<Text>(true);if(text){text.fontSize=28;Dirty(text);}
            Dirty(button);Dirty(r);
        }

        static void Dirty(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
