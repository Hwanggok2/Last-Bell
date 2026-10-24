using UnityEngine;

namespace LastBell
{
    public enum MusicMood { Battle, Boss, Lobby }
    public sealed class BattleAudio : MonoBehaviour
    {
        const float PerfectBellVolume=.38f;
        public AudioSource Music,Sfx,BellA,BellB;
        public AudioClip BattleMusic,BossMusic,LobbyMusic,QuarterMusic;
        public AudioClip Swing,Hit,Critical,Spike,Arrow,Footstep,MiniRoar,BossRoar,Lantern,Dodge,PerfectDodge;
        public AudioClip Windup,LongBell,GoldPickup;
        public AudioClip EnemySwing,EnemyImpact;
        AudioSource currentBell,fadingBell;float bellFade;
        public bool Muted;
        public bool ResidentialArea;
        MusicMood mood;
        bool initialized,paused;
        float targetVolume=.28f;
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
            if(value){if(BellA)BellA.Pause();if(BellB)BellB.Pause();}
            else{if(BellA)BellA.UnPause();if(BellB)BellB.UnPause();}
        }
        public void PlayPerfectBell()
        {
            if(Muted||paused||!LongBell||!BellA||!BellB)return;
            fadingBell=currentBell;currentBell=currentBell==BellA?BellB:BellA;
            currentBell.Stop();currentBell.clip=LongBell;currentBell.volume=0;currentBell.Play();bellFade=0;
        }
        public void Play(AudioClip clip,float volume=1)
        {
            if(!Muted&&!paused&&clip&&Sfx)Sfx.PlayOneShot(clip,Mathf.Clamp01(volume));
        }
        void Update()
        {
            if(Music){Music.mute=Muted;if(!paused)Music.volume=Mathf.MoveTowards(Music.volume,targetVolume,Time.unscaledDeltaTime*.8f);}
            if(Sfx)Sfx.mute=Muted;
            if(BellA)BellA.mute=Muted;if(BellB)BellB.mute=Muted;
            if(!paused&&currentBell)
            {
                bellFade=Mathf.Min(1,bellFade+Time.unscaledDeltaTime/.04f);currentBell.volume=PerfectBellVolume*bellFade;
                if(fadingBell){fadingBell.volume=PerfectBellVolume*(1-bellFade);if(bellFade>=1){fadingBell.Stop();fadingBell=null;}}
            }
        }
    }
}
