using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public static class FeedbackRevisionMigration
    {
        [MenuItem("Last Bell/Feedback/Apply v5.2")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isDirty)throw new InvalidOperationException("Save scene changes and leave Play mode before applying feedback.");
            PresentationArtImporter.MigrateExisting();
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{Configure(root.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            const string audioPath="Assets/Game/Prefabs/PresentationAudio.prefab";
            var audioRoot=PrefabUtility.LoadPrefabContents(audioPath);
            try{ConnectAudio(audioRoot.GetComponent<BattleAudio>());PrefabUtility.SaveAsPrefabAsset(audioRoot,audioPath);}
            finally{PrefabUtility.UnloadPrefabContents(audioRoot);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var obj in scene.GetRootGameObjects())foreach(var p in obj.GetComponentsInChildren<MvpPresenter>(true))
                {
                    // Existing scene overrides are preserved; refresh only revision-owned fields.
                    Configure(p.View);ConnectAudio(p.Presentation.Audio);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(p.View);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_FEEDBACK_V52_APPLIED");
        }
        static void Configure(MvpView v)
        {
            v.Timing=Ensure<TimingGraphic>(v.BattlePanel.transform,"TimingBar");v.Timing.raycastTarget=false;
            v.Timing.DirectionMarker=Ensure<SymbolGraphic>(v.Timing.transform,"DodgeDirection");v.Timing.DirectionMarker.raycastTarget=false;v.Timing.DirectionMarker.color=new Color32(255,198,92,255);
            v.Timing.DirectionLabel=Ensure<Text>(v.Timing.transform,"DirectionLabel");CopyText(v.Timing.DirectionLabel,v.Tutorial);v.Timing.DirectionLabel.alignment=TextAnchor.MiddleCenter;
            v.Timing.NextMarker=Ensure<SymbolGraphic>(v.Timing.transform,"NextDodge");v.Timing.NextMarker.raycastTarget=false;v.Timing.NextMarker.color=new Color32(155,170,181,180);
            v.Timing.NextLabel=Ensure<Text>(v.Timing.transform,"NextLabel");CopyText(v.Timing.NextLabel,v.Tutorial);v.Timing.NextLabel.text="다음";v.Timing.NextLabel.alignment=TextAnchor.MiddleCenter;
            var result=Ensure<Image>(v.SafeRoot,"DeathResult");Stretch(result.rectTransform);result.color=new Color32(12,23,35,255);result.raycastTarget=true;v.ResultPanel=result.gameObject;
            v.ResultTitle=ResultText(v,"ResultTitle");v.ResultScope=ResultText(v,"ResultScope");v.ResultGold=ResultText(v,"ResultGold");v.ResultFragments=ResultText(v,"ResultFragments");v.ResultRetryHint=ResultText(v,"ResultRetryHint");
            v.ResultRetry=ResultButton(v,"ResultRetry","다시하기");v.ResultLobby=ResultButton(v,"ResultLobby","로비");v.ResultPanel.SetActive(false);
            v.DodgeResult=Ensure<DodgeFeedback>(v.BattlePanel.transform,"DodgeResult");v.DodgeResult.raycastTarget=false;
            var label=Ensure<Text>(v.DodgeResult.transform,"PerfectLabel");CopyText(label,v.Tutorial);label.alignment=TextAnchor.MiddleCenter;label.text="";
            v.DodgeResult.Label=label;
            var outline=label.GetComponent<Outline>()??label.gameObject.AddComponent<Outline>();outline.effectColor=new Color32(9,20,32,240);outline.effectDistance=new Vector2(1,-1);
            var gain=Ensure<Image>(v.Stagger.transform.parent,"StaggerGain");gain.raycastTarget=false;gain.enabled=false;v.DodgeResult.StaggerGain=gain;
            foreach(var row in v.Stats){row.Price=Ensure<Text>(row.Upgrade.transform.parent,"Price");CopyText(row.Price,row.Value);row.Price.alignment=TextAnchor.MiddleRight;}
            foreach(var button in v.GetComponentsInChildren<Button>(true))Style(button);
            var attack=v.Attack.GetComponent<Image>();if(attack)attack.color=Color.clear;
            var attackPlate=Ensure<PlateGraphic>(v.Attack.transform,"Plate");Stretch(attackPlate.rectTransform);attackPlate.transform.SetAsFirstSibling();attackPlate.color=new Color32(33,52,67,255);attackPlate.Emphasis=true;attackPlate.raycastTarget=false;
            v.Attack.PressVisual=v.AttackSymbol.rectTransform;
            v.AttackPlate=attackPlate;
            for(int i=0;i<v.Dodge.Length;i++){v.Dodge[i].GetComponent<Image>().color=Color.clear;v.Dodge[i].PressVisual=v.DodgeSymbols[i].rectTransform;}
            var layout=v.GetComponent<FeedbackLayout>()??v.gameObject.AddComponent<FeedbackLayout>();layout.View=v;layout.Apply(960,540,false);
            EditorUtility.SetDirty(v);EditorUtility.SetDirty(layout);
        }
        static void Style(Button button)
        {
            var old=button.GetComponent<Image>();if(old){old.color=Color.clear;old.raycastTarget=true;}
            var plate=Ensure<PlateGraphic>(button.transform,"Plate");Stretch(plate.rectTransform);plate.transform.SetAsFirstSibling();
            plate.raycastTarget=false;plate.color=new Color32(33,52,67,255);plate.Emphasis=button.name=="Start"||button.name=="Confirm"||button.name=="ResultRetry";
            button.targetGraphic=plate;button.transition=Selectable.Transition.ColorTint;
            var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.1f,1.1f,1.1f);c.pressedColor=new Color(.65f,.72f,.78f);c.selectedColor=Color.white;c.disabledColor=new Color(.45f,.45f,.45f,.7f);c.fadeDuration=.04f;button.colors=c;
            var text=button.GetComponentInChildren<Text>();if(text){text.color=new Color32(240,222,183,255);text.raycastTarget=false;}
        }
        static void ConnectAudio(BattleAudio audio)
        {
            audio.PerfectDodge=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Presentation/perfect-confirm.wav");
            if(!audio.PerfectDodge)throw new InvalidDataException("Missing perfect dodge bell");EditorUtility.SetDirty(audio);
            if(PrefabUtility.IsPartOfPrefabInstance(audio))PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
        }
        static Text ResultText(MvpView v,string name)
        {
            var text=Ensure<Text>(v.ResultPanel.transform,name);CopyText(text,v.Tutorial);text.alignment=TextAnchor.MiddleCenter;return text;
        }
        static Button ResultButton(MvpView v,string name,string caption)
        {
            var button=Ensure<Button>(v.ResultPanel.transform,name);
            if(!button.GetComponent<Image>())button.gameObject.AddComponent<Image>();
            var text=Ensure<Text>(button.transform,"Label");CopyText(text,v.StartLabel);Stretch(text.rectTransform);text.alignment=TextAnchor.MiddleCenter;text.text=caption;return button;
        }
        static T Ensure<T>(Transform parent,string name) where T:Component
        {
            var found=parent.Find(name);var obj=found?found.gameObject:new GameObject(name,typeof(RectTransform));obj.layer=5;
            if(!found)obj.transform.SetParent(parent,false);return obj.GetComponent<T>()??obj.AddComponent<T>();
        }
        static void CopyText(Text dest,Text source)
        {dest.font=source.font;dest.fontSize=source.fontSize;dest.color=source.color;dest.raycastTarget=false;dest.horizontalOverflow=HorizontalWrapMode.Overflow;dest.verticalOverflow=VerticalWrapMode.Truncate;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        [MenuItem("Last Bell/Feedback/Validate v5.2")]
        public static void Validate()
        {
            var v=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            if(!v.Timing||!v.DodgeResult||!v.DodgeResult.Label||!v.DodgeResult.StaggerGain||!v.GetComponent<FeedbackLayout>())throw new InvalidDataException("Feedback references missing");
            foreach(var row in v.Stats)if(!row.Price)throw new InvalidDataException("Separate price binding missing");
            if(!v.ResultPanel||!v.ResultRetry||!v.ResultLobby||!v.Timing.DirectionMarker||!v.Timing.DirectionLabel)throw new InvalidDataException("Result or direction UI references missing");
            var a=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/PresentationAudio.prefab").GetComponent<BattleAudio>();
            if(!a.PerfectDodge||a.PerfectDodge==a.Dodge)throw new InvalidDataException("Perfect sound must be distinct");
            Debug.Log("LAST_BELL_FEEDBACK_REFERENCES_VALID");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
