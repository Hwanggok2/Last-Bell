using System;
using UnityEditor;
using UnityEngine;

namespace LastBell.Editor
{
    // Patch only the existing scene's missing party node/actor instances.
    // PresentationActor.prefab, the existing world and the scene save belong to the caller.
    public static class DragonV5PartyMigration
    {
        static readonly string[] DefaultIds = { "companion_tori", "companion_barun", "companion_doto" };
        static readonly string[] AllIds = { "companion_tori", "companion_barun", "companion_doto", "companion_rina", "companion_momo" };
        static readonly float[] HomeX = { 380, 288, 196 };

        public static CompanionBattleView Configure(MvpPresenter presenter)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before configuring saved companion actors.");
            RequirePresenter(presenter);
            ValidateArt(presenter.Art);
            var actorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PresentationArtImporter.PrefabPath);
            if (!actorPrefab || !actorPrefab.GetComponent<ActorView>() || !actorPrefab.GetComponent<ActorView>().Body)
                throw new InvalidOperationException("The existing saved PresentationActor.prefab is missing or incomplete; party migration does not rebuild it.");

            var root = presenter.WorldRoot.Find("CompanionParty");
            if (!root)
            {
                if (presenter.Presentation.Companions)
                    throw new InvalidOperationException("A companion view already exists outside CompanionParty; preserve and repair its saved reference instead of creating a duplicate.");
                root = new GameObject("CompanionParty").transform;
                root.SetParent(presenter.WorldRoot, false);
            }
            RequireIdentity(root);
            var party = root.GetComponent<CompanionBattleView>() ?? root.gameObject.AddComponent<CompanionBattleView>();
            if (presenter.Presentation.Companions && presenter.Presentation.Companions != party)
                throw new InvalidOperationException("BattlePresentation references a different companion view; no actor hierarchy was replaced.");
            var slots = new ActorView[3];
            for (int i = 0; i < slots.Length; i++)
            {
                ActorView actor = party.Actors != null && i < party.Actors.Length ? party.Actors[i] : null;
                bool created = false;
                if (!actor)
                {
                    var existing = root.Find("CompanionSlot" + (i + 1));
                    if (existing)
                    {
                        actor = existing.GetComponent<ActorView>();
                        if (!actor) throw new InvalidOperationException("Existing companion slot has no ActorView: " + existing.name);
                    }
                    else
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(actorPrefab, root);
                        instance.name = "CompanionSlot" + (i + 1);
                        actor = instance.GetComponent<ActorView>();
                        created = true;
                    }
                }
                if (actor.transform.parent != root || !actor.Body)
                    throw new InvalidOperationException("Companion actor must remain under CompanionParty with its saved body renderer.");
                if (Array.IndexOf(slots, actor) >= 0)
                    throw new InvalidOperationException("Companion actor is assigned to more than one slot.");

