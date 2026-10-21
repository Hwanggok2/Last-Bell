using UnityEngine;

namespace LastBell
{
    // Samples the session's transition clock. This component never grants rewards or resolves hits.
    public sealed class BattlePresentation : MonoBehaviour
    {
        public ParallaxWorld World;
        public CombatVfx Vfx;
        public StageBannerGraphic Banner;
        public PresentationOverlay Overlay;
        public DangerGraphic ImpactDanger;
        public BattleAudio Audio;
        public EnemyTelegraphGraphic Telegraphs;
        public EnemyAttackEffects EnemyEffects;
        MvpPresenter owner;
        GameSession session;
        double attackAt=-10000,dodgeAt=-10000,parryAt=-10000,parryHitAt=-10000;
        float visualTime,dodgeVisualAt,enemyImpactAt=-10000,successfulDodgeAt=-10000;
        float completedTravel,deathAttackAge;
        int variant=-1,dodgeDirection,impactRegion;
        int lastPhase=-1,lastKind=-1,lastStep=-1;
        bool critical,roared,lanternPlayed,swingPlayed;
        readonly System.Random visualRandom=new System.Random();
        public bool Reduced => owner&&owner.Effects&&owner.Effects.ReducedShake;
        public int SlashVariant => variant;
        public float TravelPixels => World?World.TravelPixels:0;

