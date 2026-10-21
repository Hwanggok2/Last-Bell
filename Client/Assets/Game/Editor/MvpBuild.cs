using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastBell.Editor
{
    public static class MvpBuild
    {
        [MenuItem("Last Bell/Build/Prepare MVP")]
        public static void Prepare()
        {
            MvpArtImporter.CreateMissing();
            MvpSceneBuilder.CreateMissing();
            MvpSceneBuilder.Validate();
            PresentationMigration.Validate();
            CombatV55Migration.Validate();
            CombatV59Migration.Validate();
            PlayerSettings.companyName="LastBellPrototype";
            PlayerSettings.productName="Last Bell MVP";
            PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.defaultScreenWidth=960;
            PlayerSettings.defaultScreenHeight=540;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;
            PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.allowedAutorotateToPortrait=false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Game/Scenes/Mvp.unity",true)};
            AssetDatabase.SaveAssets();
            Debug.Log("LAST_BELL_PREPARED");
        }

        [MenuItem("Last Bell/Build/Windows MVP")]
        public static void Windows()
        {
            Prepare();
            string destination=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Windows/LastBellMvp.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Game/Scenes/Mvp.unity"},locationPathName=destination,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development
            });
            Debug.Log($"LAST_BELL_BUILD: {report.summary.result}; {report.summary.totalSize} bytes; {report.summary.totalErrors} errors");
            if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("MVP build failed");
        }

        [MenuItem("Last Bell/Build/Android Test APK")]
        public static void Android()
        {
            Prepare();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.lastbell.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android,ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.bundleVersionCode=1;
            EditorUserBuildSettings.buildAppBundle=false;
            string destination=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Android/LastBellMvp.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Game/Scenes/Mvp.unity"},locationPathName=destination,
                target=BuildTarget.Android,options=BuildOptions.Development
            });
            Debug.Log($"LAST_BELL_ANDROID_BUILD: {report.summary.result}; {report.summary.totalSize} bytes; {report.summary.totalErrors} errors");
            if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Android MVP build failed");
        }
    }
}
