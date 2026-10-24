using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // The coordinator loads/saves the existing prefab and scene. This migration
    // extends only the inn subtree and the existing stat menu; no scene rebuild.
    public static class DragonV5InnMigration
    {
        public const string PortraitRoot="Assets/Game/Art/Dialogue/Dragon-v5/";
        static readonly string[] CompanionIds={"tori","barun","doto","rina","momo"};
        static Font font;

        public static CompanionInnView Apply(MvpView view)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before patching the saved inn.");
            if(!view||!view.Reference||!view.LobbyPanel||!view.Reference.Upgrade||view.Reference.StatButtons==null||view.Reference.StatButtons.Length<4)
                throw new InvalidOperationException("Existing saved reference lobby is required.");
            font=view.Reference.Heading.font;if(!font)throw new InvalidOperationException("Existing pixel font is missing.");
            var portraits=new Sprite[CompanionIds.Length];
            for(int i=0;i<CompanionIds.Length;i++)
            {
                string path=PortraitRoot+CompanionIds[i]+".png";
                portraits[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(!portraits[i]||portraits[i].texture.filterMode!=FilterMode.Point)
                    throw new FileNotFoundException("Import the authored 24px Point portrait before migrating the inn",path);
            }
            var lobby=view.LobbyPanel.transform;var reference=view.Reference;
            reference.GrowthTab=Button("DragonV5GrowthTab",lobby,"성장");
            reference.CompanionsTab=Button("DragonV5CompanionsTab",lobby,"동료");
            reference.InnWallet=Label("DragonV5InnWallet",lobby,"",TextAnchor.MiddleRight);
            PatchGrowth(reference);
            view.StartRun.transform.SetAsLastSibling();

            var root=Child("DragonV5CompanionInn",lobby);Stretch(root);root.SetAsLastSibling();
            var inn=root.GetComponent<CompanionInnView>()??root.gameObject.AddComponent<CompanionInnView>();view.Companions=inn;inn.View=view;
            inn.ListPanel=Panel("Roster",root);inn.DetailPanel=Panel("Detail",root);
            inn.PartyTitle=Label("PartyTitle",inn.ListPanel,"출전");
            inn.Slots=new Button[3];inn.SlotLabels=new Text[3];inn.SlotPortraits=new Image[3];
            for(int i=0;i<3;i++)
            {
                inn.Slots[i]=Button("Slot"+i,inn.ListPanel,"+");inn.SlotLabels[i]=inn.Slots[i].GetComponentInChildren<Text>(true);
                inn.SlotPortraits[i]=Picture("Portrait",inn.Slots[i].transform,null);inn.SlotPortraits[i].raycastTarget=false;
            }
            inn.RosterViewport=Child("Viewport",inn.ListPanel);Get<RectMask2D>(inn.RosterViewport);
            inn.RosterContent=Child("Content",inn.RosterViewport);
            inn.RosterScroll=Get<ScrollRect>(inn.RosterViewport);inn.RosterScroll.viewport=inn.RosterViewport;inn.RosterScroll.content=inn.RosterContent;
            inn.RosterScroll.horizontal=false;inn.RosterScroll.vertical=true;inn.RosterScroll.movementType=ScrollRect.MovementType.Clamped;inn.RosterScroll.scrollSensitivity=30;
            inn.Rows=new DragonV5CompanionRow[CompanionIds.Length];inn.ArtBindings=new DragonV5CompanionArt[CompanionIds.Length];
            for(int i=0;i<CompanionIds.Length;i++)
            {
                string id=CompanionIds[i];var select=Button("Companion_"+id,inn.RosterContent,"");
                var caption=select.GetComponentInChildren<Text>(true);caption.gameObject.SetActive(false);
                inn.Rows[i]=new DragonV5CompanionRow{Select=select,Portrait=Picture("Portrait",select.transform,portraits[i]),Name=Label("Name",select.transform,id),State=Label("State",select.transform,"")};
                inn.ArtBindings[i]=new DragonV5CompanionArt{Id=id,ArtId="companion_"+id,Portrait=portraits[i]};
            }
            inn.Portrait=Picture("Portrait",inn.DetailPanel,portraits[0]);
            inn.Name=Label("Name",inn.DetailPanel,"토리");inn.Attack=Label("Attack",inn.DetailPanel,"");inn.Damage=Label("Damage",inn.DetailPanel,"");
            inn.Service=Button("Service",inn.DetailPanel,"");inn.Service.GetComponentInChildren<Text>(true).gameObject.SetActive(false);
            inn.ServiceTitle=Label("Title",inn.Service.transform,"");inn.ServiceSummary=Label("Summary",inn.Service.transform,"");
            inn.Upgrade=Button("Upgrade",inn.DetailPanel,"강화");inn.UpgradeLabel=inn.Upgrade.GetComponentInChildren<Text>(true);
            inn.Party=Button("Party",inn.DetailPanel,"출전");inn.PartyLabel=inn.Party.GetComponentInChildren<Text>(true);
            inn.Depart=Button("Depart",inn.DetailPanel,"Stage 1");inn.DepartLabel=inn.Depart.GetComponentInChildren<Text>(true);
            var modal=Child("Confirmation",root);Stretch(modal);inn.Modal=modal.gameObject;
            var shade=Get<Image>(modal);shade.color=new Color(0,0,0,.65f);shade.raycastTarget=true;
            inn.ModalCard=Panel("Card",modal);
            inn.ModalTitle=Label("Title",inn.ModalCard,"");inn.ModalBody=Label("Body",inn.ModalCard,"",TextAnchor.UpperLeft);inn.ModalBody.lineSpacing=1.2f;
            inn.Confirm=Button("Confirm",inn.ModalCard,"확인");inn.ConfirmLabel=inn.Confirm.GetComponentInChildren<Text>(true);
            inn.Cancel=Button("Cancel",inn.ModalCard,"닫기");inn.ReplaceSlots=new Button[3];inn.ReplaceLabels=new Text[3];
            for(int i=0;i<3;i++){inn.ReplaceSlots[i]=Button("Replace"+i,inn.ModalCard,"");inn.ReplaceLabels[i]=inn.ReplaceSlots[i].GetComponentInChildren<Text>(true);inn.ReplaceSlots[i].gameObject.SetActive(false);}
            inn.Modal.SetActive(false);inn.Layout(1280,720);inn.gameObject.SetActive(false);
            reference.GrowthTab.gameObject.SetActive(false);reference.CompanionsTab.gameObject.SetActive(false);reference.InnWallet.gameObject.SetActive(false);
            Dirty(view);Dirty(reference);
            foreach(var component in root.GetComponentsInChildren<Component>(true))Dirty(component);
            foreach(var component in reference.Upgrade.GetComponentsInChildren<Component>(true))Dirty(component);
            foreach(var button in new[]{reference.GrowthTab,reference.CompanionsTab})foreach(var component in button.GetComponentsInChildren<Component>(true))Dirty(component);
            Dirty(reference.InnWallet);Dirty(reference.InnWallet.rectTransform);
            Validate(view);return inn;
        }
        static void PatchGrowth(ReferenceLobbyView reference)
        {
            var menu=reference.StatButtons[0].transform.parent;var previous=reference.StatButtons;
            reference.StatButtons=new Button[5];reference.StatLabels=new Text[5];reference.StatRatings=new Text[5];reference.GrowthRows=new DragonV5GrowthRow[5];
            string[] names={"공격력","체력","행운","기세","회복력"};
            for(int i=0;i<5;i++)
            {
                var button=i<previous.Length&&previous[i]?previous[i]:Button("SelectStat"+i,menu,"");
                reference.StatButtons[i]=button;reference.StatLabels[i]=Label("Caption",button.transform,names[i]);reference.StatRatings[i]=Label("Rating",button.transform,"");
                // Keep the + hit target beside the selector, never nested inside
                // another Button. This also avoids overlapping pointer targets.
                var oldPlus=button.transform.Find("DragonV5Plus");
                if(oldPlus){oldPlus.SetParent(menu,false);oldPlus.name="DragonV5Plus"+i;}
                var plus=Button("DragonV5Plus"+i,menu,"+");var pips=new Image[10];
                for(int j=0;j<10;j++){pips[j]=Get<Image>(Child("DragonV5Pip"+j,button.transform));pips[j].color=DragonUiPalette.Disabled;pips[j].raycastTarget=false;}
                reference.GrowthRows[i]=new DragonV5GrowthRow{Plus=plus,Pips=pips};
            }
        }
        public static void Validate(MvpView view)
        {
            if(!view||!view.Companions||!view.Reference.GrowthTab||!view.Reference.CompanionsTab||!view.Reference.InnWallet||view.Reference.StatButtons.Length!=5||view.Reference.GrowthRows.Length!=5)
                throw new InvalidOperationException("Five growth rows and inn tabs must be serialized.");
            var inn=view.Companions;
            if(inn.Rows.Length!=5||inn.ArtBindings.Length!=5||inn.Slots.Length!=3||inn.ReplaceSlots.Length!=3||!inn.Modal||!inn.Service||!inn.RosterScroll)
                throw new InvalidOperationException("Missing saved companion roster/modal references.");
            foreach(var binding in inn.ArtBindings)if(!binding.Portrait)throw new InvalidOperationException("Missing companion portrait: "+binding.Id);
            foreach(var row in view.Reference.GrowthRows)if(!row.Plus||row.Pips.Length!=10)throw new InvalidOperationException("Each growth stat requires ten pips and a plus control.");
            if(view.Reference.Purchase.transform.parent!=view.Reference.Upgrade.transform||view.Reference.Exchange.transform.parent!=view.Reference.Upgrade.transform)
                throw new InvalidOperationException("Purchase and exchange coordinates require the existing full-screen ReferenceUpgrade parent.");
            foreach(var row in view.Reference.GrowthRows)if(row.Plus.transform.parent!=view.Reference.StatButtons[0].transform.parent)
                throw new InvalidOperationException("Growth plus controls must be siblings of the stat selectors.");
        }
        static RectTransform Child(string name,Transform parent)
        {
            var old=parent.Find(name);if(old)return (RectTransform)old;
            var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        static T Get<T>(RectTransform rect)where T:Component=>rect.GetComponent<T>()??rect.gameObject.AddComponent<T>();
        static RectTransform Panel(string name,Transform parent)
        {
            var rect=Child(name,parent);var face=Get<ReferencePanelGraphic>(rect);face.DragonTheme=true;face.raycastTarget=false;face.color=Color.white;return rect;
        }
        static Text Label(string name,Transform parent,string text,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var label=Get<Text>(Child(name,parent));label.font=font;label.fontSize=28;label.fontStyle=FontStyle.Normal;label.text=text;label.color=DragonUiPalette.Ink;
            label.alignment=align;label.raycastTarget=false;label.resizeTextForBestFit=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;
        }
        static Button Button(string name,Transform parent,string text)
        {
            var rect=Panel(name,parent);var face=rect.GetComponent<ReferencePanelGraphic>();face.raycastTarget=true;
            var button=Get<Button>(rect);button.targetGraphic=face;button.transition=Selectable.Transition.None;face.Control=button;
            var caption=Label("Caption",rect,text,TextAnchor.MiddleCenter);Stretch(caption.rectTransform);
            var feedback=Get<DragonButtonFeedback>(rect);feedback.Button=button;feedback.Panels=new[]{face};feedback.Labels=new[]{caption};return button;
        }
        static Image Picture(string name,Transform parent,Sprite sprite)
        {var image=Get<Image>(Child(name,parent));image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.color=sprite?Color.white:Color.clear;return image;}
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static void Dirty(UnityEngine.Object target){EditorUtility.SetDirty(target);if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
    }
}
