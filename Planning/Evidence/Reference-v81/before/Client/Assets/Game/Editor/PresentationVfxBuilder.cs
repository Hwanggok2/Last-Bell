using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public sealed class PresentationVfxBindings
    {
        public CombatVfx CombatVfx;
        public StageBannerGraphic StageBanner;
        public PresentationOverlay Overlay;
        public DangerGraphic ImpactDanger;
        public Button ButtonNext,ButtonSkip;
        public Text StorySpeaker,StoryText;
    }

    // Only creates missing children. The caller owns prefab save / scene references / migrations.
    public static class PresentationVfxBuilder
    {
        public static PresentationVfxBindings Ensure(MvpView view)
        {
            var result=new PresentationVfxBindings();var parent=view.SafeRoot;
            result.CombatVfx=Graphic<CombatVfx>(parent,"CombatVfx");
            result.StageBanner=Graphic<StageBannerGraphic>(parent,"StageBanner");
            result.ImpactDanger=Graphic<DangerGraphic>(parent,"ImpactDanger");
            result.ImpactDanger.Hide();
            var slash=Child<Image>(result.CombatVfx.transform,"SlashSprite",out bool slashNew);slash.raycastTarget=false;
            if(slashNew){var r=slash.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(26/256f,24/128f);r.sizeDelta=new Vector2(256,128);slash.color=Color.white;slash.gameObject.SetActive(false);}
            result.CombatVfx.SlashSprite=slash;
            result.Overlay=Graphic<PresentationOverlay>(parent,"PresentationOverlay");
            Font font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NanumGothic-Regular.ttf");
            result.StorySpeaker=Label(result.Overlay.transform,"StorySpeaker",font,18,new Vector2(.04f,0),new Vector2(.79f,0),new Vector2(0,102),new Vector2(0,132));
            result.StoryText=Label(result.Overlay.transform,"StoryText",font,27,new Vector2(.04f,0),new Vector2(.79f,0),new Vector2(0,22),new Vector2(0,99));
            if(result.StoryText.fontSize==24)result.StoryText.fontSize=27;
            result.StoryText.alignment=TextAnchor.UpperLeft;result.StoryText.horizontalOverflow=HorizontalWrapMode.Wrap;
            result.ButtonNext=Button(result.Overlay.transform,"StoryNext","다음",font,new Vector2(1,0),new Vector2(-94,62),new Vector2(132,72));
            result.ButtonSkip=Button(result.Overlay.transform,"StorySkip","건너뛰기",font,new Vector2(1,1),new Vector2(-94,-54),new Vector2(132,72));
            result.Overlay.StorySpeaker=result.StorySpeaker;result.Overlay.StoryText=result.StoryText;
            result.Overlay.ButtonNext=result.ButtonNext;result.Overlay.ButtonSkip=result.ButtonSkip;result.Overlay.Hide();
            // World VFX behind battle HUD, danger above those effects, modal/pause stay foremost.
            result.CombatVfx.transform.SetAsFirstSibling();result.ImpactDanger.transform.SetSiblingIndex(1);
            result.StageBanner.transform.SetAsLastSibling();result.Overlay.transform.SetAsLastSibling();
            if(view.PausePanel)view.PausePanel.transform.SetAsLastSibling();if(view.ModalPanel)view.ModalPanel.transform.SetAsLastSibling();
            if(view.Danger){MigrateInitialDanger(view.Danger.rectTransform,new Vector2(120,130));view.Danger.raycastTarget=false;view.Danger.transform.SetAsFirstSibling();}
            if(view.NextDanger){MigrateInitialDanger(view.NextDanger.rectTransform,new Vector2(138,146));view.NextDanger.raycastTarget=false;view.NextDanger.NextPreview=true;view.NextDanger.transform.SetAsFirstSibling();}
            EditorUtility.SetDirty(view.gameObject);return result;
        }
        static T Graphic<T>(Transform parent,string name) where T:MaskableGraphic
        {
            var c=Child<T>(parent,name,out bool created);if(created){Stretch(c.rectTransform);c.color=Color.white;}c.raycastTarget=false;return c;
        }
        static T Child<T>(Transform parent,string name,out bool created) where T:Component
        {
            Transform child=parent.Find(name);created=child==null;
            if(created){var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);child=go.transform;}
            var c=child.GetComponent<T>();return c?c:child.gameObject.AddComponent<T>();
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;}
        static void MigrateInitialDanger(RectTransform r,Vector2 originalSize){if(r.sizeDelta==originalSize)Stretch(r);}
        static Text Label(Transform parent,string name,Font font,int size,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi)
        {
            var text=Child<Text>(parent,name,out bool created);
            if(created){text.font=font;text.fontSize=size;text.color=new Color32(255,240,180,255);text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;var r=text.rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=lo;r.offsetMax=hi;}
            return text;
        }
        static Button Button(Transform parent,string name,string caption,Font font,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var image=Child<Image>(parent,name,out bool created);var button=image.GetComponent<Button>();if(!button)button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            if(created){image.color=new Color32(31,54,74,255);var r=image.rectTransform;r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;}
            var label=Label(image.transform,"Label",font,22,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);label.alignment=TextAnchor.MiddleCenter;label.text=caption;return button;
        }
    }
}
