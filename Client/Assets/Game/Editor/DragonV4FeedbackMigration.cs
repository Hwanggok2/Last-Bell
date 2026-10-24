using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Only updates the existing meter and audio bindings; caller owns prefab/scene save.
    public static class DragonV4FeedbackMigration
    {
        const string AudioRoot="Assets/Game/Audio/Dragon-v4/";
        public static void Configure(MvpView view)
        {
            if(!view||!view.Stagger)throw new InvalidOperationException("Saved stagger meter is missing.");
            var background=view.Stagger.transform.parent.GetComponent<Image>();
            if(!background)throw new InvalidOperationException("Saved meter background is missing.");
            background.color=DragonUiPalette.Ink;
            var outline=background.GetComponent<Outline>()??background.gameObject.AddComponent<Outline>();
            outline.effectColor=DragonUiPalette.Ink;outline.effectDistance=new Vector2(2,-2);outline.useGraphicAlpha=true;
            view.Stagger.color=DragonUiPalette.StaggerFill;
            if(view.AudioCredits)view.AudioCredits.Credits=AssetDatabase.LoadAssetAtPath<TextAsset>(AudioRoot+"UI_CREDITS.txt");
            view.GetComponent<FeedbackLayout>().Apply(1280,720,false);
            foreach(var value in new UnityEngine.Object[]{view,view.Stagger,background,outline,view.Stagger.transform.parent,view.AudioCredits})if(value)Dirty(value);
        }
        public static void Configure(BattleAudio audio)
        {
            if(!audio)throw new InvalidOperationException("Saved battle audio is missing.");
            audio.Swing=Load("pixel-hit.wav");audio.AttackTouch=Load("pixel-touch.wav");Dirty(audio);
        }
        static AudioClip Load(string name)
        {
            string path=AudioRoot+name;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(!importer)throw new InvalidOperationException("Missing pixel sound: "+path);
            importer.forceToMono=true;importer.loadInBackground=false;
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
            importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        public static void Validate(MvpView view,BattleAudio audio)
        {
            if(!view.Stagger.transform.parent.GetComponent<Outline>()||!audio.AttackTouch||!audio.Swing||audio.Swing.name!="pixel-hit"||audio.AttackTouch.name!="pixel-touch")
                throw new InvalidOperationException("Dragon v4 feedback bindings are incomplete.");
        }
        static void Dirty(UnityEngine.Object value)
        {EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
    }
}
