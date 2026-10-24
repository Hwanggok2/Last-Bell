using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Only the caller loads/saves the existing prefab or scene. No scene generation.
    public static class DragonV093GameplayMigration
    {
        public static void Configure(MvpView view) => Apply(view);

        public static void Apply(MvpView view)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before updating saved controls.");
            if(!view||!view.BattlePanel||!view.Stage||!view.Stage.font)throw new ArgumentException("Existing saved view and font are required.");
            var shieldSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Dragon-v1/shield-normal.png");
            if(!shieldSprite)throw new InvalidOperationException("Import the existing Dragon shield sprite before configuring saved skill controls.");
            var node=view.BattlePanel.transform.Find("UniqueSkill");
            if(!node)
            {
                var go=new GameObject("UniqueSkill",typeof(RectTransform),typeof(Image),typeof(PointerCommand),typeof(Button));
                go.layer=view.BattlePanel.layer;go.transform.SetParent(view.BattlePanel.transform,false);node=go.transform;
            }
            view.Skill=node.GetComponent<PointerCommand>()??node.gameObject.AddComponent<PointerCommand>();
            var hit=node.GetComponent<Image>()??node.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var button=node.GetComponent<Button>()??node.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
            button.navigation=new Navigation{mode=Navigation.Mode.None};button.targetGraphic=hit;
            var face=node.Find("Face");
            if(!face){var go=new GameObject("Face",typeof(RectTransform),typeof(ReferencePanelGraphic));go.layer=node.gameObject.layer;go.transform.SetParent(node,false);face=go.transform;}
            face.SetAsFirstSibling();var faceRect=(RectTransform)face;
            faceRect.anchorMin=Vector2.zero;faceRect.anchorMax=Vector2.one;faceRect.offsetMin=faceRect.offsetMax=Vector2.zero;
            view.SkillFace=face.GetComponent<ReferencePanelGraphic>()??face.gameObject.AddComponent<ReferencePanelGraphic>();
            view.SkillFace.DragonTheme=true;view.SkillFace.Control=button;view.SkillFace.raycastTarget=false;
            view.Skill.PixelPress=true;view.Skill.PressVisual=(RectTransform)node;
            var rect=(RectTransform)node;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);
            float scale=Mathf.Max(1,view.SafeRoot.rect.height)/720f;
            rect.sizeDelta=new Vector2(224,112)*scale;rect.anchoredPosition=new Vector2(0,20*scale);
            view.SkillLabel=Label(node,"SkillName",view.Stage.font,.48f,.91f,"스킬",scale);
            view.SkillCooldown=Label(node,"SkillState",view.Stage.font,.08f,.49f,"사용",scale);
            var shield=node.Find("ShieldActive");
            if(!shield){var go=new GameObject("ShieldActive",typeof(RectTransform),typeof(Image));go.layer=node.gameObject.layer;go.transform.SetParent(node,false);shield=go.transform;}
            view.SkillShieldIcon=shield.GetComponent<Image>()??shield.gameObject.AddComponent<Image>();
            view.SkillShieldIcon.sprite=shieldSprite;view.SkillShieldIcon.color=Color.white;view.SkillShieldIcon.raycastTarget=false;view.SkillShieldIcon.preserveAspect=true;
            var shieldRect=view.SkillShieldIcon.rectTransform;shieldRect.anchorMin=shieldRect.anchorMax=new Vector2(.5f,1);shieldRect.pivot=new Vector2(.5f,0);
            shieldRect.anchoredPosition=new Vector2(0,8*scale);shieldRect.sizeDelta=shieldSprite.rect.size*scale;shield.gameObject.SetActive(false);
            view.Skill.Interactable=false;node.gameObject.SetActive(false);
            Dirty(view);foreach(var component in node.GetComponentsInChildren<Component>(true))Dirty(component);
            Validate(view);
        }

        public static void Apply(MvpPresenter presenter)
        {
            if(!presenter)throw new ArgumentException("Existing saved presenter is required.");
            Apply(presenter.View);
        }

        public static void Validate(MvpView view)
        {
            if(!view||!view.Skill||!view.SkillLabel||!view.SkillCooldown||!view.SkillFace||!view.SkillFace.Control||!view.SkillShieldIcon||!view.SkillShieldIcon.sprite||view.SkillShieldIcon.raycastTarget||
                view.Skill.transform.parent!=view.BattlePanel.transform||view.SkillLabel.font!=view.Stage.font)
                throw new InvalidOperationException("Incomplete saved skill control.");
        }

        static Text Label(Transform parent,string name,Font font,float bottom,float top,string text,float scale)
        {
            var node=parent.Find(name);
            if(!node){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);node=go.transform;}
            var label=node.GetComponent<Text>()??node.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.raycastTarget=false;
            label.alignment=TextAnchor.MiddleCenter;label.fontSize=Mathf.RoundToInt(28*scale);label.resizeTextForBestFit=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;label.color=DragonUiPalette.Ink;
            var r=label.rectTransform;r.anchorMin=new Vector2(.04f,bottom);r.anchorMax=new Vector2(.96f,top);r.offsetMin=r.offsetMax=Vector2.zero;
            return label;
        }

        static void Dirty(UnityEngine.Object value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
    }
}
