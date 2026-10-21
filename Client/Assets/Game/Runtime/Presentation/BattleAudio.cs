using UnityEngine;

namespace LastBell
{
    public enum MusicMood { Battle, Boss, Lobby }
    public sealed class BattleAudio : MonoBehaviour
    {
        public AudioSource Music,Sfx;
        public AudioClip BattleMusic,BossMusic,LobbyMusic;
        public AudioClip Swing,Hit,Critical,Spike,Arrow,Footstep,MiniRoar,BossRoar,Lantern,Dodge,PerfectDodge;
        public AudioClip Windup;
        public bool Muted;
        MusicMood mood;
        bool initialized,paused;
        float targetVolume=.28f;
        public void SetMood(MusicMood next,bool duck=false)
        {
            if(!Music)return;
            if(!initialized||mood!=next)
            {
                mood=next;initialized=true;
                Music.clip=next==MusicMood.Boss?BossMusic:next==MusicMood.Lobby?LobbyMusic:BattleMusic;
                Music.loop=true;Music.volume=0;
                if(Music.clip){Music.Play();if(paused)Music.Pause();}
            }
            targetVolume=duck?.12f:.28f;
        }
        public void SetPaused(bool value)
        {
            if(paused==value)return;paused=value;
            if(value){if(Music)Music.Pause();if(Sfx)Sfx.Stop();}
            else if(Music)Music.UnPause();
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
