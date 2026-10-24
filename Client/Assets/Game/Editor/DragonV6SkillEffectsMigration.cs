using System;
using UnityEditor;
using UnityEngine;

namespace LastBell.Editor
{
    // Upgrade two rendering settings in the existing saved v5 assets, preserving their GUIDs and layout.
    public static class DragonV6SkillEffectsMigration
    {
        public static SkillPixelEffects Configure(MvpPresenter presenter)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before migrating Dragon-v6 particles.");
            if (!presenter || !presenter.View || !presenter.View.SafeRoot || !presenter.Presentation)
                throw new InvalidOperationException("Existing presenter, SafeRoot and BattlePresentation are required.");
            DragonV5SkillEffectsMigration.CreateMissing();
            string path = DragonV5SkillEffectsMigration.PrefabPath;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var effects = root.GetComponent<SkillPixelEffects>();
                if (!effects) throw new InvalidOperationException("Saved particle prefab has no SkillPixelEffects; refusing a rebuild.");
                effects.ValidateSavedReferences();
                if (Patch(effects)) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var instance = DragonV5SkillEffectsMigration.Configure(presenter);
            Patch(instance);
            Validate(presenter);
            return instance;
        }

        static bool Patch(SkillPixelEffects effects)
        {
            bool changed = false;
            var main = effects.AttackParticles.main;
            if (main.maxParticles != SkillPixelEffects.AttackCapacity)
            {
                main.maxParticles = SkillPixelEffects.AttackCapacity;
                Dirty(effects.AttackParticles); changed = true;
            }
            foreach (var system in new[] { effects.AttackParticles, effects.ShieldParticles, effects.SealParticles, effects.SupportParticles })
            {
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer.sortMode == ParticleSystemSortMode.Distance) continue;
                renderer.sortMode = ParticleSystemSortMode.Distance;
                Dirty(renderer); changed = true;
            }
            return changed;
        }

        public static void Validate(MvpPresenter presenter)
        {
            DragonV5SkillEffectsMigration.Validate(presenter);
            var effects = presenter.Presentation.SkillEffects;
            if (effects.AttackParticles.main.maxParticles != SkillPixelEffects.AttackCapacity)
                throw new InvalidOperationException("Dragon-v6 contact particles require the saved 256-slot capacity.");
            foreach (var system in new[] { effects.AttackParticles, effects.ShieldParticles, effects.SealParticles, effects.SupportParticles })
                if (system.GetComponent<ParticleSystemRenderer>().sortMode != ParticleSystemSortMode.Distance)
                    throw new InvalidOperationException("Dragon-v6 opaque pixel highlights require saved Distance particle sorting.");
        }

        static void Dirty(UnityEngine.Object value)
        {
            EditorUtility.SetDirty(value);
            if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }
}
