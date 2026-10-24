using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Real, saved ParticleSystems composited into the existing overlay Canvas.
    // This component owns presentation only: no damage, cooldown, healing or buffs.
    public sealed class SkillPixelEffects : MonoBehaviour
    {
        public enum SupportKind { Upgrade, Blessing, Recovery }

        public RectTransform Root;
        public RawImage Surface;
        public Camera ParticleCamera;
        public ParticleSystem AttackParticles, ShieldParticles, SealParticles, SupportParticles;
        [Range(1, 4)] public int ScreenPixelsPerTexel = 2;
        public const int AttackCapacity = 256, ShieldCapacity = 32, SealCapacity = 24, SupportCapacity = 64;
        public int ActiveParticles { get; private set; }
        public RenderTexture Output => output;

        static readonly Vector3 IsolatedOrigin = new Vector3(16384, 16384, 0);
        static readonly Color32 Cream = new Color32(255, 245, 211, 255);
        static readonly Color32 Gold = new Color32(245, 188, 66, 255), Ochre = new Color32(175, 105, 35, 255);
        static readonly Color32 Blue = new Color32(113, 198, 244, 255), Navy = new Color32(46, 103, 153, 255);
        static readonly Color32 Mint = new Color32(146, 234, 180, 255), Green = new Color32(47, 135, 112, 255);
        static readonly Color32 Lilac = new Color32(184, 162, 237, 255), Violet = new Color32(110, 99, 172, 255);
        static readonly Color32 Clay = new Color32(235, 149, 101, 255), Bark = new Color32(133, 91, 61, 255);
        static readonly Color32 Ink = new Color32(35, 39, 67, 255), Flame = new Color32(250, 102, 43, 255);

        struct Dot
        {
            public Vector2 position, velocity;
            public float born, life;
            public int size;
            public Color32 color;
        }
        struct Contact
        {
            public string skillId, companionId;
            public double timeMs;
            public int variant;
            public bool absorb, projectile, started;
        }
        struct Projectile { public string companionId; public Vector2 origin; public float born, life; public bool skill; }

        readonly Dot[] attack = new Dot[AttackCapacity], support = new Dot[SupportCapacity];
        readonly Contact[] contacts = new Contact[16];
        readonly Projectile[] projectiles = new Projectile[6];
        readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[AttackCapacity];
        readonly Vector3[] corners = new Vector3[4];
        MvpPresenter owner;
        Canvas canvas;
        RenderTexture output;
        int attackCount, supportCount, contactCount, projectileCount, renderedFrame = -1;
        float effectClock, uiClock, defeatTail;
        double lastCoreClock;
        bool initialized, shieldWasActive, absorbedSinceRender;

        public void Initialize(MvpPresenter presenter)
        {
            if (!presenter || !presenter.WorldCamera || !presenter.PlayerActor || !presenter.EnemyActor)
                throw new InvalidOperationException("SkillPixelEffects requires the saved presenter, world camera and actors.");
            ValidateSavedReferences();
            owner = presenter;
            canvas = Root.GetComponentInParent<Canvas>();
            if (!canvas) throw new InvalidOperationException("SkillPixelEffects must be inside the saved UI Canvas.");
            ParticleCamera.enabled = false;
            ParticleCamera.transform.SetPositionAndRotation(IsolatedOrigin + Vector3.back * 10, Quaternion.identity);
            Surface.raycastTarget = false;
            StopSystem(AttackParticles); StopSystem(ShieldParticles); StopSystem(SealParticles); StopSystem(SupportParticles);
            initialized = true;
            Clear();
            lastCoreClock = presenter.Session == null ? 0 : presenter.Session.ClockMs;
        }

        public void ValidateSavedReferences()
        {
            if (!Root || !Surface || !ParticleCamera || !AttackParticles || !ShieldParticles || !SealParticles || !SupportParticles)
                throw new InvalidOperationException("Dragon-v5 saved particle prefab references are incomplete; run the targeted migration.");
            foreach (var system in new[] { AttackParticles, ShieldParticles, SealParticles, SupportParticles })
            {
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (!renderer || !renderer.sharedMaterial || !renderer.sharedMaterial.mainTexture)
                    throw new InvalidOperationException("Dragon-v5 particle material/point texture is missing: " + system.name);
            }
        }

        public void OnEvent(SessionEvent e, int slashVariant)
        {
            if (!initialized || e == null) return;
            if (e.Kind == SessionEventKind.PlayerDefeated) { Clear(); return; }
            if (e.Kind == SessionEventKind.StagePrepared)
            {
                attackCount = contactCount = projectileCount = 0; defeatTail = 0;
                return;
            }
            if (e.Kind == SessionEventKind.EnemyDefeated) { defeatTail = .34f; projectileCount = 0; }
            if (e.Kind == SessionEventKind.PlayerSkillStarted)
                Queue(new Contact { skillId = e.SkillId, timeMs = e.TimeMs, started = true });
            if (e.Kind == SessionEventKind.PlayerSkillContact && e.DamageMilli > 0)
                Queue(new Contact { skillId = e.SkillId, timeMs = e.TimeMs, variant = Mathf.Clamp(slashVariant, 0, 5) });
            if (e.Kind == SessionEventKind.SkillShieldAbsorbed && e.AbsorbedMilli > 0)
            {
                absorbedSinceRender = true;
                Queue(new Contact { absorb = true, timeMs = e.TimeMs });
            }
            if (e.Kind == SessionEventKind.CompanionAttackStarted)
            {
                bool special = !string.IsNullOrEmpty(e.SkillId);
                bool ranged = e.CompanionId == "tori" || e.CompanionId == "momo" || e.CompanionId == "rina" && !special;
                if (ranged || special)
                    Queue(new Contact { companionId = e.CompanionId, skillId = e.SkillId, timeMs = e.TimeMs, started = true, projectile = ranged });
            }
            if (e.Kind == SessionEventKind.CompanionDamaged && e.DamageMilli > 0)
                Queue(new Contact { companionId = e.CompanionId, skillId = e.SkillId, timeMs = e.TimeMs });
        }

        // Call only after a successful upgrade/reservation, never from a per-frame UI render.
        public void PlaySupport(SupportKind kind, RectTransform target)
        {
            if (!initialized || !target) return;
            var targetCanvas = target.GetComponentInParent<Canvas>();
            var camera = targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
            EmitSupport(kind, UiPoint(screen));
        }

        // The confirmed PlayerRecovered event can use the player's canonical body point.
        public void PlaySupportWorld(SupportKind kind, Vector3 worldPosition)
        {
            if (!initialized) return;
            EmitSupport(kind, UiPoint(owner.WorldCamera.WorldToScreenPoint(worldPosition)));
        }

        public void Render(GameSession session)
        {
            if (!initialized || session == null || !isActiveAndEnabled) return;
            EnsureTarget();
            float realDelta = renderedFrame == Time.frameCount ? 0 : Time.unscaledDeltaTime;
            renderedFrame = Time.frameCount;
            double coreDelta = Math.Max(0, session.ClockMs - lastCoreClock);
            if (session.ClockMs < lastCoreClock) { attackCount = contactCount = projectileCount = 0; effectClock = 0; }
            lastCoreClock = session.ClockMs;
            bool suspended = session.CombatSuspended;
            if (!suspended)
            {
                uiClock += realDelta;
                effectClock += (float)(coreDelta / 1000);
                // A confirmed killing hit gets a short cosmetic tail; it grants no extra combat time.
                if (defeatTail > 0)
                {
                    if (coreDelta < .001) effectClock += realDelta;
                    defeatTail = Mathf.Max(0, defeatTail - realDelta);
                }
            }
            bool combat = session.Phase == SessionPhase.Combat && session.PlayerHp > 0;
            bool tailVisible = defeatTail > 0 && session.PlayerHp > 0;
            // Reclaim expired presentation slots before simultaneous player/party contacts arrive.
            CompactExpired(attack, ref attackCount, effectClock);
            CompactExpired(support, ref supportCount, uiClock);
            for (int i = 0; i < projectileCount;)
                if (effectClock - projectiles[i].born >= projectiles[i].life) projectiles[i] = projectiles[--projectileCount]; else i++;
            for (int i = 0; i < contactCount; i++) EmitContact(contacts[i], session);
            contactCount = 0;
            if (!combat && !tailVisible) attackCount = projectileCount = 0;
            if (session.EnemyHp <= 0) projectileCount = 0;

            bool shieldActive = session.SkillShieldMilli > 0 && session.SkillShieldRemainingMs > 0 && session.PlayerHp > 0;
            if (combat && shieldWasActive && !shieldActive && !absorbedSinceRender)
                Scatter(attack, ref attackCount, ActorPoint(owner.PlayerActor), 4, Navy, Blue, effectClock, .18f, 16);
            shieldWasActive = shieldActive;
            absorbedSinceRender = false;
            int count = DrawDots(AttackParticles, attack, ref attackCount, effectClock, false);
            count = DrawProjectiles(count);
            Submit(AttackParticles, count);
            count += DrawShield(combat && shieldActive, session);
            count += DrawSeal(combat && session.EnemyHp > 0, session);
            count += DrawDots(SupportParticles, support, ref supportCount, uiClock);
            ActiveParticles = count;
            Surface.enabled = count > 0;
            if (count > 0) ParticleCamera.Render();
        }

        public void Clear()
        {
            attackCount = supportCount = contactCount = projectileCount = 0;
            effectClock = uiClock = defeatTail = 0;
            shieldWasActive = absorbedSinceRender = false;
            ActiveParticles = 0;
            if (Surface) Surface.enabled = false;
            ClearSystem(AttackParticles); ClearSystem(ShieldParticles); ClearSystem(SealParticles); ClearSystem(SupportParticles);
        }

        void Queue(Contact contact)
        {
            if (contactCount < contacts.Length) contacts[contactCount++] = contact;
        }

        void EmitContact(Contact contact, GameSession session)
        {
            Vector2 player = ActorPoint(owner.PlayerActor), enemy = ActorPoint(owner.EnemyActor);
            float born = effectClock - Mathf.Max(0, (float)((session.ClockMs - contact.timeMs) / 1000));
            if (!string.IsNullOrEmpty(contact.companionId))
            {
                bool skill = !string.IsNullOrEmpty(contact.skillId);
                if (contact.projectile)
                {
                    var actors = owner.Presentation ? owner.Presentation.Companions : null;
                    var actor = actors ? actors.FindActor(contact.companionId) : null;
                    if (actor && projectileCount < projectiles.Length && session.EnemyHp > 0)
                        projectiles[projectileCount++] = new Projectile
                        {
                            companionId = contact.companionId, origin = ActorPoint(actor), born = born,
                            life = Mathf.Max(1, session.Catalog.adventure.companionWindupMs) / 1000f, skill = skill
                        };
                }
                else if (contact.started)
                {
                    var actor = owner.Presentation && owner.Presentation.Companions ? owner.Presentation.Companions.FindActor(contact.companionId) : null;
                    if (actor)
                    {
                        CompanionPalette(contact.companionId, out Color32 primary, out Color32 dark);
                        Ring(ActorPoint(actor), 20, 8, primary, dark, born, .18f, 3);
                    }
                }
                else if (skill) CompanionSkill(contact.companionId, enemy, born);
                else
                {
                    CompanionPalette(contact.companionId, out Color32 primary, out Color32 dark);
                    Impact(enemy, primary, dark, born, .22f, 3);
                    Scatter(attack, ref attackCount, enemy, 8, primary, Cream, born, .24f, 42);
                }
                return;
            }
            if (contact.absorb)
            {
                Scatter(attack, ref attackCount, player, 14, Blue, Cream, born, .22f, 72);
                return;
            }
            if (contact.started)
            {
                Color32 color = contact.skillId == "skill_miel_star_seal" ? Lilac : contact.skillId == "skill_daren_iron_counter" ? Blue : Gold;
                Ring(player, 22, 8, color, Cream, born, .18f, 3);
                return;
            }
            switch (contact.skillId)
            {
                case "skill_luka_gale_slash":
                    // Match BattlePresentation's accepted slash direction; never roll a second direction.
                    bool up = contact.variant < 3;
                    float variation = ((contact.variant % 3) - 1) * 7;
                    Vector3 home = owner.WorldRoot.TransformPoint(ParallaxWorld.GroundPoint(500, session.Stage));
                    Vector2 from = UiPoint(owner.WorldCamera.WorldToScreenPoint(home));
                    from.y = player.y + (up ? -18 : 18);
                    Vector2 end = enemy + new Vector2(12, (up ? 24 : -24) + variation);
                    DotLine(from, end, Gold, born, .22f, 3, 24);
                    DotLine(from + Vector2.up * 12, end + Vector2.up * 12, Cream, born, .16f, 2, 20);
                    Impact(enemy, Cream, Ochre, born, .26f, 4);
                    Scatter(attack, ref attackCount, enemy, 12, Gold, Cream, born, .28f, 68);
                    break;
                case "skill_daren_iron_counter":
                    Ring(enemy, 30, 16, Blue, Cream, born, .28f, 3);
                    Impact(enemy, Cream, Navy, born, .24f, 4);
                    Scatter(attack, ref attackCount, enemy, 12, Blue, Cream, born, .28f, 58);
                    break;
                case "skill_miel_star_seal":
                    Lightning(enemy, Lilac, Violet, born, true);
                    // Persistent seal below reads real stagger. No invented slow/debuff timer.
                    break;
            }
        }

        void CompanionSkill(string id, Vector2 enemy, float born)
        {
            switch (id)
            {
                case "tori":
                    // Two converging arrow impacts; this never schedules a second damage contact.
                    DotLine(enemy + new Vector2(-76, 24), enemy + Vector2.up * 8, Gold, born, .23f, 3, 12);
                    DotLine(enemy + new Vector2(-76, -24), enemy - Vector2.up * 8, Cream, born, .23f, 3, 12);
                    Impact(enemy, Cream, Ochre, born, .26f, 4);
                    Scatter(attack, ref attackCount, enemy, 10, Gold, Cream, born, .28f, 54);
                    break;
                case "barun":
                    GroundImpact(enemy, Clay, Bark, born, false);
                    break;
                case "doto":
                    GroundImpact(enemy, Flame, Gold, born, true);
                    break;
                case "rina":
                    Lightning(enemy, Blue, Navy, born, false);
                    break;
                case "momo":
                    Ring(enemy, 29, 16, Green, Cream, born, .28f, 3);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4;
                        Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Add(attack, ref attackCount, enemy + d * 13, d * 72, born, .32f, 3, i % 2 == 0 ? Clay : Mint);
                    }
                    Impact(enemy, Cream, Green, born, .24f, 4);
                    // Recovery is emitted only by the Core's actual PlayerRecovered event.
                    break;
            }
        }

        void DotLine(Vector2 from, Vector2 to, Color32 color, float born, float life, int size, int limit)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(from, to) / 9), 2, limit);
            for (int i = 0; i < n; i++)
                Add(attack, ref attackCount, Vector2.Lerp(from, to, i / (float)(n - 1)), Vector2.zero, born, life, size, color);
        }

        void Impact(Vector2 center, Color32 bright, Color32 dark, float born, float life, int size)
        {
            // Opaque stepped cross, not a full-screen flash or damage-area overlay.
            Add(attack, ref attackCount, center, Vector2.zero, born, life, size + 2, dark);
            Add(attack, ref attackCount, center, Vector2.zero, born, life * .75f, size, bright);
            for (int i = 0; i < 4; i++)
            {
                Vector2 d = new Vector2(Mathf.Cos(i * Mathf.PI / 2), Mathf.Sin(i * Mathf.PI / 2));
                Add(attack, ref attackCount, center + d * 12, d * 24, born, life, size - 1, bright);
            }
        }

        void Ring(Vector2 center, float radius, int amount, Color32 first, Color32 second, float born, float life, int size)
        {
            for (int i = 0; i < amount; i++)
            {
                float a = i * Mathf.PI * 2 / amount;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Add(attack, ref attackCount, center + d * radius, d * 28, born, life, size, i % 3 == 0 ? second : first);
            }
        }

        void Lightning(Vector2 enemy, Color32 bright, Color32 dark, float born, bool large)
        {
            // Leave the upper HUD clear. Both bolts end at the confirmed enemy contact, never at controls.
            float top = Mathf.Min(Root.rect.yMax - 116, enemy.y + (large ? 290 : 230));
            if (top - enemy.y < 48) { Impact(enemy, Cream, dark, born, .27f, 4); return; }
            Vector2 origin = new Vector2(enemy.x - 18, top), previous = origin;
            int bends = large ? 6 : 5;
            for (int i = 1; i <= bends; i++)
            {
                float t = i / (float)bends;
                Vector2 next = Vector2.Lerp(origin, enemy, t);
                if (i < bends) next.x += (i % 2 == 0 ? -1 : 1) * (large ? 23 : 15);
                // Overlapping square samples form a solid bolt instead of two faint dotted trails.
                DotLine(previous, next, Ink, born, .27f, large ? 9 : 7, 6);
                DotLine(previous, next, i % 2 == 0 ? Cream : bright, born, .20f, large ? 7 : 5, 6);
                previous = next;
            }
            Impact(enemy, Cream, dark, born, .27f, 4);
            Scatter(attack, ref attackCount, enemy, large ? 12 : 8, bright, Cream, born, .30f, 64);
        }

        void GroundImpact(Vector2 enemy, Color32 first, Color32 second, float born, bool fire)
        {
            Vector2 feet = UiPoint(owner.WorldCamera.WorldToScreenPoint(owner.EnemyActor.CanonicalBodyPoint(0))) + Vector2.up * 12;
            DotLine(enemy + Vector2.up * 55, feet, first, born, .18f, 4, 12);
            DotLine(feet - Vector2.right * 63, feet + Vector2.right * 63, second, born, .26f, 3, 16);
            for (int i = 0; i < 10; i++)
            {
                Vector2 p = feet + new Vector2((i - 4.5f) * 12, 0);
                Add(attack, ref attackCount, p, new Vector2((i - 4.5f) * 10, fire ? 94 : 55), born, .30f, i % 3 == 0 ? 4 : 3, i % 2 == 0 ? first : second);
            }
            Impact(enemy, Cream, second, born, .24f, 4);
        }

        void EmitSupport(SupportKind kind, Vector2 center)
        {
            Color32 primary = kind == SupportKind.Upgrade ? Gold : kind == SupportKind.Blessing ? Blue : Mint;
            Color32 dark = kind == SupportKind.Upgrade ? Ochre : kind == SupportKind.Blessing ? Navy : Green;
            int count = owner.Effects && owner.Effects.ReducedShake ? 8 : 16;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = new Vector2((i * 19 % 47) - 23, (i * 11 % 19) - 9);
                Vector2 velocity = new Vector2(((i % 3) - 1) * 8, 30 + i % 4 * 8);
                Add(support, ref supportCount, center + offset, velocity, uiClock, .32f + i % 3 * .04f,
                    i % 3 == 0 ? 3 : 2, i % 3 == 0 ? Cream : i % 3 == 1 ? primary : dark);
            }
        }

        void Scatter(Dot[] dots, ref int count, Vector2 center, int amount, Color32 primary, Color32 highlight, float born, float life, float speed)
        {
            if (owner.Effects && owner.Effects.ReducedShake) amount = Mathf.Min(6, amount);
            for (int i = 0; i < amount; i++)
            {
                float angle = i * Mathf.PI * 2 / amount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Add(dots, ref count, center + direction * 5, direction * (speed + i % 3 * 9), born,
                    life - (i % 3) * .02f, i % 4 == 0 ? 3 : 2, i % 3 == 0 ? highlight : primary);
            }
        }

        static void Add(Dot[] dots, ref int count, Vector2 position, Vector2 velocity, float born, float life, int size, Color32 color)
        {
            if (count >= dots.Length) return;
            dots[count++] = new Dot { position = position, velocity = velocity, born = born, life = life, size = size, color = color };
        }

        static void CompactExpired(Dot[] dots, ref int count, float clock)
        {
            for (int i = 0; i < count;)
                if (clock - dots[i].born >= dots[i].life) dots[i] = dots[--count]; else i++;
        }

        int DrawDots(ParticleSystem system, Dot[] dots, ref int count, float clock, bool submit = true)
        {
            int rendered = 0;
            for (int i = 0; i < count;)
            {
                Dot dot = dots[i]; float age = clock - dot.born;
                if (age >= dot.life) { dots[i] = dots[--count]; continue; }
                if (age >= 0) Put(rendered++, dot.position + dot.velocity * age, dot.size, dot.color);
                i++;
            }
            if (submit) Submit(system, rendered);
            return rendered;
        }

        int DrawProjectiles(int count)
        {
            Vector2 target = ActorPoint(owner.EnemyActor);
            for (int i = 0; i < projectileCount;)
            {
                Projectile shot = projectiles[i];
                float age = effectClock - shot.born;
                if (age >= shot.life) { projectiles[i] = projectiles[--projectileCount]; continue; }
                int shots = shot.skill && shot.companionId == "tori" ? 2 : 1;
                if (age >= 0 && count + shots * 5 <= AttackCapacity)
                {
                    CompanionPalette(shot.companionId, out Color32 primary, out Color32 dark);
                    Vector2 direction = (target - shot.origin).normalized;
                    Vector2 normal = new Vector2(-direction.y, direction.x);
                    Vector2 center = Vector2.Lerp(shot.origin, target, Mathf.Clamp01(age / shot.life));
                    if (shot.companionId == "tori")
                    {
                        for (int j = 0; j < shots; j++)
                        {
                            Vector2 arrow = center + normal * (shots == 2 ? (j == 0 ? -12 : 12) : 0);
                            Put(count++, arrow, shot.skill ? 4 : 3, Cream);
                            Put(count++, arrow - direction * 7 + normal * 5, 2, primary);
                            Put(count++, arrow - direction * 7 - normal * 5, 2, primary);
                            Put(count++, arrow - direction * 13, 2, primary);
                            Put(count++, arrow - direction * 20, 2, dark);
                        }
                    }
                    else
                    {
                        Put(count++, center, shot.skill ? 5 : 3, primary);
                        Put(count++, center + normal * 6, 2, Cream);
                        Put(count++, center - normal * 6, 2, dark);
                        Put(count++, center - direction * 9, 2, dark);
                        if (shot.skill) Put(count++, center + direction * 7, 2, Green);
                    }
                }
                i++;
            }
            return count;
        }

        static void CompanionPalette(string id, out Color32 primary, out Color32 dark)
        {
            primary = id == "rina" ? Lilac : id == "momo" || id == "barun" ? Clay : Gold;
            dark = id == "rina" ? Violet : id == "momo" || id == "barun" ? Bark : Ochre;
        }

        void Submit(ParticleSystem system, int count)
        {
            system.SetParticles(particles, count);
            system.Simulate(0, false, false, false);
        }

        int DrawShield(bool visible, GameSession session)
        {
            int n = 0;
            if (visible)
            {
                Vector2 center = ActorPoint(owner.PlayerActor);
                float height = ActorHeight(owner.PlayerActor);
                float rx = Mathf.Clamp(height * .46f, 24, 54), ry = Mathf.Clamp(height * .55f, 30, 66);
                for (int i = 0; i < 20; i++)
                {
                    // An open, sparse outline preserves the body and directional warning area.
                    float angle = (15 + i * 18) * Mathf.Deg2Rad;
                    Vector2 p = center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry);
                    Color32 color = i % 5 == 0 ? Cream : i % 2 == 0 ? Blue : Navy;
                    Put(n++, p, session.SkillShieldRemainingMs < 600 ? 1 : 2, color);
                }
            }
            ShieldParticles.SetParticles(particles, n);
            ShieldParticles.Simulate(0, false, false, false);
            return n;
        }

        int DrawSeal(bool visible, GameSession session)
        {
            int n = 0;
            Vector2 center = UiPoint(owner.WorldCamera.WorldToScreenPoint(owner.EnemyActor.FaceWorldPosition)) + Vector2.up * 48;
            center.y = Mathf.Min(center.y, Root.rect.yMax - 164);
            if (visible && session.IsEnemyStunned)
            {
                // Three high-contrast seven-dot stars (21/24 slots), sampled from the Core clock.
                for (int i = 0; i < 3; i++)
                {
                    float angle = (float)(session.ClockMs / 1000) * 3 + i * Mathf.PI * 2 / 3;
                    Vector2 star = center + new Vector2(Mathf.Cos(angle) * 56, Mathf.Sin(angle) * 32);
                    Put(n++, star, 7, Ink);
                    Put(n++, star + Vector2.up * 7, 3, Gold);
                    Put(n++, star + Vector2.down * 7, 3, Gold);
                    Put(n++, star + Vector2.left * 7, 3, Gold);
                    Put(n++, star + Vector2.right * 7, 3, Gold);
                    Put(n++, star, 4, Gold);
                    Put(n++, star, 2, Cream);
                }
            }
            else if (visible && session.CurrentCharacterId == "rin" && session.Stagger > 0 && !session.IsStaggerImmune)
            {
                float threshold = Mathf.Max(1, session.Catalog.combat.staggerThreshold);
                int filled = Mathf.Clamp(Mathf.CeilToInt(session.Stagger / threshold * 8), 1, 8);
                for (int i = 0; i < filled; i++)
                {
                    float angle = i * Mathf.PI * 2 / 8;
                    Put(n++, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10, 2, i % 2 == 0 ? Mint : Green);
                }
                if (session.IsStunReady)
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = i * Mathf.PI / 2;
                        Put(n++, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 16, 2, Cream);
                    }
            }
            SealParticles.SetParticles(particles, n);
            SealParticles.Simulate(0, false, false, false);
            return n;
        }

        void Put(int index, Vector2 uiPosition, int size, Color32 color)
        {
            Rect r = Root.rect;
            float x = (uiPosition.x - r.xMin) / Mathf.Max(1, r.width) * output.width - output.width * .5f;
            float y = (uiPosition.y - r.yMin) / Mathf.Max(1, r.height) * output.height - output.height * .5f;
            // Align square edges to target texels, including odd-sized particles.
            float offsetX = (size + output.width) % 2 == 0 ? 0 : .5f;
            float offsetY = (size + output.height) % 2 == 0 ? 0 : .5f;
            // The camera looks along +Z; opaque highlights remain in front after slot compaction.
            float depth = color.Equals(Cream) ? -.1f : color.Equals(Ink) || color.Equals(Navy) || color.Equals(Violet) || color.Equals(Bark) || color.Equals(Ochre) ? .1f : 0;
            Vector3 position = IsolatedOrigin + new Vector3(Mathf.Round(x - offsetX) + offsetX, Mathf.Round(y - offsetY) + offsetY, depth);
            particles[index] = new ParticleSystem.Particle
            {
                position = position, velocity = Vector3.zero, startSize = size,
                startColor = color, startLifetime = 1, remainingLifetime = 1, rotation = 0, randomSeed = (uint)(73 + index)
            };
        }

        Vector2 ActorPoint(ActorView actor) => UiPoint(owner.WorldCamera.WorldToScreenPoint(actor.CanonicalBodyPoint(.5f)));
        float ActorHeight(ActorView actor)
        {
            Vector2 a = UiPoint(owner.WorldCamera.WorldToScreenPoint(actor.CanonicalBodyPoint(0)));
            Vector2 b = UiPoint(owner.WorldCamera.WorldToScreenPoint(actor.CanonicalBodyPoint(1)));
            return Mathf.Abs(b.y - a.y);
        }
        Vector2 UiPoint(Vector2 screen)
        {
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, uiCamera, out Vector2 point);
            return point;
        }

        void EnsureTarget()
        {
            // Canvas resize/reflow can move a child camera even when RT dimensions stay equal.
            ParticleCamera.transform.SetPositionAndRotation(IsolatedOrigin + Vector3.back * 10, Quaternion.identity);
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Root.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            int step = Mathf.Clamp(ScreenPixelsPerTexel, 1, 4);
            int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(b.x - a.x) / step));
            int height = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(b.y - a.y) / step));
            if (output && output.width == width && output.height == height && output.IsCreated()) return;
            ReleaseTarget();
            output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
            {
                name = "LastBell_SkillPixel_Output", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1, useMipMap = false, autoGenerateMips = false, hideFlags = HideFlags.DontSave
            };
            if (!output.Create()) throw new InvalidOperationException("Could not create the Dragon-v5 particle render target.");
            ParticleCamera.targetTexture = output;
            ParticleCamera.orthographicSize = height * .5f;
            ParticleCamera.aspect = width / (float)height;
            Surface.texture = output;
        }

        static void StopSystem(ParticleSystem system)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.useAutoRandomSeed = false; system.randomSeed = 73;
        }
        static void ClearSystem(ParticleSystem system) { if (system) system.Clear(true); }
        void ReleaseTarget()
        {
            if (Surface) { Surface.enabled = false; Surface.texture = null; }
            if (ParticleCamera) ParticleCamera.targetTexture = null;
            if (!output) return;
            output.Release();
            if (Application.isPlaying) Destroy(output); else DestroyImmediate(output);
            output = null;
        }
        void OnDisable() { Clear(); ReleaseTarget(); }
        void OnDestroy() { ReleaseTarget(); }
    }
}
