using System;
using UnityEngine;

namespace LastBell
{
    // Saved party actors. Core owns the roster, period, hit and damage; this only samples poses.
    public sealed class CompanionBattleView : MonoBehaviour
    {
        public ActorView[] Actors=Array.Empty<ActorView>();
        readonly string[] ids=new string[3];
        readonly double[] contactAt={-100000,-100000,-100000};
        MvpPresenter owner;
        float idleClock;
        int sampledFrame=-1;
        public void Bind(MvpPresenter presenter)
        {
            owner=presenter;
            idleClock=0;sampledFrame=-1;
            for(int i=0;i<3;i++){ids[i]=null;contactAt[i]=-100000;}
        }
        public ActorView FindActor(string id)
        {
            for(int i=0;i<Actors.Length&&i<ids.Length;i++)if(ids[i]==id)return Actors[i];
            return null;
        }
        public void OnEvent(SessionEvent e)
        {
            if(e.Kind==SessionEventKind.StagePrepared)
                for(int i=0;i<contactAt.Length;i++)contactAt[i]=-100000;
            if(e.Kind==SessionEventKind.CompanionDamaged)
                for(int i=0;i<ids.Length;i++)if(ids[i]==e.CompanionId)contactAt[i]=e.TimeMs;
        }
        public void Sample(GameSession session)
        {
            if(!owner||session==null)return;
            if(sampledFrame!=Time.frameCount&&!session.CombatSuspended)idleClock+=Time.unscaledDeltaTime;
            sampledFrame=Time.frameCount;
            var party=session.RunCompanions;
            var tr=session.Transition;bool combat=session.Phase==SessionPhase.Combat;
            bool visible=owner.Presentation.ShowsBattle(session)&&session.Phase!=SessionPhase.StoryDialogue&&session.Phase!=SessionPhase.ComboHint;
            for(int i=0;i<Actors.Length;i++)
            {
                var actor=Actors[i];if(!actor)continue;
                bool show=visible&&i<party.Count;
                actor.gameObject.SetActive(show);if(!show)continue;
                var member=party[i];
                if(ids[i]!=member.Id){ids[i]=member.Id;contactAt[i]=-100000;actor.ApplyArt(owner.Art.Find("companion_"+member.Id),true);}
                actor.SetPlaybackPaused(session.CombatSuspended);
                float home=380-i*92,x=home;
                var clip=actor.GetClip(ActorMotion.Attack);
                bool melee=member.Id=="barun"||member.Id=="doto";
                float since=(float)((session.ClockMs-contactAt[i])/1000);
                if(tr!=null&&tr.Kind==TransitionKind.EnemyDefeat)since+=tr.ElapsedSeconds;
                if(tr!=null&&tr.Kind==TransitionKind.Travel&&!owner.Presentation.Reduced)
                    actor.SampleMotion(ActorMotion.Run,tr.ElapsedSeconds+i*.06f);
                else if(combat&&member.IsWindingUp)
                {
                    float elapsed=Mathf.Max(0,(session.Catalog.adventure.companionWindupMs-(float)member.RemainingMs)/1000);
                    float q=Mathf.Clamp01(elapsed/(session.Catalog.adventure.companionWindupMs/1000f));
                    actor.SampleMotion(ActorMotion.Attack,elapsed);
                    if(melee)x=Mathf.Lerp(home,ContactX(actor,clip),q*q*(3-2*q));
                }
                else if((combat||tr!=null&&tr.Kind==TransitionKind.EnemyDefeat)&&since>=0&&since<.42f)
                {
                    actor.SampleMotion(ActorMotion.Attack,.18f+since);
                    if(melee)x=Mathf.Lerp(ContactX(actor,clip),home,Mathf.SmoothStep(0,1,since/.42f));
                }
                else actor.SampleMotion(ActorMotion.Idle,idleClock+i*.17f);
                actor.transform.localPosition=ParallaxWorld.GroundPoint(x,session.Stage);
                actor.SetDissolve(tr!=null&&tr.Kind==TransitionKind.Death?Mathf.Clamp01((tr.ElapsedSeconds-.2f)/.35f):0);
            }
        }
        float ContactX(ActorView actor,ActorClip clip)
        {
            // Meet the enemy's current front without moving the enemy or resolving a hit.
            float enemy=900;
            if(owner.EnemyActor)enemy=900+(owner.EnemyActor.transform.localPosition.x-ParallaxWorld.GroundPoint(900,owner.Session.Stage).x)/ParallaxWorld.UnitsPerPixel;
            return enemy-Mathf.Max(32,actor.ContactOffsetX(clip)/ParallaxWorld.UnitsPerPixel)-40;
        }
    }
}
