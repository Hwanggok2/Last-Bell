using System;
using UnityEngine;

namespace LastBell
{
    [CreateAssetMenu(menuName = "Last Bell/Art Catalog")]
    public sealed class ArtCatalog : ScriptableObject
    {
        public ActorSprites[] actors = Array.Empty<ActorSprites>();
        public Sprite gateBackground;
        public Font uiFont;
        public Material actorMaterial;
        public Sprite[] diagonalSlash = Array.Empty<Sprite>();

        public ActorSprites Find(string id)
        {
            foreach (var actor in actors)
                if (actor.id == id) return actor;
            return null;
        }
    }

    [Serializable]
    public sealed class ActorSprites
    {
        public string id;
        public Sprite[] idle = Array.Empty<Sprite>();
        public Sprite[] attack = Array.Empty<Sprite>();
        public Sprite[] stun = Array.Empty<Sprite>();
        public Sprite[] dodgeUp = Array.Empty<Sprite>();
        public Sprite[] dodgeDown = Array.Empty<Sprite>();
        public Sprite[] dodgeBack = Array.Empty<Sprite>();
        public Sprite[] dodgeForward = Array.Empty<Sprite>();
        public bool facesRight;
        public float height = 1f;
        // Measured opaque height of the canonical pose, not the padded 128px cell.
        public float referenceHeightPixels;
        public Material material;
        public ActorClip[] clips = Array.Empty<ActorClip>();

        public ActorClip FindClip(ActorMotion motion)
        {
            foreach (var clip in clips)
                if (clip != null && clip.motion == motion) return clip;
            return null;
        }
    }

    public enum ActorMotion
    {
        Idle, Run, Attack, AttackUp, AttackDown, Hit, Stun,
        DodgeUp, DodgeBack, DodgeDown, DodgeForward, Return, Death, Roar,
        AttackLow, AttackHigh, AttackThrust, AttackRear
    }

    [Serializable]
    public sealed class ActorClip
    {
        public ActorMotion motion;
        public string sourceId;
        public Sprite[] frames = Array.Empty<Sprite>();
        public int[] sourceFrames = Array.Empty<int>();
        public float fps = 12f;
        // Optional authored hold times. Empty keeps the existing uniform-fps playback.
        public int[] frameDurationsMs = Array.Empty<int>();
        public bool loop;
        // Index in the selected frames array. Presentation never decides damage.
        public int contactFrame = -1;
        // Only legacy atlas fallbacks override the actor's common reference height.
        public float referenceHeightPixels;
        public Sprite[] lowPoseFrames = Array.Empty<Sprite>();
        public Sprite[] recoverFrames = Array.Empty<Sprite>();
        public Vector2[] faceAnchors = Array.Empty<Vector2>();
        public Vector2 effectOrigin;
        public bool staticFallback;

        int AuthoredDurationMs
        {
            get
            {
                if (frameDurationsMs == null || frameDurationsMs.Length != frames.Length) return 0;
                int total = 0;
                foreach (int ms in frameDurationsMs) { if (ms <= 0) return 0; total += ms; }
                return total;
            }
        }
        public float Duration => AuthoredDurationMs > 0 ? AuthoredDurationMs / 1000f : frames.Length / Mathf.Max(1f, fps);
        public bool HasAuthoredTiming => AuthoredDurationMs > 0;
        public float ContactTime
        {
            get
            {
                if (contactFrame < 0 || contactFrame >= frames.Length) return -1;
                if (!HasAuthoredTiming) return contactFrame / Mathf.Max(1f, fps);
                int ms = 0;
                for (int i = 0; i < contactFrame; i++) ms += frameDurationsMs[i];
                return ms / 1000f;
            }
        }

        public int FrameIndex(float elapsed, float duration = 0, float contactTime = -1)
        {
            if (frames.Length < 2) return 0;
            duration = duration > 0 ? duration : Duration;
            elapsed = Mathf.Max(0, elapsed);
            int authoredMs = AuthoredDurationMs;
            // Keep the existing attack/contact remapping; run loops use their authored holds.
            if (authoredMs > 0 && (loop || contactTime < 0))
            {
                float ms = (loop ? Mathf.Repeat(elapsed, duration) : elapsed) / duration * authoredMs;
                // One microsecond tolerates float clock accumulation at exact boundaries.
                if (loop && authoredMs - ms <= .001f) ms = 0;
                int end = 0;
                for (int i = 0; i < frames.Length; i++)
                {
                    end += frameDurationsMs[i];
                    if (ms + .001f < end) return i;
                }
                return frames.Length - 1;
            }
            if (loop) return Mathf.Min(frames.Length - 1,
                Mathf.FloorToInt(Mathf.Repeat(elapsed, duration) / duration * frames.Length));
            if (contactFrame >= 0 && contactFrame < frames.Length && contactTime >= 0 && contactTime < duration)
            {
                if (elapsed < contactTime && contactFrame > 0)
                    return Mathf.Min(contactFrame - 1, Mathf.FloorToInt(elapsed / Mathf.Max(.0001f, contactTime) * contactFrame));
                float recovery = Mathf.Clamp01((elapsed - contactTime) / Mathf.Max(.0001f, duration - contactTime));
                return Mathf.Min(frames.Length - 1, contactFrame + Mathf.FloorToInt(recovery * (frames.Length - contactFrame)));
            }
            return Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed / Mathf.Max(.0001f, duration) * frames.Length));
        }
    }
}
