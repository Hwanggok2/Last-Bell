using UnityEngine;

namespace LastBell
{
    // Renders stored frames only. The battle director owns root movement and hit timing.
    public sealed class ActorView : MonoBehaviour
    {
        public SpriteRenderer Body;
        public Sprite[] IdleFrames, AttackFrames, HurtFrames, StunFrames, DodgeFrames;
        public Vector3 SpriteOffset = Vector3.zero;
        public ArtCatalog DefaultCatalog;
        public string DefaultActorId = "hero_on";
        public bool DefaultFacesRight = true;

        public ActorMotion CurrentMotion { get; private set; }
        public int CurrentFrame { get; private set; }
        public float HitFlashAmount { get; private set; }
        public float DissolveAmount { get; private set; }
        public bool PlaybackPaused => paused;
        public string ArtId => art?.id;
        // Imported sprite origin is its authored feet pivot. Do not normalize each
        // frame's opaque bounds: airborne poses and squash must retain their motion.
        public Vector3 FeetWorldPosition=>Body?Body.transform.TransformPoint(Vector3.zero):transform.position;
        // Canonical opaque body height excludes transparent padding in the 128px cell.
        public Vector3 CanonicalBodyPoint(float heightFraction)=>transform.TransformPoint(SpriteOffset+Vector3.up*(art==null?1:art.height)*heightFraction);
        public bool IsHitPlaying => transient && transientMotion == ActorMotion.Hit;
        public ActorClip GetClip(ActorMotion motion) => ResolveClip(motion);
        // Authored contact point is in source pixels, measured from the top-left.
        // Use the canonical body scale so padding never changes the approach distance.
        public float ContactOffsetX(ActorClip clip)
        {
            if (art == null || clip == null || clip.contactFrame < 0 || clip.contactFrame >= clip.frames.Length) return 0;
            var sprite = clip.frames[clip.contactFrame];
            float reference = clip.referenceHeightPixels > 0 ? clip.referenceHeightPixels : art.referenceHeightPixels;
            float offset = (clip.effectOrigin.x - sprite.pivot.x) * art.height / Mathf.Max(1, reference);
            return SpriteOffset.x + (Body.flipX ? -offset : offset);
        }

        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        MaterialPropertyBlock properties;
        ActorSprites art;
        ActorClip displayedClip;
        ActorMotion transientMotion;
        float idleAge, poseAge, poseDuration, poseContact = -1, flashAge = 1;
        int sampledFrame = -1;
        bool transient, paused, stunned, running, facingRight = true;

        void Awake()
        {
            if (DefaultCatalog) ApplyArt(DefaultCatalog.Find(DefaultActorId), DefaultFacesRight);
        }

        public void ApplyArt(ActorSprites value, bool faceRight)
        {
            if (value == null || (value == art && faceRight == facingRight)) return;
            art = value; facingRight = faceRight;
            IdleFrames = value.idle; AttackFrames = value.attack; StunFrames = value.stun;
            var hit = value.FindClip(ActorMotion.Hit);
            HurtFrames = hit == null ? null : hit.frames;
            SpriteOffset = Vector3.zero;
            if (Body)
            {
                Body.flipX = value.facesRight != faceRight;
                if (value.material) Body.sharedMaterial = value.material;
            }
            ResetVisuals();
        }

        public void PlayAttack() => PlayAttack(true);
        public void PlayAttack(bool rising, float duration = .18f, float contactTime = .08f)
            => Play(rising ? ActorMotion.AttackUp : ActorMotion.AttackDown, duration, contactTime);
        public void PlayHit()
        {
            Play(ActorMotion.Hit, .18f); FlashHit();
        }
        // Enemy damage changes only the shader, preserving its current attack pose.
        public void FlashHit() { flashAge = 0; ApplyMaterialProperties(); }
        public void PlayDodge(int direction) => Play(DodgeMotion(direction), .28f);
        public void SetStunned(bool value) { stunned = value; }
        public void SetRunning(bool value) { running = value; }
        public void SetPlaybackPaused(bool value) { paused = value; }

