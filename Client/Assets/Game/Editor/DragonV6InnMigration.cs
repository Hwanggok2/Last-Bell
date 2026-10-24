using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Patches the already saved v5 inn only. The coordinator owns loading/saving
    // scene and prefab assets; this operation never builds or recreates a view.
    public static class DragonV6InnMigration
    {
        public static void Apply(MvpView view)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before patching the saved inn.");
            DragonV5InnMigration.Validate(view);
            var layout=view.GetComponent<FeedbackLayout>();
            if(!layout||layout.View!=view||!view.Companions.Damage||!view.Companions.Damage.font)
                throw new InvalidOperationException("Existing lobby layout and companion text style are required.");
            var r=view.Reference;var inn=view.Companions;
            inn.SkillName=Label("DragonV6SkillName",inn.DetailPanel,inn.Damage);
            inn.SkillEffect=Label("DragonV6SkillEffect",inn.DetailPanel,inn.Damage);
            inn.SkillProgress=Label("DragonV6SkillProgress",inn.DetailPanel,inn.Damage);
            r.RestoreUpgradeV6=true;
            r.GrowthTab.gameObject.SetActive(false);r.CompanionsTab.gameObject.SetActive(true);
            r.CompanionsTab.GetComponentInChildren<Text>(true).text="동료";
            r.InnWallet.gameObject.SetActive(false);r.UpgradeWallet.gameObject.SetActive(true);r.StartStage.gameObject.SetActive(false);
            foreach(var row in r.GrowthRows)
            {
                row.Plus.gameObject.SetActive(false);
                foreach(var pip in row.Pips)pip.gameObject.SetActive(false);
            }
            layout.ApplyInnLayout(1280,720);
            // Preserve serialized array references and existing GameObjects. Only
            // new detail labels and the explicit lobby layout are dirtied here.
            foreach(var component in view.LobbyPanel.GetComponentsInChildren<Component>(true))Dirty(component);
            Dirty(view);Dirty(r);Dirty(inn);
            Validate(view);
        }

        public static void Validate(MvpView view)
        {
            DragonV5InnMigration.Validate(view);
            var r=view.Reference;var inn=view.Companions;
            if(!r.RestoreUpgradeV6||r.GrowthTab.gameObject.activeSelf||!r.CompanionsTab.gameObject.activeSelf)
                throw new InvalidOperationException("The restored upgrade UI requires one visible top-right companion control.");
            foreach(var text in new[]{inn.SkillName,inn.SkillEffect,inn.SkillProgress})
                if(!text||!text.font||text.transform.parent!=inn.DetailPanel||text.raycastTarget)
                    throw new InvalidOperationException("Dedicated saved companion skill labels are missing or intercept pointer input.");
            foreach(var row in r.GrowthRows)
            {
                if(row.Plus.gameObject.activeSelf)throw new InvalidOperationException("V5 instant-growth controls must stay hidden in the restored UI.");
                foreach(var pip in row.Pips)if(pip.gameObject.activeSelf)throw new InvalidOperationException("V5 growth pips must stay hidden in the restored UI.");
            }
            if(r.StatButtons.Length!=5||r.StatLabels.Length!=5||r.StatRatings.Length!=5)
                throw new InvalidOperationException("Recovery must remain the fifth selectable growth stat.");
        }

        static Text Label(string name,Transform parent,Text style)
        {
            var node=parent.Find(name);
            if(!node)
            {
                var created=new GameObject(name,typeof(RectTransform));created.layer=parent.gameObject.layer;
                created.transform.SetParent(parent,false);node=created.transform;
            }
            var text=node.GetComponent<Text>()??node.gameObject.AddComponent<Text>();
            text.font=style.font;text.fontSize=26;text.fontStyle=FontStyle.Normal;text.color=DragonUiPalette.Ink;
            text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;text.resizeTextForBestFit=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.lineSpacing=1;
            return text;
        }
        static void Dirty(UnityEngine.Object value)
        {
            EditorUtility.SetDirty(value);
            if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }
}
