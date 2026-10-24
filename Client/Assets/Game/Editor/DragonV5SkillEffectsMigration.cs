using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LastBell.Editor
{
    // Only creates missing dedicated effects assets and patches the caller's loaded presenter.
    // The root migration owns scene/prefab saving and the final Unity validation pass.
    public static class DragonV5SkillEffectsMigration
    {
        public const string Folder = "Assets/Game/Art/Effects/Dragon-v5";
        public const string PrefabPath = Folder + "/SkillPixelEffects.prefab";
        const string TexturePath = Folder + "/square-point.png", MaterialPath = Folder + "/PixelParticles.mat";

        public static SkillPixelEffects Configure(MvpPresenter presenter)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before configuring saved skill particle assets.");
            if (!presenter || !presenter.View || !presenter.View.SafeRoot || !presenter.Presentation)
                throw new InvalidOperationException("Existing presenter, view SafeRoot and BattlePresentation are required.");
            GameObject prefab = CreateMissing();
            var node = presenter.View.SafeRoot.Find("SkillPixelEffects");
            SkillPixelEffects effects;
            if (!node)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, presenter.View.SafeRoot);
                instance.name = "SkillPixelEffects";
                effects = instance.GetComponent<SkillPixelEffects>();
                // Draw on both battle and inn, but below existing modal/pause controls.
                instance.transform.SetAsLastSibling();
                var modal = presenter.View.ModalPanel;
                var pause = presenter.View.PausePanel;
                int before = instance.transform.GetSiblingIndex();
                if (modal && modal.transform.parent == instance.transform.parent) before = Mathf.Min(before, modal.transform.GetSiblingIndex());
                if (pause && pause.transform.parent == instance.transform.parent) before = Mathf.Min(before, pause.transform.GetSiblingIndex());
                instance.transform.SetSiblingIndex(before);
            }
            else
            {
                effects = node.GetComponent<SkillPixelEffects>();
                if (!effects) throw new InvalidOperationException("Existing SkillPixelEffects node has no component; preserve and repair that authored node.");
            }
            effects.ValidateSavedReferences();
            presenter.Presentation.SkillEffects = effects;
            Dirty(effects); Dirty(presenter.Presentation);
            return effects;
        }

        public static GameObject CreateMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before creating skill particle assets.");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var material = EnsureMaterial();
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing)
            {
                var old = existing.GetComponent<SkillPixelEffects>();
                if (!old) throw new InvalidOperationException("Existing skill particles prefab lacks SkillPixelEffects; refusing a full prefab rebuild.");
                old.ValidateSavedReferences();
                return existing;
            }
            var root = new GameObject("SkillPixelEffects", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(SkillPixelEffects));
            root.layer = 5;
            try
            {
                var effects = root.GetComponent<SkillPixelEffects>();
                effects.Root = (RectTransform)root.transform;
                effects.Root.anchorMin = Vector2.zero; effects.Root.anchorMax = Vector2.one;
                effects.Root.offsetMin = effects.Root.offsetMax = Vector2.zero;
                effects.Root.pivot = new Vector2(.5f, .5f);
                effects.Surface = root.GetComponent<RawImage>();
                effects.Surface.raycastTarget = false; effects.Surface.color = Color.white; effects.Surface.enabled = false;
                var cameraNode = new GameObject("ParticleOnlyCamera", typeof(Camera));
                cameraNode.transform.SetParent(root.transform, false);
                cameraNode.transform.localPosition = new Vector3(16384, 16384, -10);
                effects.ParticleCamera = cameraNode.GetComponent<Camera>();
                var camera = effects.ParticleCamera;
                camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 135;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 2; // Existing Ignore Raycast layer; isolated outside the playable world.
                camera.allowHDR = false; camera.allowMSAA = false; camera.useOcclusionCulling = false;
                effects.AttackParticles = System(root.transform, "SkillContact", SkillPixelEffects.AttackCapacity, material, 1);
                effects.ShieldParticles = System(root.transform, "ActualShield", SkillPixelEffects.ShieldCapacity, material, 0);
                effects.SealParticles = System(root.transform, "ActualStagger", SkillPixelEffects.SealCapacity, material, 2);
                effects.SupportParticles = System(root.transform, "ServiceAndRecovery", SkillPixelEffects.SupportCapacity, material, 3);
                effects.ValidateSavedReferences();
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static Material EnsureMaterial()
        {
            if (!File.Exists(TexturePath))
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white); texture.Apply();
                File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
                importer.textureType = TextureImporterType.Default; importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            var pixel = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            var settings = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (!pixel || settings == null || settings.filterMode != FilterMode.Point || settings.mipmapEnabled)
                throw new InvalidOperationException("Dragon-v5 square texture must be imported with Point filtering and no mipmaps.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (!shader) throw new InvalidOperationException("Built-in unlit Sprites/Default shader is unavailable.");
                material = new Material(shader) { name = "DragonV5PixelParticles", mainTexture = pixel };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            if (!material.mainTexture || material.mainTexture != pixel)
                throw new InvalidOperationException("Dragon-v5 particle material points to a missing or different square texture; authored asset was not overwritten.");
            return material;
        }

        static ParticleSystem System(Transform parent, string name, int capacity, Material material, int order)
        {
            var node = new GameObject(name, typeof(ParticleSystem)); node.layer = 2;
            node.transform.SetParent(parent, false);
            var system = node.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.useAutoRandomSeed = false; system.randomSeed = 73;
            var main = system.main;
            main.loop = false; main.playOnAwake = false; main.duration = 1;
            main.startLifetime = 1; main.startSpeed = 0; main.startSize = 2;
            main.maxParticles = capacity; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var color = system.colorOverLifetime; color.enabled = false;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material; renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View; renderer.sortingOrder = order;
            renderer.minParticleSize = 0; renderer.maxParticleSize = 1;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return system;
        }

        public static void Validate(MvpPresenter presenter)
        {
            if (!presenter || !presenter.Presentation || !presenter.Presentation.SkillEffects)
                throw new InvalidOperationException("Saved BattlePresentation.SkillEffects reference is missing.");
            var effects = presenter.Presentation.SkillEffects;
            effects.ValidateSavedReferences();
            if (effects.transform.parent != presenter.View.SafeRoot || effects.Surface.raycastTarget || effects.ParticleCamera.enabled ||
                !effects.ParticleCamera.orthographic || effects.ParticleCamera.allowMSAA || effects.ParticleCamera.allowHDR ||
                effects.ParticleCamera.cullingMask != 1 << 2)
                throw new InvalidOperationException("Saved Dragon-v5 UI particle camera/composite contract is invalid.");
        }

        static void Dirty(UnityEngine.Object value)
        {
            EditorUtility.SetDirty(value);
            if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }
}
