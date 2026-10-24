using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public static class AudioV72Migration
    {
        const string Folder="Assets/Game/Audio/Combat-v72/",AudioPrefab="Assets/Game/Prefabs/PresentationAudio.prefab";
        [MenuItem("Last Bell/Audio v7.2/Apply")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isDirty)throw new InvalidOperationException("Stop Play mode and save scene changes first.");
            foreach(string name in new[]{"sword-fire.wav","player-body-hit.wav","dodge-whoosh.wav"})
            {
                string path=Folder+name;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=false;importer.loadInBackground=false;
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            var audio=PrefabUtility.LoadPrefabContents(AudioPrefab);
            try{Connect(audio.GetComponent<BattleAudio>());PrefabUtility.SaveAsPrefabAsset(audio,AudioPrefab);}
            finally{PrefabUtility.UnloadPrefabContents(audio);}
            var view=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{ConfigureCredits(view.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(view,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(view);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var root in scene.GetRootGameObjects())foreach(var p in root.GetComponentsInChildren<MvpPresenter>(true))
                {Connect(p.Presentation.Audio);ConfigureCredits(p.View);}
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_AUDIO_V72_APPLIED");
        }
        static void Connect(BattleAudio a)
        {
            a.Swing=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Combat-v86/sword-strike.wav");
            a.Hit=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"player-body-hit.wav");
            a.Dodge=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Combat-v86/dodge-air.wav");
            a.EnemySwing=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Reviewed-v4/sword-clash.wav");
            a.EnemyImpact=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Reviewed-v4/hit-sharp.wav");Dirty(a);
        }
        static RectTransform Rect(string name,Transform parent)
        {
            var old=parent.Find(name);if(old)return (RectTransform)old;
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        static T Get<T>(RectTransform r) where T:Component => r.GetComponent<T>()??r.gameObject.AddComponent<T>();
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static Button Button(string name,Transform parent,Button template,string label)
        {
            var old=parent.Find(name);var b=old?old.GetComponent<Button>():UnityEngine.Object.Instantiate(template,parent);
            b.name=name;b.onClick=new Button.ButtonClickedEvent();b.GetComponentInChildren<Text>().text=label;return b;
        }
        static void ConfigureCredits(MvpView v)
        {
            var c=v.AudioCredits?v.AudioCredits:v.gameObject.AddComponent<AudioCreditsView>();v.AudioCredits=c;
            c.Credits=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Audio/Combat-v86/UI_CREDITS.txt");
            c.SourceUrl="https://opengameart.org/content/4-chiptunes-adventure";c.LicenseUrl="https://creativecommons.org/publicdomain/zero/1.0/";
            c.OpenButton=Button("AudioCredits",v.LobbyPanel.transform,v.ReplayPrologue,"음원 출처");
            var panel=Rect("AudioCreditsPanel",v.LobbyPanel.transform);Stretch(panel);c.Panel=panel.gameObject;
            Get<Image>(panel).color=new Color(.02f,.04f,.07f,.94f);
            c.Dialog=Rect("Dialog",panel);Get<Image>(c.Dialog).color=new Color32(22,40,54,255);
            c.Heading=Get<Text>(Rect("Heading",c.Dialog));c.Heading.font=v.ModalBody.font;c.Heading.text="음원 출처 · 라이선스";c.Heading.color=new Color32(241,210,151,255);c.Heading.raycastTarget=false;
            var scroll=Rect("Scroll",c.Dialog);c.Scroll=Get<ScrollRect>(scroll);c.Scroll.horizontal=false;c.Scroll.vertical=true;c.Scroll.movementType=ScrollRect.MovementType.Clamped;c.Scroll.scrollSensitivity=32;
            var viewport=Rect("Viewport",scroll);Stretch(viewport);Get<Image>(viewport).color=new Color(0,0,0,.02f);Get<RectMask2D>(viewport);
            c.Body=Get<Text>(Rect("Credits",viewport));c.Body.font=v.ModalBody.font;c.Body.text=c.Credits.text;c.Body.color=new Color32(231,224,206,255);
            c.Body.alignment=TextAnchor.UpperLeft;c.Body.horizontalOverflow=HorizontalWrapMode.Wrap;c.Body.verticalOverflow=VerticalWrapMode.Overflow;c.Body.lineSpacing=1.2f;c.Body.raycastTarget=false;
            var content=c.Body.rectTransform;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
            Get<ContentSizeFitter>(content).verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            c.Scroll.viewport=viewport;c.Scroll.content=content;
            c.SourceButton=Button("Source",c.Dialog,v.ReplayPrologue,"음악 원본");c.LicenseButton=Button("License",c.Dialog,v.ReplayPrologue,"CC0 1.0");c.CloseButton=Button("Close",c.Dialog,v.ReplayPrologue,"닫기");
            panel.SetAsLastSibling();panel.gameObject.SetActive(false);
            v.GetComponent<FeedbackLayout>().Apply(960,540,false);
            foreach(var g in panel.GetComponentsInChildren<Graphic>(true))if(!g.GetComponent<CanvasRenderer>())g.gameObject.AddComponent<CanvasRenderer>();
            Dirty(v);Dirty(c);foreach(var component in panel.GetComponentsInChildren<Component>(true))Dirty(component);Dirty(c.OpenButton);
        }
        static void Dirty(UnityEngine.Object obj){EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
        public static void Validate()
        {
            var c=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>().AudioCredits;
            if(!c||!c.Credits||!c.Credits.text.Contains("Juhani Junkala")||!c.Credits.text.Contains("CC0-1.0")||!c.OpenButton||!c.CloseButton||!c.Scroll)throw new InvalidOperationException("Missing saved audio credits.");
            foreach(string name in new[]{"CREDITS.txt","manifest.json","CC-BY-4.0.txt","CC0-1.0.txt"})
                if(!File.Exists("Assets/StreamingAssets/AudioLicenses/"+name))throw new InvalidOperationException("Missing distributed license: "+name);
            var manifest=JsonUtility.FromJson<AudioManifest>(File.ReadAllText(Folder+"manifest.json"));
            using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var file in manifest.files)
            {
                string actual=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Folder+file.file))).Replace("-","").ToLowerInvariant();
                if(actual!=file.sha256)throw new InvalidOperationException("Audio/license manifest hash mismatch: "+file.file);
            }
        }
        [Serializable] sealed class AudioManifest { public AudioFile[] files; }
        [Serializable] sealed class AudioFile { public string file,sha256; }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
        public static void ApplyAndBuildAll(){Apply();MvpBuild.Windows();MvpBuild.Web();}
    }
}