        public void Bind(MvpPresenter presenter)
        {
            owner=presenter;session=presenter.Session;
            if(EnemyEffects)EnemyEffects.Bind(owner);
            visualTime=completedTravel=0;attackAt=dodgeAt=parryAt=parryHitAt=-10000;enemyImpactAt=successfulDodgeAt=-10000;
            lastPhase=lastKind=lastStep=-1;roared=lanternPlayed=false;
            owner.PlayerActor.ResetVisuals();owner.EnemyActor.ResetVisuals();ResetCamera();
            if(World)World.ShowTravel(0,Reduced);
            if(Overlay)Overlay.Hide();if(Vfx)Vfx.ClearEffects();
        }
        public void ApplyPause(bool paused)
        {
            if(!owner)return;owner.PlayerActor.SetPlaybackPaused(paused);owner.EnemyActor.SetPlaybackPaused(paused);
            owner.Effects.Paused=paused;if(Audio)Audio.SetPaused(paused);
        }
        public bool ShowsBattle(GameSession state)
        {
            if(state.Phase==SessionPhase.Combat||state.Phase==SessionPhase.TutorialDodge||state.Phase==SessionPhase.TutorialAttack||state.Phase==SessionPhase.StoryDialogue)return true;
            return state.Phase==SessionPhase.Transition&&(state.Transition.Kind!=TransitionKind.Death||state.Transition.ElapsedSeconds<.85f);
        }
        public bool ShowsLargeStage => session!=null&&session.Transition!=null&&session.Transition.Kind==TransitionKind.Arrival&&session.Transition.ElapsedSeconds>=.2f;
        public bool ShowsEnemyHp => session!=null&&(session.Phase==SessionPhase.Combat||session.Phase==SessionPhase.TutorialDodge||session.Phase==SessionPhase.TutorialAttack
            ||session.Transition!=null&&session.Transition.Kind==TransitionKind.BossIntro&&session.Transition.ElapsedSeconds>=session.Transition.DurationSeconds-.2f);
        public void OnEvent(SessionEvent e)
        {
            if(!owner)return;
            if(EnemyEffects)EnemyEffects.OnEvent(e);
            if(e.Kind==SessionEventKind.AttackStarted)
            {
                int next=visualRandom.Next(2);variant=variant<0?visualRandom.Next(3):(variant+1+next)%3;
                parryAt=-10000;dodgeAt=successfulDodgeAt=-10000;
                attackAt=session.ClockMs;critical=false;swingPlayed=false;
                if(e.PerfectCounter){dodgeAt=successfulDodgeAt=-10000;owner.PlayerActor.transform.localPosition=ParallaxWorld.PlanPoint(500,590);owner.PlayerActor.EndSample();}
            }
            if(e.Kind==SessionEventKind.ParryStarted){parryAt=session.ClockMs;parryHitAt=session.EnemyThreats[0].ImpactAtMs;attackAt=dodgeAt=successfulDodgeAt=-10000;owner.PlayerActor.transform.localPosition=ParallaxWorld.PlanPoint(500,590);}
            if(e.Kind==SessionEventKind.DodgeStarted)
            {
                parryAt=-10000;dodgeAt=session.ClockMs;dodgeVisualAt=visualTime;successfulDodgeAt=-10000;dodgeDirection=(int)e.Direction;
                // Accepted dodge cancels the visual lead only while the scheduled hit is unconfirmed.
                attackAt=-10000;
            }
            if(e.Kind==SessionEventKind.EnemyDamaged){critical=e.Critical;if(Audio)Audio.Play(e.Critical?Audio.Critical:Audio.Hit,.75f);}
            if(e.Kind==SessionEventKind.PlayerDamaged||e.Kind==SessionEventKind.Dodged||e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.Parried||e.Kind==SessionEventKind.PerfectParry)
            {
                enemyImpactAt=(float)(e.TimeMs/1000);impactRegion=Region(e.Direction);
                if(e.Kind!=SessionEventKind.PlayerDamaged){successfulDodgeAt=(float)(e.TimeMs/1000);dodgeDirection=(int)e.Direction;if(Audio)Audio.Play((e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.PerfectParry)?Audio.PerfectDodge:Audio.Dodge,(e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.PerfectParry)?.65f:.3f);}
                else if(Audio)Audio.Play(Audio.Hit,.65f);

            }
            if(e.Kind==SessionEventKind.EnemyDefeated)deathAttackAge=(float)((session.ClockMs-attackAt)/1000);
            if(e.Kind==SessionEventKind.PlayerDefeated){attackAt=-10000;owner.PlayerActor.SampleMotion(ActorMotion.Death,0,.55f);}
            if(e.Kind==SessionEventKind.StagePrepared)
            {
                owner.EnemyActor.ResetVisuals();owner.PlayerActor.EndSample();
                attackAt=dodgeAt=parryAt=parryHitAt=-10000;enemyImpactAt=successfulDodgeAt=-10000;lastStep=-1;
            }
            if(e.Kind==SessionEventKind.StageStarted){ResetCamera();owner.PlayerActor.SetDissolve(0);owner.EnemyActor.SetDissolve(0);}
        }
        public void TickVisuals(float deltaSeconds)
        {
            if(!owner||session==null)return;
            ApplyPause(session.IsPaused);if(!session.IsPaused)visualTime+=deltaSeconds;
            var tr=session.Transition;int kind=tr==null?-1:(int)tr.Kind;
            if(lastKind==(int)TransitionKind.Travel&&kind!=lastKind)completedTravel+=1920;
            if(lastPhase!=(int)session.Phase||lastKind!=kind)
            {
                roared=lanternPlayed=false;lastStep=-1;
                if(Vfx)Vfx.ClearEffects();
                if(kind==(int)TransitionKind.Travel||kind==(int)TransitionKind.Arrival||session.Phase==SessionPhase.Combat)owner.PlayerActor.SetDissolve(0);
                lastPhase=(int)session.Phase;lastKind=kind;
            }
            bool battle=ShowsBattle(session);
            owner.PlayerActor.transform.localPosition=ParallaxWorld.PlanPoint(500,590);
            owner.EnemyActor.transform.localPosition=ParallaxWorld.PlanPoint(900,590);
            owner.PlayerActor.SetRunning(false);ResetCamera();
            if(Vfx)Vfx.ClearEffects(true);if(Banner)Banner.Show(session.Stage,-1,Reduced);if(Overlay)Overlay.Hide();if(ImpactDanger)ImpactDanger.Hide();
            if(World)World.ShowTravel(completedTravel,Reduced);
            if(Audio)Audio.SetMood(!battle||session.Phase==SessionPhase.StoryDialogue?MusicMood.Lobby:session.Catalog.GetStage(session.Stage).kind=="boss"?MusicMood.Boss:MusicMood.Battle);

            if(tr!=null)
            {
                float t=tr.ElapsedSeconds;
                switch(tr.Kind)
                {
                    case TransitionKind.EnemyDefeat:
                        SampleAttack(deathAttackAge+t);
                        owner.EnemyActor.SampleMotion(ActorMotion.Death,t,.55f);
                        owner.EnemyActor.SetDissolve(t/.55f);
                        if(Vfx)Vfx.SetDissolve(Point(owner.EnemyActor.transform.position),BodySize(owner.EnemyActor),t/.55f,Reduced);
                        break;
                    case TransitionKind.Travel:
                        owner.EnemyActor.SetDissolve(0);
                        if(!Reduced)owner.PlayerActor.SampleMotion(ActorMotion.Run,t);
                        if(World)World.ShowTravel(completedTravel+1920*tr.Progress,Reduced);
                        owner.EnemyActor.transform.localPosition=ParallaxWorld.PlanPoint(Reduced?900:2820-1920*tr.Progress,590);
                        if(Reduced)owner.EnemyActor.SetDissolve(1-tr.Progress);
                        // Approved 400ms loop contacts at 0ms and 200ms.
                        int step=Mathf.FloorToInt(t*5);
                        if(step!=lastStep&&!session.IsPaused){lastStep=step;if(Audio&&!Reduced)Audio.Play(Audio.Footstep,.24f);}
                        break;
                    case TransitionKind.Arrival:
                        owner.EnemyActor.SetDissolve(0);if(Banner)Banner.Show(session.Stage,t-.2f,Reduced);break;
                    case TransitionKind.BossIntro:ShowBoss(tr);break;
                    case TransitionKind.Death:
                        owner.PlayerActor.SampleMotion(ActorMotion.Death,Mathf.Min(t,.55f),.55f);
                        owner.PlayerActor.SetDissolve(Mathf.Clamp01((t-.2f)/.35f));
                        if(t<.55f&&Vfx)Vfx.SetDissolve(Point(owner.PlayerActor.transform.position),BodySize(owner.PlayerActor),Mathf.Clamp01((t-.2f)/.35f),Reduced);
                        if(Overlay)Overlay.ShowDeath(t);
                        if(t>=.85f&&!lanternPlayed){lanternPlayed=true;if(Audio){Audio.SetMood(MusicMood.Lobby);Audio.Play(Audio.Lantern,.7f);}}
                        break;
                }
            }
            else if(session.Phase==SessionPhase.StoryDialogue)
            {
                owner.EnemyActor.SetDissolve(1);
                var story=session.Story;if(story!=null&&Overlay)Overlay.ShowStory(story.Speaker,story.Text);
                if(Audio)Audio.SetMood(MusicMood.Lobby);
            }
            else if(battle)
            {
                float attackAge=(float)((session.ClockMs-attackAt)/1000);
                SampleAttack(attackAge);SampleDodge();SampleParry();SampleEnemy();
            }
            if(Telegraphs)Telegraphs.Sample(session,Reduced);
            if(EnemyEffects)EnemyEffects.Sample();
            owner.PlayerActor.SetPlaybackPaused(session.IsPaused);owner.EnemyActor.SetPlaybackPaused(session.IsPaused);
        }
        void SampleAttack(float age)
        {
            if(age<0||age>=.5f||attackAt<0)return;
            float strikeX=720;
            float x=CombatGeometry.AttackX(session.Catalog.GetStage(session.Stage).kind,age*1000)/.75f;
            owner.PlayerActor.transform.localPosition=ParallaxWorld.PlanPoint(x,590);
            int frame=age<.12f?0:age<.2f?Mathf.Min(6,Mathf.FloorToInt((age-.12f)/.08f*7)):Mathf.Min(13,7+Mathf.FloorToInt((age-.2f)/.3f*7));
            owner.PlayerActor.SampleClipFrame(ActorMotion.AttackUp,frame);
            if(age>=.175f&&!swingPlayed){swingPlayed=true;if(Audio)Audio.Play(Audio.Swing,.65f);}
            if(Vfx)
            {
                bool up=variant<3;float hitY=up?(strikeX==680?330:strikeX==690?380:460):(strikeX==680||strikeX==690?545:530);
                Vector2 from=PlanPoint(strikeX+(up?45:8),up?545:460),hit=PlanPoint(strikeX<700?875:900,hitY);
                float angle=((variant%3)-1)*7*Mathf.Deg2Rad;Vector2 d=hit-from;
                hit=from+new Vector2(d.x*Mathf.Cos(angle)-d.y*Mathf.Sin(angle),d.x*Mathf.Sin(angle)+d.y*Mathf.Cos(angle));
                Vfx.SetSlash(from,hit,variant,age,critical);
            }
        }
        void SampleDodge()
        {
            float age=session.Phase==SessionPhase.TutorialAttack?visualTime-dodgeVisualAt:(float)((session.ClockMs-dodgeAt)/1000);
            float move=session.Phase==SessionPhase.TutorialAttack?CombatGeometry.DodgeAmount(age*1000,50,150):session.DodgeVisualAmount;
            if(age<0||move<=0)return;
            var pos=ParallaxWorld.PlanPoint(500,590);
            pos.x+=CombatGeometry.DodgeX((DodgeDirection)dodgeDirection)*ParallaxWorld.UnitsPerPixel/.75f*move;
            pos.y-=CombatGeometry.DodgeY((DodgeDirection)dodgeDirection)*ParallaxWorld.UnitsPerPixel/.75f*move;
            owner.PlayerActor.transform.localPosition=pos;
            if(move<=.25f)owner.PlayerActor.SampleMotion(ActorMotion.Idle,0);
            else owner.PlayerActor.SampleSourceFrame(dodgeDirection==1?ActorMotion.DodgeBack:dodgeDirection==0?ActorMotion.DodgeUp:ActorMotion.DodgeForward,dodgeDirection==1?8:4);
        }
        void SampleParry()
        {
            double age=session.ClockMs-parryAt,hitAge=session.ClockMs-parryHitAt;
            if(age<0||hitAge>=180||parryAt<0)return;
            owner.PlayerActor.transform.localPosition=ParallaxWorld.PlanPoint(500,590);
            int frame=hitAge<0?Mathf.FloorToInt((float)(age/System.Math.Max(1,parryHitAt-parryAt))*7):Mathf.Min(13,7+Mathf.FloorToInt((float)hitAge/180*7));
            owner.PlayerActor.SampleClipFrame(ActorMotion.AttackUp,frame);
        }
        void SampleEnemy()
        {
            float impactAge=(float)(session.ClockMs/1000)-enemyImpactAt;
            if(!session.IsEnemyStunned)
            {
                if(impactAge>=0&&impactAge<.24f)owner.EnemyActor.SampleMotion(ActorMotion.Attack,.1f+impactAge,.34f,.1f);
                else
                {
                    var ts=session.EnemyThreats;
                    foreach(var t in ts)
                    {
                        if(session.ClockMs>=t.ReleaseAtMs&&session.ClockMs<=t.ImpactAtMs)
                        {owner.EnemyActor.SampleMotion(ActorMotion.Attack,.02f+(float)((session.ClockMs-t.ReleaseAtMs)/(t.ImpactAtMs-t.ReleaseAtMs))*.08f,.34f,.1f);break;}
                        if(session.ClockMs>=t.CueAtMs&&session.ClockMs<t.CueEndMs)
                        {float q=(float)((session.ClockMs-t.CueAtMs)/(t.CueEndMs-t.CueAtMs));owner.EnemyActor.SampleMotion(ActorMotion.Attack,.08f*q*q,.34f,.1f);}
                    }
                }
            }
        }
        void ShowBoss(TransitionView tr)
        {
            bool boss=session.Catalog.GetStage(session.Stage).kind=="boss";
            float enter=tr.IsRepeat?(boss?.25f:.2f):(boss?.5f:.4f);
            float hold=tr.IsRepeat?(boss?.45f:.35f):(boss?1f:.75f);
            float leave=tr.IsRepeat?(boss?.3f:.25f):(boss?.5f:.45f);
            float t=tr.ElapsedSeconds;
            float amount=t<enter?Smooth(t/enter):t<enter+hold?1:1-Smooth((t-enter-hold)/leave);
            if(Reduced)amount=0;
            float zoom=Mathf.Lerp(1,boss?1.6f:1.45f,amount);owner.WorldCamera.orthographicSize=3.1f/zoom;
            Vector3 focus=ParallaxWorld.PlanPoint(boss?880:900,boss?270:330),screen=ParallaxWorld.PlanPoint(boss?800:820,350);
            var cameraPos=Vector3.Lerp(Vector3.zero,focus-screen/zoom,amount);cameraPos.z=-10;owner.WorldCamera.transform.position=cameraPos;
            if(t>=enter&&t<enter+hold)
            {
                float p=(t-enter)/hold;
                owner.EnemyActor.SampleMotion(ActorMotion.Roar,t-enter,hold,hold*.35f);
                if(Vfx)Vfx.SetRoar(Point(owner.EnemyActor.FaceWorldPosition),81,p);
                if(!roared){roared=true;if(Audio)Audio.Play(boss?Audio.BossRoar:Audio.MiniRoar,.85f);}
            }
            if(Audio)Audio.SetMood(boss?MusicMood.Boss:MusicMood.Battle,t>=enter&&t<enter+hold);
        }
        void ResetCamera(){if(owner&&owner.WorldCamera){owner.WorldCamera.orthographicSize=3.1f;owner.WorldCamera.transform.position=new Vector3(0,0,-10);}}
        Vector2 Point(Vector3 world)=>owner.ScreenPoint(world-owner.Effects.WorldOffset);
        Vector2 PlanPoint(float x,float y)=>Point(owner.WorldRoot.TransformPoint(ParallaxWorld.PlanPoint(x,y)));
        Vector2 BodySize(ActorView actor)
        {
            if(!actor.Body)return Vector2.one*80;var bounds=actor.Body.bounds;
            return new Vector2(Mathf.Abs(Point(bounds.max).x-Point(bounds.min).x),Mathf.Abs(Point(bounds.max).y-Point(bounds.min).y));
        }
        static float Smooth(float value){value=Mathf.Clamp01(value);return value*value*(3-2*value);}
        static int Region(DodgeDirection direction)=>direction==DodgeDirection.Up?2:direction==DodgeDirection.Down?0:direction==DodgeDirection.Back?3:1;
    }
}
