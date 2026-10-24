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
        public SkillPixelEffects SkillEffects;
        public CompanionBattleView Companions;
        MvpPresenter owner;
        GameSession session;
        double attackAt=-10000,dodgeAt=-10000,parryAt=-10000,parryHitAt=-10000;
        float visualTime,dodgeVisualAt,enemyImpactAt=-10000,successfulDodgeAt=-10000;
        float completedTravel,deathAttackAge,attackSpeed=1,activeTravelPixels=1920;
        int variant=-1,dodgeDirection,impactRegion;
        int lastPhase=-1,lastKind=-1,lastStep=-1;
        bool critical,roared,lanternPlayed,skillAttack;
        string enemyImpactPattern;
        ActorMotion defeatedEnemyMotion;
        Vector3 defeatedEnemyPosition;
        int defeatedEnemyFrame;
        readonly System.Random visualRandom=new System.Random();
        sealed class PracticeVisualState
        {
            public double Attack,Dodge,Parry,ParryHit;
            public float Time,DodgeTime,EnemyImpact,SuccessfulDodge,Speed;
            public int Variant,Direction,Region;
            public bool Critical,Skill;
            public string Pattern;
        }
        PracticeVisualState practiceVisual;
        public void SavePracticeVisual()
        {
            practiceVisual=new PracticeVisualState{Attack=attackAt,Dodge=dodgeAt,Parry=parryAt,ParryHit=parryHitAt,Time=visualTime,DodgeTime=dodgeVisualAt,
                EnemyImpact=enemyImpactAt,SuccessfulDodge=successfulDodgeAt,Speed=attackSpeed,Variant=variant,Direction=dodgeDirection,Region=impactRegion,Critical=critical,Skill=skillAttack,Pattern=enemyImpactPattern};
        }
        public void RestorePracticeVisual()
        {
            var p=practiceVisual;if(p==null)return;
            attackAt=p.Attack;dodgeAt=p.Dodge;parryAt=p.Parry;parryHitAt=p.ParryHit;visualTime=p.Time;dodgeVisualAt=p.DodgeTime;
            enemyImpactAt=p.EnemyImpact;successfulDodgeAt=p.SuccessfulDodge;attackSpeed=p.Speed;variant=p.Variant;dodgeDirection=p.Direction;impactRegion=p.Region;
            critical=p.Critical;skillAttack=p.Skill;enemyImpactPattern=p.Pattern;practiceVisual=null;
        }
        public bool Reduced => owner&&owner.Effects&&owner.Effects.ReducedShake;
        public int SlashVariant => variant;
        public float TravelPixels => World?World.TravelPixels:0;

        public void Bind(MvpPresenter presenter)
        {
            owner=presenter;session=presenter.Session;
            practiceVisual=null;
            if(EnemyEffects)EnemyEffects.Bind(owner);
            if(SkillEffects)SkillEffects.Initialize(owner);
            if(Companions)Companions.Bind(owner);
            visualTime=completedTravel=0;attackAt=dodgeAt=parryAt=parryHitAt=-10000;enemyImpactAt=successfulDodgeAt=-10000;
            enemyImpactPattern=null;skillAttack=false;
            defeatedEnemyMotion=ActorMotion.Idle;defeatedEnemyFrame=0;
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
            if(state.Phase==SessionPhase.Combat||state.Phase==SessionPhase.TutorialDodge||state.Phase==SessionPhase.TutorialAttack||state.Phase==SessionPhase.StoryDialogue||state.Phase==SessionPhase.ComboHint)return true;
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
                int next=visualRandom.Next(5);variant=variant<0?visualRandom.Next(6):(variant+1+next)%6;
                parryAt=-10000;dodgeAt=successfulDodgeAt=-10000;
                attackAt=session.ClockMs;attackSpeed=(float)session.AttackSpeed;critical=false;
                skillAttack=!string.IsNullOrEmpty(e.SkillId);
                if(e.PerfectCounter){dodgeAt=successfulDodgeAt=-10000;owner.PlayerActor.transform.localPosition=GroundPoint(500);owner.PlayerActor.EndSample();}
            }
            if(e.Kind==SessionEventKind.ParryStarted)
            {
                if(Audio)Audio.PlayDodge();
                parryAt=session.ClockMs;var threats=session.EnemyThreats;
                parryHitAt=session.IsParryWindow?threats[0].ImpactAtMs:parryAt+session.Catalog.combat.dodgeActiveMs;
                attackAt=dodgeAt=successfulDodgeAt=-10000;owner.PlayerActor.transform.localPosition=GroundPoint(500);
            }
            if(e.Kind==SessionEventKind.DodgeStarted)
            {
                if(Audio)Audio.PlayDodge();
                parryAt=-10000;dodgeAt=session.ClockMs;dodgeVisualAt=visualTime;successfulDodgeAt=-10000;dodgeDirection=(int)e.Direction;
                // Accepted dodge cancels the visual lead only while the scheduled hit is unconfirmed.
                attackAt=-10000;
            }
            if(e.Kind==SessionEventKind.EnemyDamaged)
            {
                critical=e.Critical;
                // Short blade/impact sound starts only on confirmed contact.
                if(Audio)Audio.Play(Audio.Swing,.75f);
            }
            // Independent skill/party contacts do not start or replace the player's slash pose.
            if(e.Kind==SessionEventKind.PlayerSkillStarted&&Audio)Audio.PlayAttackTouch();
            if(e.Kind==SessionEventKind.PlayerSkillContact&&Audio)Audio.Play(Audio.Hit,.7f);
            if(e.Kind==SessionEventKind.CompanionDamaged&&Audio)Audio.Play(Audio.Hit,string.IsNullOrEmpty(e.SkillId)?.38f:.6f);
            if(e.Kind==SessionEventKind.PlayerDamaged||e.Kind==SessionEventKind.SkillShieldAbsorbed||e.Kind==SessionEventKind.Dodged||e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.Parried||e.Kind==SessionEventKind.PerfectParry)
            {
                enemyImpactAt=(float)(e.TimeMs/1000);impactRegion=Region(e.Direction);
                enemyImpactPattern=e.PatternId;
                // Dodge audio already played on accepted input, including perfect dodges.
                if(e.Kind!=SessionEventKind.PlayerDamaged&&e.Kind!=SessionEventKind.SkillShieldAbsorbed){successfulDodgeAt=(float)(e.TimeMs/1000);dodgeDirection=(int)e.Direction;}
                else if(e.Kind==SessionEventKind.PlayerDamaged&&Audio)Audio.Play(Audio.Hit,.6f);

            }
            if(e.Kind==SessionEventKind.EnemyDefeated)
            {
                deathAttackAge=(float)((session.ClockMs-attackAt)/1000);
                defeatedEnemyMotion=owner.EnemyActor.CurrentMotion;defeatedEnemyFrame=owner.EnemyActor.CurrentFrame;
                defeatedEnemyPosition=owner.EnemyActor.transform.localPosition;
                if(owner.View.GoldPickup)owner.View.GoldPickup.Begin(e.GoldReward,Point(owner.EnemyActor.Body.bounds.center),session.Stage==10&&session.Phase==SessionPhase.StoryDialogue);
            }
            if(e.Kind==SessionEventKind.PlayerDefeated){attackAt=-10000;owner.PlayerActor.SampleMotion(ActorMotion.Death,0,.55f);}
            if(e.Kind==SessionEventKind.StagePrepared)
            {
                owner.EnemyActor.ResetVisuals();owner.PlayerActor.EndSample();
                attackAt=dodgeAt=parryAt=parryHitAt=-10000;enemyImpactAt=successfulDodgeAt=-10000;lastStep=-1;
            }
            if(e.Kind==SessionEventKind.StageStarted){ResetCamera();owner.PlayerActor.SetDissolve(0);owner.EnemyActor.SetDissolve(0);}
            if(SkillEffects)SkillEffects.OnEvent(e,variant);
            if(Companions)Companions.OnEvent(e);
        }
        public void TickVisuals(float deltaSeconds)
        {
            if(!owner||session==null)return;
            if(practiceVisual!=null&&!session.ComboPractice.IsReplay)RestorePracticeVisual();
            ApplyPause(session.CombatSuspended);if(!session.CombatSuspended)visualTime+=deltaSeconds;
            var tr=session.Transition;int kind=tr==null?-1:(int)tr.Kind;
            if(lastKind==(int)TransitionKind.Travel&&kind!=lastKind)completedTravel+=activeTravelPixels;
            if(kind==(int)TransitionKind.Travel)activeTravelPixels=ParallaxWorld.TravelDistance(tr);
            if(lastPhase!=(int)session.Phase||lastKind!=kind)
            {
                roared=lanternPlayed=false;lastStep=-1;
                if(Vfx)Vfx.ClearEffects();
                if(kind==(int)TransitionKind.Travel||kind==(int)TransitionKind.Arrival||session.Phase==SessionPhase.Combat)owner.PlayerActor.SetDissolve(0);
                lastPhase=(int)session.Phase;lastKind=kind;
            }
            bool battle=ShowsBattle(session);
            owner.PlayerActor.transform.localPosition=GroundPoint(500);
            owner.EnemyActor.transform.localPosition=GroundPoint(900);
            owner.PlayerActor.SetRunning(false);ResetCamera();
            if(Vfx)Vfx.ClearEffects(true);if(Banner)Banner.Show(session.Stage,-1,Reduced);if(Overlay)Overlay.Hide();if(ImpactDanger)ImpactDanger.Hide();
            if(World)World.ShowTravel(completedTravel,Reduced);
            if(World)World.ShowStage(session.Stage,tr,session.EnemyHp<=0&&session.Phase!=SessionPhase.StoryDialogue,Reduced);
            if(Audio){Audio.ResidentialArea=session.Stage>=11;Audio.SetMood(!battle||session.Phase==SessionPhase.StoryDialogue?MusicMood.Lobby:session.Catalog.GetStage(session.Stage).kind!="normal"?MusicMood.Boss:MusicMood.Battle);}

            if(tr!=null)
            {
                float t=tr.ElapsedSeconds;
                switch(tr.Kind)
                {
                    case TransitionKind.EnemyDefeat:
                        owner.EnemyActor.transform.localPosition=defeatedEnemyPosition;
                        SampleAttack(deathAttackAge+t);
                        owner.EnemyActor.SampleClipFrame(defeatedEnemyMotion,defeatedEnemyFrame);
                        owner.EnemyActor.SetDissolve(t/.55f);
                        // Gold pixels leave immediately; no falling debris precedes collection.
                        break;
                    case TransitionKind.Travel:
                        owner.EnemyActor.SetDissolve(0);
                        if(!Reduced)owner.PlayerActor.SampleMotion(ActorMotion.Run,t);
                        if(World)World.ShowTravel(completedTravel+activeTravelPixels*tr.Progress,Reduced);
                        owner.EnemyActor.transform.localPosition=GroundPoint(Reduced?900:900+activeTravelPixels*(1-tr.Progress));
                        if(Reduced)owner.EnemyActor.SetDissolve(1-tr.Progress);
                        // Approved 400ms loop contacts at 0ms and 200ms.
                        int step=Mathf.FloorToInt(t*5);
                        if(step!=lastStep&&!session.IsPaused){lastStep=step;if(Audio&&!Reduced)Audio.Play(Audio.Footstep,.22f);}
                        break;
                    case TransitionKind.Arrival:
                        owner.EnemyActor.SetDissolve(0);if(Banner)Banner.Show(session.Stage,t-.2f,Reduced);break;
                    case TransitionKind.BossIntro:ShowBoss(tr);break;
                    case TransitionKind.Death:
                        owner.PlayerActor.SampleMotion(ActorMotion.Death,Mathf.Min(t,.55f),.55f);
                        owner.PlayerActor.SetDissolve(Mathf.Clamp01((t-.2f)/.35f));
                        if(t<.55f&&Vfx)Vfx.SetDissolve(Point(owner.PlayerActor.transform.position),BodySize(owner.PlayerActor),Mathf.Clamp01((t-.2f)/.35f),Reduced);
                        if(Overlay)Overlay.ShowDeath(t);
                        if(t>=.85f&&!lanternPlayed){lanternPlayed=true;if(Audio){Audio.SetMood(MusicMood.Lobby);Audio.Play(Audio.Lantern,.38f);}}
                        break;
                }
            }
            else if(session.Phase==SessionPhase.StoryDialogue)
            {
                if(session.Stage==10){owner.EnemyActor.SetDissolve(0);owner.EnemyActor.SampleClipFrame(defeatedEnemyMotion,defeatedEnemyFrame);}
                else owner.EnemyActor.SetDissolve(1);
                var story=session.Story;if(story!=null&&Overlay){Overlay.ShowStory(story.Speaker,story.Text,story.PortraitId);Overlay.ShowCompanionChoice(story.AwaitingCompanionChoice);}

                if(Audio)Audio.SetMood(MusicMood.Lobby);
            }
            else if(battle)
            {
                float attackAge=(float)((session.ClockMs-attackAt)/1000);
                SampleEnemy();SampleAttack(attackAge);SampleDodge();SampleParry();
            }
            if(Telegraphs)Telegraphs.Sample(session,Reduced);
            if(EnemyEffects)EnemyEffects.Sample();
            if(Companions)Companions.Sample(session);
            if(SkillEffects)SkillEffects.Render(session);
            owner.PlayerActor.SetPlaybackPaused(session.CombatSuspended);owner.EnemyActor.SetPlaybackPaused(session.CombatSuspended);
        }
        void SampleAttack(float age)
        {
            age*=attackSpeed;
            if(age<0||age>=.5f||attackAt<0)return;
            float strikeX=720;
            float x=CombatGeometry.AttackX(session.Catalog.GetStage(session.Stage).kind,age*1000)/.75f;
            owner.PlayerActor.transform.localPosition=GroundPoint(x);
            int frame=age<.12f?0:age<.2f?Mathf.Min(6,Mathf.FloorToInt((age-.12f)/.08f*7)):Mathf.Min(13,7+Mathf.FloorToInt((age-.2f)/.3f*7));
            // Keep the approved approach pose for 120ms, then the authored slash
            // reaches its contact pose on the existing 200ms gameplay hit.
            if(IsDragonPlayer)owner.PlayerActor.SampleMotion(ActorMotion.Attack,Mathf.Max(0,age-.12f),.38f,.08f);
            else owner.PlayerActor.SampleClipFrame(ActorMotion.AttackUp,frame);
            if(Vfx)
            {
                bool up=variant<3;float hitY=up?(strikeX==680?330:strikeX==690?380:460):(strikeX==680||strikeX==690?545:530);
                Vector2 from=PlanPoint(strikeX+(up?45:8),up?545:460),hit=PlanPoint(strikeX<700?875:900,hitY);
                if(IsNewCreature(owner.EnemyActor.ArtId))hit=Point(owner.EnemyActor.CanonicalBodyPoint(.5f));
                float angle=((variant%3)-1)*7*Mathf.Deg2Rad;Vector2 d=hit-from;
                hit=from+new Vector2(d.x*Mathf.Cos(angle)-d.y*Mathf.Sin(angle),d.x*Mathf.Sin(angle)+d.y*Mathf.Cos(angle));
                Vfx.SetSlash(from,hit,variant,age,critical||skillAttack);
                if(Vfx.SlashSprite)
                {
                    Vfx.SlashSprite.color=!skillAttack?Color.white:session.CurrentCharacterId=="dam"?new Color32(146,211,255,255):
                        session.CurrentCharacterId=="rin"?new Color32(158,247,195,255):new Color32(255,219,123,255);
                    if(skillAttack)Vfx.SlashSprite.rectTransform.sizeDelta*=1.2f;
                }
            }
        }
        void SampleDodge()
        {
            float age=session.Phase==SessionPhase.TutorialAttack?visualTime-dodgeVisualAt:(float)((session.ClockMs-dodgeAt)/1000);
            float move=session.Phase==SessionPhase.TutorialAttack?CombatGeometry.DodgeAmount(age*1000,50,150):session.DodgeVisualAmount;
            if(age<0||move<=0)return;
            var pos=GroundPoint(500);
            pos.x+=CombatGeometry.DodgeX((DodgeDirection)dodgeDirection)*ParallaxWorld.UnitsPerPixel/.75f*move;
            pos.y-=CombatGeometry.DodgeY((DodgeDirection)dodgeDirection)*ParallaxWorld.UnitsPerPixel/.75f*move;
            owner.PlayerActor.transform.localPosition=pos;
            if(IsDragonPlayer)
            {
                // Follow the accepted dodge's actual travel/hold/recovery, including
                // an immediate contact-frame dodge. Presentation never extends safety.
                var motion=dodgeDirection==1?ActorMotion.DodgeBack:dodgeDirection==0?ActorMotion.DodgeUp:ActorMotion.DodgeForward;
                var clip=owner.PlayerActor.GetClip(motion);
                int last=clip.frames.Length-1,hold=clip.contactFrame>=0?clip.contactFrame:last/2;
                int pose=move>=.999f?hold:age<.05f?Mathf.Min(hold,Mathf.FloorToInt(move*(hold+1))):Mathf.Min(last,hold+1+Mathf.FloorToInt((1-move)*(last-hold)));
                owner.PlayerActor.SampleClipFrame(motion,pose);
            }
            else if(move<=.25f)owner.PlayerActor.SampleMotion(ActorMotion.Idle,0);
            else owner.PlayerActor.SampleSourceFrame(dodgeDirection==1?ActorMotion.DodgeBack:dodgeDirection==0?ActorMotion.DodgeUp:ActorMotion.DodgeForward,dodgeDirection==1?8:4);
        }
        void SampleParry()
        {
            double age=session.ClockMs-parryAt,hitAge=session.ClockMs-parryHitAt;
            if(age<0||hitAge>=180||parryAt<0)return;
            owner.PlayerActor.transform.localPosition=GroundPoint(500);
            int frame=hitAge<0?Mathf.FloorToInt((float)(age/System.Math.Max(1,parryHitAt-parryAt))*7):Mathf.Min(13,7+Mathf.FloorToInt((float)hitAge/180*7));
            if(IsDragonPlayer)owner.PlayerActor.SampleMotion(ActorMotion.DodgeDown,(float)age/1000,(float)(parryHitAt-parryAt+180)/1000,(float)(parryHitAt-parryAt)/1000);
            else owner.PlayerActor.SampleClipFrame(ActorMotion.AttackUp,frame);
        }
        bool IsDragonPlayer => owner.PlayerActor.ArtId=="dragon_hero_on"||owner.PlayerActor.ArtId=="player_dragon_daren"||owner.PlayerActor.ArtId=="player_dragon_miel";
        void SampleEnemy()
        {
            if(session.Phase==SessionPhase.ComboHint||session.IsEnemyStunned)return;
            var pending=session.EnemyThreats;
            EnemyThreatView next=null;
            foreach(var threat in pending)
                if(!threat.Resolved&&session.ClockMs>=threat.CueAtMs&&session.ClockMs<=threat.ImpactAtMs
                    &&(next==null||threat.ImpactAtMs<next.ImpactAtMs))next=threat;
            // The actual impact clock is the one source of truth. The following cue
            // may already be visible, but cannot replace this attack or its return.
            if(next!=null)
            {
                bool follow=next.SequenceIndex>0;
                float before=(float)(next.ImpactAtMs-session.ClockMs)/1000;
                float lead=follow?.37f:.5f;
                if(before<=lead)
                {
                    var motion=EnemyMotion(next.PatternId);var clip=owner.EnemyActor.GetClip(motion);
                    if(clip==null)return;
                    float contact=Mathf.Max(0,clip.ContactTime);
                    float retreat=RetreatDistance(follow);
                    float reach=AttackReach(clip),x,pose;
                    if(before>.25f)
                    {
                        float q=Smooth((lead-before)/(lead-.25f));
                        x=900+retreat*q;pose=contact*.65f*q;
                    }
                    else if(before>.16f){x=900+retreat;pose=contact*.65f;}
                    else
                    {
                        float q=Smooth(1-before/.16f);
                        x=Mathf.Lerp(900+retreat,900-reach,q);pose=Mathf.Lerp(contact*.65f,contact,q);
                    }
                    owner.EnemyActor.transform.localPosition=GroundPoint(x);
                    owner.EnemyActor.SampleMotion(motion,pose);return;
                }
            }
            float since=(float)(session.ClockMs/1000)-enemyImpactAt;
            if(since>=0&&since<.28f)
            {
                var motion=EnemyMotion(enemyImpactPattern);var clip=owner.EnemyActor.GetClip(motion);
                if(clip!=null)
                {
                    float q=Smooth(since/.28f);
                    owner.EnemyActor.transform.localPosition=GroundPoint(900-AttackReach(clip)*(1-q));
                    owner.EnemyActor.SampleMotion(motion,Mathf.Lerp(Mathf.Max(0,clip.ContactTime),clip.Duration-.001f,q));return;
                }
            }
            if(next!=null)owner.EnemyActor.SampleClipFrame(EnemyMotion(next.PatternId),0);
        }
        float RetreatDistance(bool follow)
        {
            string kind=session.Catalog.GetStage(session.Stage).kind;
            float distance=follow?12:kind=="boss"?24:kind=="miniboss"?32:48;
            float extent=owner.EnemyActor.Body?Mathf.Max(0,(owner.EnemyActor.Body.bounds.max.x-owner.EnemyActor.transform.position.x)/ParallaxWorld.UnitsPerPixel):0;
            return Mathf.Min(distance,Mathf.Max(0,1264-900-extent));
        }
        float AttackReach(ActorClip clip)=>Mathf.Clamp(400+owner.EnemyActor.ContactOffsetX(clip)/ParallaxWorld.UnitsPerPixel,0,400);
        public static bool IsNewCreature(string id)=>(id!=null&&id.StartsWith("dragon_"))||id=="wild_dog"||id=="grey_wolf"||id=="brown_bear"||id=="sewer_rat"||id=="drowned_resident"||id=="water_wraith"
            ||id=="red_fox"||id=="wild_boar"||id=="badger"||id=="lynx"||id=="mountain_goat"||id=="great_stag";
        public static ActorMotion EnemyMotion(string pattern)
        {
            switch(pattern){case "low_sweep":return ActorMotion.AttackLow;case "high_sweep":return ActorMotion.AttackHigh;
                case "thrust":case "frontal_dash":return ActorMotion.AttackThrust;case "outer_arc":return ActorMotion.AttackRear;default:return ActorMotion.Attack;}
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
            var cameraPos=Vector3.Lerp(new Vector3(0,-1.04f,0),focus-screen/zoom,amount);cameraPos.z=-10;owner.WorldCamera.transform.position=cameraPos;
            if(t>=enter&&t<enter+hold)
            {
                float p=(t-enter)/hold;
                owner.EnemyActor.SampleMotion(ActorMotion.Roar,t-enter,hold,hold*.35f);
                if(Vfx)Vfx.SetRoar(Point(owner.EnemyActor.FaceWorldPosition),81,p);
                if(!roared){roared=true;if(Audio)Audio.Play(boss?Audio.BossRoar:Audio.MiniRoar,.85f);}
            }
            if(Audio)Audio.SetMood(boss?MusicMood.Boss:MusicMood.Battle,t>=enter&&t<enter+hold);
        }
        void ResetCamera(){if(owner&&owner.WorldCamera){owner.WorldCamera.orthographicSize=3.1f;owner.WorldCamera.transform.position=new Vector3(0,-1.04f,-10);}}
        Vector3 GroundPoint(float x)=>ParallaxWorld.GroundPoint(x,session.Stage);
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
