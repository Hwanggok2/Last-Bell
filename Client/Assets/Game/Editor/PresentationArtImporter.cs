using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace LastBell.Editor
{
    // Explicit migration only. Never replaces the source PNG or recreates art on Play/reload.
    public static class PresentationArtImporter
    {
        public const string Root = "Assets/Game/Art/Presentation";
        public const string CatalogPath = Root + "/PresentationArtCatalog.asset";
        public const string MaterialPath = Root + "/PresentationSprite.mat";
        public const string PrefabPath = Root + "/PresentationActor.prefab";
        public const string ManifestPath = Root + "/manifest.json";

        [Serializable] public sealed class Manifest
        {
            public int version;
            public TextureSpec[] textures;
            public ActorSpec[] actors;
            public string[] diagonalSlash;
        }
        [Serializable] public sealed class TextureSpec
        {
            public string path, source, sha256;
            public int width, height;
            public Vector2 pivot;
        }
        [Serializable] public sealed class ActorSpec
        {
            public string id;
            public bool facesRight;
            public float height, referenceHeightPixels;
            public ClipSpec[] clips;
        }
        [Serializable] public sealed class ClipSpec
        {
            public string motion, sourceId, sourceJob, note;
            public string[] frames, lowPoseFrames, recoverFrames;
            public int[] sourceFrames;
            public int[] frameDurationsMs;
            public float fps, referenceHeightPixels;
            public bool loop, staticFallback, legacyStun;
            public int contactFrame = -1;
            public Vector2[] faceAnchors;
            public Vector2 effectOrigin;
        }

        [MenuItem("Last Bell/Art/Presentation/Create Missing")]
        public static void CreateMissing()
        {
            if (!AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath)) MigrateExisting();
        }

        public static void MigrateAndBuild()
        {
            MigrateExisting();
            MvpBuild.Windows();
        }

        [MenuItem("Last Bell/Art/Presentation/Migrate Existing")]
        public static void MigrateExisting()
        {
            var manifest = ReadAndValidateSources();
            var legacy = AssetDatabase.LoadAssetAtPath<ArtCatalog>(MvpArtImporter.CatalogPath);
            if (!legacy) throw new InvalidDataException("Legacy catalog is required for explicitly documented single-pose Stun fallbacks.");
            foreach (var actor in manifest.actors)
                if (actor.clips.Any(c => c.legacyStun) && (legacy.Find(actor.id)?.stun?.Length ?? 0) == 0)
                    throw new InvalidDataException("Missing legacy Stun fallback: " + actor.id);
            // All input validation happens before touching the last good serialized catalog.
            foreach (var texture in manifest.textures) ImportTexture(texture);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/SpriteWhiteFlash.shader");
            if (!shader) throw new InvalidDataException("Presentation Sprite shader was not imported.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
            material.shader = shader; material.SetColor("_Color", Color.white);
            material.SetFloat("PixelSnap", 1); material.SetFloat("_Flash", 0); material.SetFloat("_Dissolve", 0);
            material.EnableKeyword("PIXELSNAP_ON"); EditorUtility.SetDirty(material);

            var actors = manifest.actors.Select(spec => BuildActor(spec, legacy, material)).ToArray();
            var slash = manifest.diagonalSlash.Select(LoadSprite).ToArray();
            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<ArtCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.actors = actors; catalog.actorMaterial = material; catalog.diagonalSlash = slash;
            catalog.gateBackground = legacy.gateBackground; catalog.uiFont = legacy.uiFont;
            EditorUtility.SetDirty(catalog);
            SaveActorPrefab(catalog);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log($"LAST_BELL_PRESENTATION_ART_IMPORTED: {manifest.textures.Length} preserved source PNGs; {actors.Length} actors; approved hero run retained.");
        }

        [MenuItem("Last Bell/Art/Presentation/Validate")]
        public static void Validate()
        {
            var manifest = ReadAndValidateSources();
            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath);
            if (!catalog || catalog.actors.Length != manifest.actors.Length || !catalog.actorMaterial || catalog.diagonalSlash.Length != 10)
                throw new InvalidDataException("Presentation catalog/material/slash references are incomplete.");
            foreach (var texture in manifest.textures)
            {
                var sprite = LoadSprite(texture.path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texture.path);
                if (sprite.rect.width != texture.width || sprite.rect.height != texture.height || sprite.pixelsPerUnit != 100 ||
                    Vector2.Distance(sprite.pivot, new Vector2(texture.pivot.x, texture.height - texture.pivot.y)) > .01f ||
                    importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed)
                    throw new InvalidDataException("Pixel geometry/import settings changed: " + texture.path);
            }
            foreach (var spec in manifest.actors)
            {
                var actor = catalog.Find(spec.id);
                if (actor == null || actor.height != spec.height || actor.referenceHeightPixels != spec.referenceHeightPixels || actor.material != catalog.actorMaterial)
                    throw new InvalidDataException("Actor reference geometry mismatch: " + spec.id);
                foreach (var selected in spec.clips)
                {
                    var clip = actor.FindClip((ActorMotion)Enum.Parse(typeof(ActorMotion), selected.motion));
                    if (clip == null || clip.frames.Length == 0 || clip.frames.Any(s => !s) || clip.sourceId != selected.sourceId || clip.staticFallback != selected.staticFallback)
                        throw new InvalidDataException("Clip missing/fallback mismatch: " + spec.id + "/" + selected.motion);
                    if (!selected.legacyStun && (!clip.sourceFrames.SequenceEqual(selected.sourceFrames) || clip.frames.Length != selected.frames.Length))
                        throw new InvalidDataException("Selected source sequence mismatch: " + selected.sourceId);
                    if (!clip.frameDurationsMs.SequenceEqual(selected.frameDurationsMs ?? Array.Empty<int>()))
                        throw new InvalidDataException("Authored frame holds mismatch: " + selected.sourceId);
                }
            }
            var run = catalog.Find("hero_on").FindClip(ActorMotion.Run);
            if (run.sourceId != "hero-approved-dash-20261021-02" || run.frames.Length != 8 || !run.loop ||
                !run.frameDurationsMs.SequenceEqual(new[] { 40, 40, 60, 60, 40, 40, 60, 60 }) || !Mathf.Approximately(run.Duration, .4f))
                throw new InvalidDataException("Run must preserve the approved eight poses and authored 400ms rhythm.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab || !prefab.GetComponent<ActorView>() || prefab.GetComponent<ActorView>().DefaultCatalog != catalog)
                throw new InvalidDataException("Saved presentation actor prefab is not linked to its catalog.");
        }

        static Manifest ReadAndValidateSources()
        {
            if (!File.Exists(ManifestPath)) throw new FileNotFoundException("Selected-art manifest missing", ManifestPath);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.version != 1 || manifest.textures == null || manifest.actors == null)
                throw new InvalidDataException("Invalid presentation art manifest.");
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var texture in manifest.textures)
            {
                if (!texture.path.StartsWith(Root + "/Clips/", StringComparison.Ordinal) || texture.path.Contains("..") || !paths.Add(texture.path))
                    throw new InvalidDataException("Unexpected or duplicate art destination: " + texture.path);
                if (!File.Exists(texture.path)) throw new FileNotFoundException("Selected PNG is missing", texture.path);
                var bytes = File.ReadAllBytes(texture.path);
                using (var hash = SHA256.Create())
                    if (BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() != texture.sha256)
                        throw new InvalidDataException("Selected PNG differs from the recorded source: " + texture.path);
                if (bytes.Length < 24 || PngInt(bytes, 16) != texture.width || PngInt(bytes, 20) != texture.height)
                    throw new InvalidDataException("Source canvas size changed: " + texture.path);
            }
            foreach (var actor in manifest.actors)
            {
                if (actor.height <= 0 || actor.referenceHeightPixels <= 0) throw new InvalidDataException("Invalid actor geometry: " + actor.id);
                var motions = new HashSet<string>();
                foreach (var clip in actor.clips)
                {
                    if (!Enum.TryParse<ActorMotion>(clip.motion, out _) || !motions.Add(clip.motion)) throw new InvalidDataException("Invalid/duplicate motion: " + clip.motion);
                    if (clip.legacyStun) continue;
                    if (clip.frames == null || clip.frames.Length == 0 || clip.sourceFrames == null || clip.frames.Length != clip.sourceFrames.Length || clip.fps <= 0)
                        throw new InvalidDataException("Empty/invalid selected clip: " + clip.sourceId);
                    if (clip.frameDurationsMs != null && clip.frameDurationsMs.Length > 0 &&
                        (clip.frameDurationsMs.Length != clip.frames.Length || clip.frameDurationsMs.Any(ms => ms <= 0)))
                        throw new InvalidDataException("Each authored frame requires one positive hold time: " + clip.sourceId);
                    foreach (string path in clip.frames.Concat(clip.lowPoseFrames ?? Array.Empty<string>()).Concat(clip.recoverFrames ?? Array.Empty<string>()))
                        if (!paths.Contains(path)) throw new InvalidDataException("Unlisted clip image: " + path);
                    if (clip.contactFrame < -1 || clip.contactFrame >= clip.frames.Length) throw new InvalidDataException("Contact frame outside sequence: " + clip.sourceId);
                }
            }
            foreach (string path in manifest.diagonalSlash)
                if (!paths.Contains(path)) throw new InvalidDataException("Unlisted slash image: " + path);
            return manifest;
        }

        static int PngInt(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];

        static void ImportTexture(TextureSpec spec)
        {
            AssetDatabase.ImportAsset(spec.path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(spec.path);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            var pivot = new Vector2(spec.pivot.x / spec.width, 1f - spec.pivot.y / spec.height);
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                importer.filterMode == FilterMode.Point && importer.wrapMode == TextureWrapMode.Clamp &&
                !importer.mipmapEnabled && importer.alphaIsTransparency && importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.npotScale == TextureImporterNPOTScale.None && importer.spritePixelsPerUnit == 100 && !importer.isReadable &&
                settings.spriteAlignment == (int)SpriteAlignment.Custom && Vector2.Distance(settings.spritePivot, pivot) < .00001f &&
                settings.spriteMeshType == SpriteMeshType.FullRect && !settings.spriteGenerateFallbackPhysicsShape) return;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100; importer.isReadable = false;
            // Refresh after setting the type/filter fields: stale Default settings would
            // overwrite those fields and prevent a newly added PNG from becoming a Sprite.
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite) throw new InvalidDataException("Sprite reference cannot be loaded: " + path);
            return sprite;
        }

        static ActorSprites BuildActor(ActorSpec spec, ArtCatalog legacy, Material material)
        {
            var actor = new ActorSprites { id = spec.id, facesRight = spec.facesRight, height = spec.height,
                referenceHeightPixels = spec.referenceHeightPixels, material = material };
            actor.clips = spec.clips.Select(selected =>
            {
                var old = legacy.Find(spec.id);
                return new ActorClip {
                    motion = (ActorMotion)Enum.Parse(typeof(ActorMotion), selected.motion), sourceId = selected.sourceId,
                    frames = selected.legacyStun ? old.stun : selected.frames.Select(LoadSprite).ToArray(),
                    sourceFrames = selected.sourceFrames ?? Array.Empty<int>(), fps = selected.fps, loop = selected.loop,
                    frameDurationsMs = selected.frameDurationsMs ?? Array.Empty<int>(),
                    contactFrame = selected.contactFrame, staticFallback = selected.staticFallback,
                    referenceHeightPixels = selected.legacyStun ? old.idle[0].rect.height : selected.referenceHeightPixels,
                    lowPoseFrames = (selected.lowPoseFrames ?? Array.Empty<string>()).Select(LoadSprite).ToArray(),
                    recoverFrames = (selected.recoverFrames ?? Array.Empty<string>()).Select(LoadSprite).ToArray(),
                    faceAnchors = selected.faceAnchors ?? Array.Empty<Vector2>(), effectOrigin = selected.effectOrigin
                };
            }).ToArray();
            actor.idle = actor.FindClip(ActorMotion.Idle).frames;
            actor.attack = (actor.FindClip(ActorMotion.Attack) ?? actor.FindClip(ActorMotion.AttackUp)).frames;
            actor.stun = actor.FindClip(ActorMotion.Stun)?.frames ?? Array.Empty<Sprite>();
            actor.dodgeUp = actor.FindClip(ActorMotion.DodgeUp)?.frames ?? Array.Empty<Sprite>();
            actor.dodgeDown = actor.FindClip(ActorMotion.DodgeDown)?.frames ?? Array.Empty<Sprite>();
            actor.dodgeBack = actor.FindClip(ActorMotion.DodgeBack)?.frames ?? Array.Empty<Sprite>();
            actor.dodgeForward = actor.FindClip(ActorMotion.DodgeForward)?.frames ?? Array.Empty<Sprite>();
            return actor;
        }

        static void SaveActorPrefab(ArtCatalog catalog)
        {
            bool existing = File.Exists(PrefabPath);
            var instance = existing ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("PresentationActor");
            try
            {
                var actor = instance.GetComponent<ActorView>() ?? instance.AddComponent<ActorView>();
                if (!actor.Body)
                {
                    var body = new GameObject("Body"); body.transform.SetParent(instance.transform, false);
                    actor.Body = body.AddComponent<SpriteRenderer>(); actor.Body.sortingOrder = 2;
                }
                actor.DefaultCatalog = catalog; actor.DefaultActorId = "hero_on"; actor.DefaultFacesRight = true;
                actor.ApplyArt(catalog.Find("hero_on"), true);
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                if (existing) PrefabUtility.UnloadPrefabContents(instance); else UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