                actor.DefaultCatalog = presenter.Art;
                actor.DefaultActorId = DefaultIds[i];
                actor.DefaultFacesRight = true;
                if (created)
                {
                    actor.transform.localPosition = ParallaxWorld.GroundPoint(HomeX[i], 1);
                    actor.transform.localRotation = Quaternion.identity;
                    actor.transform.localScale = Vector3.one;
                    // Runtime Sample decides visibility from the actual saved run roster.
                    actor.gameObject.SetActive(false);
                }
                actor.ApplyArt(presenter.Art.Find(DefaultIds[i]), true);
                actor.Body.sortingLayerID = presenter.PlayerActor.Body.sortingLayerID;
                actor.Body.sortingOrder = Math.Min(presenter.PlayerActor.Body.sortingOrder, presenter.EnemyActor.Body.sortingOrder) - 1;
                foreach (var value in new UnityEngine.Object[] { actor, actor.gameObject, actor.transform, actor.Body, actor.Body.transform }) Dirty(value);
                slots[i] = actor;
            }
            party.Actors = slots;
            presenter.Presentation.Companions = party;
            Dirty(root); Dirty(party); Dirty(presenter.Presentation);
            Validate(presenter);
            return party;
        }

        public static void Validate(MvpPresenter presenter)
        {
            RequirePresenter(presenter);
            ValidateArt(presenter.Art);
            var party = presenter.Presentation.Companions;
            if (!party || party.transform.parent != presenter.WorldRoot || party.name != "CompanionParty" ||
                party.Actors == null || party.Actors.Length != 3)
                throw new InvalidOperationException("Saved CompanionParty must have exactly three actor slots under WorldRoot.");
            RequireIdentity(party.transform);
            for (int i = 0; i < 3; i++)
            {
                var actor = party.Actors[i];
                if (!actor || !actor.Body || actor.transform.parent != party.transform || actor.DefaultCatalog != presenter.Art ||
                    actor.DefaultActorId != DefaultIds[i] || !actor.DefaultFacesRight ||
                    actor.Body.sortingLayerID != presenter.PlayerActor.Body.sortingLayerID ||
                    actor.Body.sortingOrder >= Math.Min(presenter.PlayerActor.Body.sortingOrder, presenter.EnemyActor.Body.sortingOrder))
                    throw new InvalidOperationException("Invalid saved companion actor references or sorting in slot " + (i + 1));
                for (int j = 0; j < i; j++)
                    if (party.Actors[j] == actor) throw new InvalidOperationException("Duplicate saved companion actor slot.");
            }
        }

        static void ValidateArt(ArtCatalog catalog)
        {
            foreach (string id in AllIds)
            {
                var actor = catalog.Find(id);
                var idle = actor?.FindClip(ActorMotion.Idle);
                var attack = actor?.FindClip(ActorMotion.Attack);
                var run = actor?.FindClip(ActorMotion.Run);
                if (idle == null || idle.frames == null || idle.frames.Length < 4 ||
                    attack == null || attack.frames == null || attack.frames.Length < 6)
                    throw new InvalidOperationException(id + ": import the authored Idle >=4 and Attack >=6 frames before party migration.");
                if (run == null || run.frames == null || run.frames.Length < 8 || Mathf.Abs(run.Duration - .5f) > .0001f)
                    throw new InvalidOperationException(id + ": import the authored eight-frame 500ms Run clip before party migration.");
                foreach (var sprite in run.frames)
                    if (!sprite) throw new InvalidOperationException(id + ": a Run frame reference is missing.");
                foreach (var sprite in idle.frames)
                    if (!sprite) throw new InvalidOperationException(id + ": an Idle frame reference is missing.");
                foreach (var sprite in attack.frames)
                    if (!sprite) throw new InvalidOperationException(id + ": an Attack frame reference is missing.");
                if (Mathf.Abs(attack.ContactTime - .18f) > .0001f || Mathf.Abs(attack.Duration - .6f) > .0001f)
                    throw new InvalidOperationException(id + ": expected saved attack contact 180ms and total duration 600ms.");
            }
        }

        static void RequirePresenter(MvpPresenter presenter)
        {
            if (!presenter || !presenter.WorldRoot || !presenter.Presentation || !presenter.Art ||
                !presenter.PlayerActor || !presenter.PlayerActor.Body || !presenter.EnemyActor || !presenter.EnemyActor.Body)
                throw new InvalidOperationException("Existing presenter, world, art catalog and both actors are required for the targeted party migration.");
            if (presenter.PlayerActor.Body.sortingLayerID != presenter.EnemyActor.Body.sortingLayerID)
                throw new InvalidOperationException("Player/enemy sorting layers differ; explicitly choose the party sorting layer before migration.");
        }

        static void RequireIdentity(Transform root)
        {
            if (root.localPosition.sqrMagnitude > .000001f || Quaternion.Angle(root.localRotation, Quaternion.identity) > .001f ||
                (root.localScale - Vector3.one).sqrMagnitude > .000001f)
                throw new InvalidOperationException("CompanionParty root must be an identity transform so the shared ground/plan positions remain correct. Authored offsets were not overwritten.");
        }

        static void Dirty(UnityEngine.Object value)
        {
            EditorUtility.SetDirty(value);
            if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }
}