        void Play(ActorMotion motion, float duration, float contactTime = -1)
        {
            transient = true; transientMotion = motion; poseAge = 0;
            poseDuration = Mathf.Max(.001f, duration); poseContact = contactTime;
            Render(motion, 0, poseDuration, poseContact);
        }

        public void SampleMotion(ActorMotion motion, float elapsedSeconds, float duration = 0, float contactTime = -1)
        {
            sampledFrame = Time.frameCount;
            Render(motion, elapsedSeconds, duration, contactTime);
        }

        public void SampleDodgeImpact(int direction, float elapsedSeconds)
        {
            if (elapsedSeconds >= .22f) { SampleMotion(ActorMotion.Idle, idleAge); return; }
            var motion = DodgeMotion(direction);
            var clip = ResolveClip(motion);
            sampledFrame = Time.frameCount;
            if (direction == 2 && clip != null && clip.lowPoseFrames.Length > 0)
            {
                var frames = elapsedSeconds < .14f ? clip.lowPoseFrames : clip.recoverFrames;
                if (frames.Length > 0)
                {
                    float elapsed = elapsedSeconds < .14f ? elapsedSeconds : elapsedSeconds - .14f;
                    float duration = elapsedSeconds < .14f ? .14f : .08f;
                    int index = Mathf.Clamp(Mathf.FloorToInt(elapsed / duration * frames.Length), 0, frames.Length - 1);
                    CurrentMotion = motion; CurrentFrame = index; displayedClip = clip;
                    SetSprite(frames[index], clip); return;
                }
            }
            Render(motion, Mathf.Max(0, elapsedSeconds) + .06f, .28f, -1);
        }

        public void SampleClipFrame(ActorMotion motion,int index)
        {
            var clip=ResolveClip(motion);if(clip==null||clip.frames.Length==0)return;
            sampledFrame=Time.frameCount;CurrentMotion=motion;displayedClip=clip;
            CurrentFrame=Mathf.Clamp(index,0,clip.frames.Length-1);SetSprite(clip.frames[CurrentFrame],clip);
        }
        public void SampleSourceFrame(ActorMotion motion,int sourceFrame)
        {
            var clip=ResolveClip(motion);if(clip==null)return;
            SampleClipFrame(motion,System.Array.IndexOf(clip.sourceFrames,sourceFrame));
        }

        public void ClearMotionOverride() { sampledFrame = -1; RenderAutomatic(); }
        public void EndSample() => ClearMotionOverride();
        public void SetDissolve(float value)
        {
            DissolveAmount = Mathf.Clamp01(value); ApplyMaterialProperties();
        }
        public void ResetVisuals()
        {
            idleAge = poseAge = 0; flashAge = 1; transient = stunned = running = false;
            paused = false; sampledFrame = -1; DissolveAmount = 0;
            Render(ActorMotion.Idle, 0, 0, -1); ApplyMaterialProperties();
        }

        void LateUpdate()
        {
            if (!Body || paused) return;
            float dt = Time.unscaledDeltaTime;
            idleAge += dt; flashAge += dt;
            if (transient)
            {
                poseAge += dt;
                if (poseAge >= poseDuration && transientMotion != ActorMotion.Death) transient = false;
            }
            // A sample lasts one frame. Missing the next sample cannot leave a permanent pose.
            if (sampledFrame != Time.frameCount) RenderAutomatic();
            ApplyMaterialProperties();
        }

        void RenderAutomatic()
        {
            if (transient) Render(transientMotion, poseAge, poseDuration, poseContact);
            else Render(stunned ? ActorMotion.Stun : running ? ActorMotion.Run : ActorMotion.Idle, idleAge, 0, -1);
        }

        ActorClip ResolveClip(ActorMotion motion)
        {
            if (art == null) return null;
            var clip = art.FindClip(motion);
            if (clip != null && clip.frames.Length > 0) return clip;
            if (motion == ActorMotion.AttackUp || motion == ActorMotion.AttackDown || motion == ActorMotion.Roar ||
                motion == ActorMotion.AttackLow || motion == ActorMotion.AttackHigh || motion == ActorMotion.AttackThrust || motion == ActorMotion.AttackRear)
                return art.FindClip(ActorMotion.Attack);
            return null;
        }

