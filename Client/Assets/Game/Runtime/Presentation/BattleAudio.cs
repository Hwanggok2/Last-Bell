using UnityEngine;

namespace LastBell
{
    public enum MusicMood { Battle, Boss, Lobby }
    public sealed class BattleAudio : MonoBehaviour
    {
        // Bell fields are retained for serialized prefab compatibility; v8.6 clears them.
        public AudioSource Music,Sfx,BellA,BellB;
        public AudioClip BattleMusic,BossMusic,LobbyMusic,QuarterMusic;
        public AudioClip Swing,Hit,Critical,Spike,Arrow,Footstep,MiniRoar,BossRoar,Lantern,Dodge,PerfectDodge;
        public AudioClip Windup,LongBell,GoldPickup;
        public AudioClip EnemySwing,EnemyImpact;
        public AudioClip AttackTouch;
        public bool Muted;
        public bool ResidentialArea;
        MusicMood mood;
        bool initialized,paused;
        float targetVolume=.28f;
        float lastTouchTime=-1;
        public void SetMood(MusicMood next,bool duck=false)
        {
            if(!Music)return;
            var clip=next==MusicMood.Boss?BossMusic:next==MusicMood.Lobby?LobbyMusic:ResidentialArea?QuarterMusic:BattleMusic;
            if(!initialized||mood!=next||Music.clip!=clip)
            {
                mood=next;initialized=true;
                Music.clip=clip;
                Music.loop=true;Music.volume=0;
                if(Music.clip){Music.Play();if(paused)Music.Pause();}
            }
            targetVolume=duck?.12f:next==MusicMood.Boss?.20f:.24f;
        }
        public void SetPaused(bool value)
        {
            if(paused==value)return;paused=value;
            if(value){if(Music)Music.Pause();if(Sfx)Sfx.Stop();}
            else if(Music)Music.UnPause();
        }
        public void PlayDodge()
        {
            Play(Dodge,.65f);
        }
        public void PlayAttackTouch()
        {
            // A quiet input tick, independent of the later confirmed contact sound.
            if(Muted||paused||!AttackTouch||!Sfx||Time.unscaledTime-lastTouchTime<.05f)return;
            lastTouchTime=Time.unscaledTime;Play(AttackTouch,.35f);
        }
        public void Play(AudioClip clip,float volume=1)
        {
            if(!Muted&&!paused&&clip&&Sfx)Sfx.PlayOneShot(clip,Mathf.Clamp01(volume));
        }
        void Update()
        {
            if(Music){Music.mute=Muted;if(!paused)Music.volume=Mathf.MoveTowards(Music.volume,targetVolume,Time.unscaledDeltaTime*.8f);}
            if(Sfx)Sfx.mute=Muted;
        }
    }
}
