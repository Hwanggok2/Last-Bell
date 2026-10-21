using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class PresentationQaBuild
{
    public static void BuildHitPriority(){LastBell.Editor.MvpBuild.Prepare();Build("C:/Github/Test2/Temp/PresentationIntegration/BuildQA3",true);}
    public static void BuildObservations(){LastBell.Editor.MvpBuild.Prepare();Build("C:/Github/Test2/Temp/PresentationIntegration/BuildQA2",true);}
    public static void BuildFinal(){LastBell.Editor.MvpBuild.Prepare();Build("C:/Github/Test2/Temp/PresentationIntegration/BuildFinal",false);}
    public static void BuildCandidates()
    {
        LastBell.Editor.MvpBuild.Prepare();
        Build("C:/Github/Test2/Temp/PresentationIntegration/BuildR2",false);
        Build("C:/Github/Test2/Temp/PresentationIntegration/BuildQA",true);
    }
    static void Build(string folder,bool instrumentation)
    {
        Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Game/Scenes/Mvp.unity"},locationPathName=folder+"/LastBellMvp.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,
            extraScriptingDefines=instrumentation?new[]{"LASTBELL_QA"}:new string[0]
        });
        UnityEngine.Debug.Log($"QA_BUILD: {folder}; {report.summary.result}; {report.summary.totalErrors} errors");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Candidate build failed");
    }
}
