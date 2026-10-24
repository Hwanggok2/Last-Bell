using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBell.Editor
{
    public static class GateCheckpointMigration
    {
        const string WorldPath="Assets/Game/Prefabs/PresentationWorld.prefab";
        const string MaterialPath="Assets/Game/Art/Presentation/GatePortcullis.mat";
        [MenuItem("Last Bell/Gate checkpoint/Apply")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isDirty)
                throw new InvalidOperationException("Stop Play mode and save scene changes first.");
            var shader=Shader.Find("LastBell/Gate Portcullis");
            if(!shader)throw new InvalidOperationException("Missing gate shader.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,MaterialPath);}
            material.shader=shader;material.EnableKeyword("PIXELSNAP_ON");EditorUtility.SetDirty(material);
            var root=PrefabUtility.LoadPrefabContents(WorldPath);
            try{Configure(root.GetComponent<ParallaxWorld>(),material);PrefabUtility.SaveAsPrefabAsset(root,WorldPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var r in scene.GetRootGameObjects())foreach(var world in r.GetComponentsInChildren<ParallaxWorld>(true))Configure(world,material);
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_GATE_CHECKPOINT_APPLIED");
        }
        static void Configure(ParallaxWorld world,Material material)
        {
            if(!world||!world.Gate||!world.GateClosed)throw new InvalidOperationException("Missing saved gate.");
            world.Gate.sharedMaterial=material;world.Gate.sprite=world.GateClosed;
            EditorUtility.SetDirty(world.Gate);
            if(PrefabUtility.IsPartOfPrefabInstance(world.Gate))PrefabUtility.RecordPrefabInstancePropertyModifications(world.Gate);
        }
        public static void Validate()
        {
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath).GetComponent<ParallaxWorld>();
            if(!world.Gate||!world.GateClosed||world.Gate.sharedMaterial.shader.name!="LastBell/Gate Portcullis")
                throw new InvalidOperationException("Gate portcullis material is not saved.");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