        void Render(ActorMotion motion, float elapsed, float duration, float contactTime)
        {
            CurrentMotion = motion; displayedClip = ResolveClip(motion);
            var frames = displayedClip != null ? displayedClip.frames : LegacyFrames(motion);
            if (frames == null || frames.Length == 0) { frames = IdleFrames; displayedClip = null; }
            if (frames == null || frames.Length == 0) return;
            CurrentFrame = displayedClip != null ? displayedClip.FrameIndex(elapsed, duration, contactTime)
                : Mathf.Min(frames.Length - 1, Mathf.FloorToInt(Mathf.Max(0, elapsed) * 8) % frames.Length);
            SetSprite(frames[CurrentFrame], displayedClip);
        }

        Sprite[] LegacyFrames(ActorMotion motion)
        {
            switch (motion)
            {
                case ActorMotion.Attack: case ActorMotion.AttackUp: case ActorMotion.AttackDown: case ActorMotion.Roar: return AttackFrames;
                case ActorMotion.Hit: return HurtFrames;
                case ActorMotion.Stun: return StunFrames;
                case ActorMotion.DodgeUp: return art == null ? DodgeFrames : art.dodgeUp;
                case ActorMotion.DodgeBack: return art == null ? DodgeFrames : art.dodgeBack;
                case ActorMotion.DodgeDown: return art == null ? DodgeFrames : art.dodgeDown;
                case ActorMotion.DodgeForward: return art == null ? DodgeFrames : art.dodgeForward;
                default: return IdleFrames;
            }
        }

        void SetSprite(Sprite sprite, ActorClip clip)
        {
            if (!Body || !sprite) return;
            Body.sprite = sprite;
            float reference = clip != null && clip.referenceHeightPixels > 0 ? clip.referenceHeightPixels
                : art == null ? 0 : art.referenceHeightPixels;
            float scale = art == null ? 1 : reference > 0 ? art.height * sprite.pixelsPerUnit / reference
                : art.height / Mathf.Max(.01f, IdleFrames != null && IdleFrames.Length > 0 ? IdleFrames[0].bounds.size.y : sprite.bounds.size.y);
            Body.transform.localPosition = SpriteOffset;
            Body.transform.localRotation = Quaternion.identity;
            Body.transform.localScale = new Vector3(scale, scale, 1);
            Body.color = Color.white;
        }

        public Vector3 SourcePixelToWorld(Vector2 topLeftPixel)
        {
            if (!Body || !Body.sprite) return transform.position;
            var sprite = Body.sprite;
            float x = (topLeftPixel.x - sprite.pivot.x) / sprite.pixelsPerUnit;
            float y = (sprite.rect.height - topLeftPixel.y - sprite.pivot.y) / sprite.pixelsPerUnit;
            return Body.transform.TransformPoint(new Vector3(Body.flipX ? -x : x, y));
        }

        public Vector3 FaceWorldPosition
        {
            get
            {
                if (displayedClip != null && displayedClip.faceAnchors.Length > CurrentFrame)
                    return SourcePixelToWorld(displayedClip.faceAnchors[CurrentFrame]);
                return Body ? Body.bounds.center + Vector3.up * Body.bounds.extents.y * .6f : transform.position;
            }
        }

        void ApplyMaterialProperties()
        {
            HitFlashAmount = flashAge <= .045f ? 1 : Mathf.Clamp01(1 - (flashAge - .045f) / .075f);
            if (!Body) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            Body.GetPropertyBlock(properties);
            properties.SetFloat(FlashId, HitFlashAmount); properties.SetFloat(DissolveId, DissolveAmount);
            Body.SetPropertyBlock(properties);
        }

        static ActorMotion DodgeMotion(int direction) => direction == 0 ? ActorMotion.DodgeUp
            : direction == 1 ? ActorMotion.DodgeBack : direction == 2 ? ActorMotion.DodgeDown : ActorMotion.DodgeForward;
    }
}
