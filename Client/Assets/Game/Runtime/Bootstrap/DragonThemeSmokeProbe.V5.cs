#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastBell
{
    // Explicit Development-only observation. No production rule or view mutations.
    public sealed partial class DragonThemeSmokeProbe
    {
        [Serializable] sealed class V5Evidence
        {
            public string scope="v0.9.4 representative changed-scope observations; not all 60 acceptance criteria";
            public string visualReview="NOT_RUN",osInput="DEFERRED",audioListening="NOT_RUN",userDefaultProfile="NOT_ACCESSED";
            public string coreTests="Handed-over Client evidence only; no Core test campaign executed";
            public string limits="Explicit DTO fixtures, synthetic UI raycasts and bounded real-render windows. Manual clock gaps are labelled. Future NPC fixtures do not prove stages21-50. No device performance, audio voice-count or full acceptance certification.";
            public string[] pending={"manual PNG readability/occlusion review","all archetype retreat/return boundaries","typewriter/held/prebuffer edge audit","all economy/formula boundaries","all corruption/fallback migrations","native stunned-state restoration","physical mouse/keyboard/mobile","audio listening and normal-speed performance"};
            public List<V5Event> events=new List<V5Event>();
            public List<string> fixtures=new List<string>();
            public int stateSamples,staticCaptures,inputEdges;
        }
        [Serializable] sealed class V5Event
        {
            public string scenario,fixture,kind,skill,companion,service,pattern;public int frame,stage,runId;
            public double clockMs,realtime;public long damageMilli,absorbedMilli,recoveryMilli,gold;
        }
        [Serializable] sealed class V5Input
        {public string scenario,control,kind;public int sequence,frame,eventCountBefore,eventCountAfter;public double clockBefore,clockAfter;public bool interactable;}
        [Serializable] sealed class V5Dot
        {public Vector3 position;public float size,life;public Color32 color;}
        [Serializable] sealed class V5Particles
        {public string name,path,texture,filter;public int instance,count;public List<V5Dot> dots=new List<V5Dot>();}
        [Serializable] sealed class V5Member
        {public string id;public int slot,level,periodMs;public double remainingMs;public long damageMilli;public bool windingUp;public V3ActorFrame actor;}
        [Serializable] sealed class V5Threat
        {public string pattern,direction;public int sequenceIndex;public double cueAtMs,cueEndMs,impactAtMs;public bool resolved;}
        [Serializable] sealed class V5State
        {
            public string tag,file,fixture,clockMode,character,phase,scene,speaker,text,portrait;
            public int frame,stage,runId,schema,line,lineCount,recovery,slots,weaponRank,rtWidth,rtHeight,rtAA,rtStep;
            public double clockMs,realtime,cooldownMs,shieldMs,stunSeconds;public long hpMilli,enemyHpMilli,shieldMilli,gold;public int gems;
            public bool paused,suspended,practiceActive,practiceHold,practiceCompleted,practiceSkipped,stunReady,stunned,immune,playerSpeaking;
            public int practiceStep;public float stagger;public string requiredDirection,rtFilter;
            public bool rtMips,rawVisible,rawRaycast,textureBound;public Rect rawBounds,portraitBounds,speakerBounds,bodyBounds;
            public int speakerFont,bodyFont;public V3ActorFrame player,enemy;
            public List<V5Particles> particles=new List<V5Particles>();public List<V5Member> members=new List<V5Member>();public List<V5Threat> threats=new List<V5Threat>();
        }
        [Serializable] sealed class V5ResumeExpectation {public string kind;public GameSaveData save;}
        readonly V5Evidence v5Evidence=new V5Evidence();
        StreamWriter v5States,v5Inputs;bool v5Active;string v5Fixture="native-boot";V5ResumeExpectation v5Resume;

        static void V5Preflight(string dest,string save,string mode)
        {
            string expected=Path.Combine(dest,"expected-catalog.json");RejectLinks(expected);
            if(!File.Exists(expected))throw new IOException("V5 requires the frozen expected catalog.");
            if(mode=="v5-resume")
            {
                string checkpoint=Path.Combine(dest,"v5-resume-expected.json");RejectLinks(checkpoint);
                if(!File.Exists(checkpoint)||!File.Exists(Path.Combine(save,"last-bell-save.json")))throw new IOException("V5 resume requires an isolated checkpoint and expectation.");
            }
            else if(mode!="v5-story12")
            {
                string seed=Path.Combine(dest,"legacy-source","last-bell-save.json");RejectLinks(seed);
                if(!File.Exists(seed))throw new IOException("V5 requires an explicit prior-QA schema8 source copy.");
                if(mode=="v5-legacy"&&(!File.Exists(Path.Combine(save,"last-bell-save.json"))||File.ReadAllText(seed)!=File.ReadAllText(Path.Combine(save,"last-bell-save.json"))))
                    throw new IOException("V5 legacy boot must use exactly the copied prior-QA seed.");
            }
            foreach(string file in Directory.Exists(save)?Directory.GetFiles(save):Array.Empty<string>())RejectLinks(file);
        }
        IEnumerator DragonV5(bool resume,bool legacyBoot,bool story12Only=false)
        {
            v5Active=true;v5States=new StreamWriter(new FileStream(Path.Combine(output,"v5-state.jsonl"),FileMode.CreateNew));
            v5Inputs=new StreamWriter(new FileStream(Path.Combine(output,"v5-input.jsonl"),FileMode.CreateNew));
            v3Index=new StreamWriter(new FileStream(Path.Combine(output,"frames.jsonl"),FileMode.CreateNew));
            v3Evidence.coverage="V5 reuses buffered rendered-frame collector only; no V3/V4 campaign";
            report.limits=v5Evidence.limits;session.EventRaised+=V5EventRaised;
            Require(Application.version=="0.9.4"&&catalog.dataVersion=="mvp1.17.0","V5 observed version/data");
            Require((Screen.width==1280&&Screen.height==720)||(Screen.width==640&&Screen.height==360),"V5 target resolution");
#if LASTBELL_PLAYTEST_092 && UNITY_STANDALONE_WIN && !UNITY_EDITOR
            v3Evidence.playtestDefine=true;
#endif
            Require(v3Evidence.playtestDefine,"V5 Windows LASTBELL_PLAYTEST_092 build");
            Require(JsonUtility.ToJson(catalog)==JsonUtility.ToJson(JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(output,"expected-catalog.json")))),"V5 loaded catalog equals frozen input");
            Require(session.ExportSave().schemaVersion==9,"V5 live schema9");
            V5References();yield return Rendered();
            if(story12Only)
            {
                v5Evidence.scope="DQA5-002 targeted Stage12 dialogue only; three player IDs at launch resolution";
                v5Evidence.limits="Explicit weak-enemy Stage12 fixtures and synthetic UI raycasts only. Candidate02 unchanged evidence is separate. No campaign rerun, native resume, OS input, audio or full acceptance claim.";
                v5Evidence.pending=new[]{"manual PNG review","physical mouse/keyboard/mobile","all non-Stage12 acceptance criteria"};report.limits=v5Evidence.limits;
                Require(catalog.stories.Single(x=>x.stage==12).lines[0].portraitId=="barun","Stage12 unnamed speaker data uses Barun portrait");
                foreach(string id in new[]{"on","dam","rin"})yield return V5Story(id,12,true);
                yield break;
            }
            if(resume){yield return V5Resume();yield break;}
            bool recovered;var source=new JsonSaveRepository(Path.Combine(output,"legacy-source")).Load(out recovered);
            Require(!recovered&&source!=null&&source.schemaVersion==8,"prior-QA schema8 source loads without backup");
            if(legacyBoot){V5MigrationCheck(source,session,"native-schema8");yield return V5Capture("v5-native-legacy");}
            yield return V5Migrations(source);
            V5Bind(new GameSession(catalog),"new-account-prologue");
            Require(session.Phase==SessionPhase.Prologue,"new v5 account prologue");yield return V5Capture("v5-new-prologue");
            V5Click(p.View.Prologue.Skip);yield return Rendered();Require(session.Phase==SessionPhase.Lobby,"new account skips to selection");
            yield return V5Inn();yield return V5Tutorial();
            foreach(int stage in new[]{3,10,20})yield return V5Recovery(stage);
            foreach(string id in new[]{"on","dam","rin"})yield return V5Skill(id);
            foreach(string id in new[]{"tori","barun","doto","rina","momo"})yield return V5Companion(id);
            foreach(string id in new[]{"on","dam","rin"})foreach(int stage in new[]{2,12,10,20})yield return V5Story(id,stage);
            if(Screen.width==640)yield return V5CompanionCheckpoint();
            Require(v5Resume!=null,"V5 native checkpoint prepared");repository.Save(v5Resume.save);
            File.WriteAllText(Path.Combine(output,"v5-resume-expected.json"),JsonUtility.ToJson(v5Resume,true));
            Require(v3Evidence.writtenFrames==v3Evidence.segments.Sum(x=>x.frames),"all V5 continuous frames written");
        }
        void V5References()
        {
            Require(p.View.Companions&&p.View.Reference.CompanionsTab&&p.View.Reference.GrowthRows.Length==5,"saved v5 inn and five growth rows");
            Require(p.Presentation.Companions&&p.Presentation.Companions.Actors.Length==3,"saved three party actor slots");
            Require(p.Presentation.Overlay.StoryV5,"saved StoryV5 layout enabled");
            var fx=p.Presentation.SkillEffects;Require(fx,"saved SkillPixelEffects reference");fx.ValidateSavedReferences();
            var systems=new[]{fx.AttackParticles,fx.ShieldParticles,fx.SealParticles,fx.SupportParticles};
            Require(systems.Distinct().Count()==4&&fx.GetComponentsInChildren<ParticleSystem>(true).Length==4,"four distinct actual ParticleSystems");
            Require(!fx.Surface.raycastTarget&&!fx.ParticleCamera.enabled&&fx.ParticleCamera.orthographic&&!fx.ParticleCamera.allowHDR&&!fx.ParticleCamera.allowMSAA,"particle camera and nonblocking RawImage");
            foreach(var system in systems)Require(system.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture.filterMode==FilterMode.Point,"Point particle texture: "+system.name);
        }
        void V5Bind(GameSession value,string fixture,bool unpause=true)
        {
            if(session!=null)session.EventRaised-=V5EventRaised;
            v5Fixture=fixture;v5Evidence.fixtures.Add(fixture);Bind(value,false);if(unpause)value.SetPaused(false);session.EventRaised+=V5EventRaised;
        }
        GameSaveData V5Lobby(string id="on",bool allCompanions=false)
        {
            var save=LobbyFixture();save.selectedCharacterId=id;save.comboPracticeSkipped=true;save.gold=20000;save.fragments=20;
            save.characters.Single(x=>x.id==id).statLevels[1]=catalog.growth.maxLevel;
            if(allCompanions)
            {
                save.highestClearedStage=20;
                foreach(var definition in catalog.companions)save.companions.Add(new CompanionProgressData{id=definition.id,level=1});
            }
            return save;
        }
        GameSaveData V5BattleSave(string id,int stage,bool weak=false,string companion=null,bool guided=false)
        {
            var lobby=V5Lobby(id,companion!=null);lobby.comboPracticeSkipped=!guided;
            if(companion!=null)lobby.characters.Single(x=>x.id==id).companionIds.Add(companion);
            var temp=new GameSession(catalog,lobby);Require(temp.StartRun(),"V5 fixture departure "+id);
            var save=temp.ExportSave();var run=save.run;save.phase=SessionPhase.Combat;run.transition=null;run.stage=stage;
            run.patternStep=run.patternIndex=run.sequenceCursor=0;run.randomSequence=Array.Empty<string>();
            run.enemyMaxHpMilli=weak?catalog.GetStage(stage).maxHp*1000L:1000000000L;run.enemyHpMilli=weak?1000:run.enemyMaxHpMilli;
            return save;
        }
        IEnumerator V5Prepare(string id,int stage,string label,bool weak=false,string companion=null,bool guided=false)
        {
            scenario=label;V5Bind(new GameSession(catalog,V5BattleSave(id,stage,weak,companion,guided)),label);
            yield return Rendered();yield return Rendered();
        }
        void V5EventRaised(SessionEvent e)
        {
            v5Evidence.events.Add(new V5Event{scenario=scenario,fixture=v5Fixture,kind=e.Kind.ToString(),skill=e.SkillId,companion=e.CompanionId,service=e.ServiceId,pattern=e.PatternId,
                frame=Time.frameCount,stage=e.Stage,runId=session.ExportSave().run?.runId??0,clockMs=e.TimeMs,realtime=Time.realtimeSinceStartupAsDouble,
                damageMilli=e.DamageMilli,absorbedMilli=e.AbsorbedMilli,recoveryMilli=e.RecoveryMilli,gold=session.Gold});
        }
        void V5Click(Button button)
        {
            var record=new V5Input{scenario=scenario,control=button?button.name:"null",kind="synthetic-click",sequence=++v5Evidence.inputEdges,frame=Time.frameCount,clockBefore=session.ClockMs,eventCountBefore=v5Evidence.events.Count,interactable=button&&button.interactable};
            Click(button);record.clockAfter=session.ClockMs;record.eventCountAfter=v5Evidence.events.Count;v5Inputs.WriteLine(JsonUtility.ToJson(record));
        }
        void V5Pointer(PointerCommand control,bool enabled=true)
        {
            var record=new V5Input{scenario=scenario,control=control?control.name:"null",kind="synthetic-down-up",sequence=++v5Evidence.inputEdges,frame=Time.frameCount,clockBefore=session.ClockMs,eventCountBefore=v5Evidence.events.Count,interactable=control&&control.Interactable};
            V4Pointer(control,enabled);record.clockAfter=session.ClockMs;record.eventCountAfter=v5Evidence.events.Count;v5Inputs.WriteLine(JsonUtility.ToJson(record));
        }
        void V5Advance(double seconds)
        {
            p.TickSession=false;clockMode="manual-v5-gap-no-render";
            while(seconds>0){double step=Math.Min(.05,seconds);session.Tick((float)step);seconds-=step;}
        }
        IEnumerator V5Until(Func<bool> condition,double maxSeconds,string label)
        {
            p.TickSession=false;clockMode="manual-v5-25ms-per-render";int budget=(int)Math.Ceiling(maxSeconds/.025);
            while(!condition()&&budget-->0){session.Tick(.025f);yield return null;}
            Require(condition(),label);yield return Rendered();
        }
        IEnumerator V5Observe(string label,float seconds,bool realtime=true,Action onFrame=null)
        {
            yield return Rendered();yield return Rendered();
            yield return V3Observe(label,seconds,realtime,t=>{clockMode=realtime?"real-frame-v5":"real-frame-v5-static-session";V5Snapshot(label,v3Frames[v3Frames.Count-1].index.file);onFrame?.Invoke();});
        }
        IEnumerator V5Capture(string label)
        {scenario=label;yield return Capture(label);V5Snapshot(label,label+".png");v5Evidence.staticCaptures++;}
        void V5Duration(double actual,double expected,string label)
        {Require(Math.Abs(actual-expected)<=.000001,label+" expected="+expected.ToString("R")+" actual="+actual.ToString("R"));}

        void V5MigrationCheck(GameSaveData before,GameSession loaded,string label)
        {
            var after=loaded.ExportSave();Require(before.schemaVersion==8&&after.schemaVersion==9&&!after.adventureNewAccount,label+" schema8 to9");
            Require(after.gold==before.gold&&after.fragments==before.fragments&&after.selectedCharacterId==before.selectedCharacterId,label+" wallet and character preserved");
            foreach(var old in before.characters)
            {
                var current=after.characters.Single(x=>x.id==old.id);
                Require(current.statLevels.Length==5&&current.statLevels.Take(4).SequenceEqual(old.statLevels.Take(4))&&current.statLevels[4]==0,label+" append recovery0: "+old.id);
                Require(current.companionIds.Count==0&&current.weaponRank==0&&current.reservations.Count==0,label+" migration does not auto-equip/spend: "+old.id);
            }
            Require(after.companionSlots==1,label+" one companion slot");
            if(before.run!=null)
            {
                Require(after.run.stage==before.run.stage&&after.run.playerHpMilli==before.run.playerHpMilli&&after.phase==before.phase,label+" run stage HP phase preserved");
                V5Duration(after.run.skillCooldownRemainingMs,before.run.skillCooldownRemainingMs,label+" cooldown");
                V5Duration(after.run.skillShieldRemainingMs,before.run.skillShieldRemainingMs,label+" shield time");
                if(before.run.story!=null)Require(after.run.story!=null&&after.run.story.sceneId==before.run.story.sceneId&&after.run.story.lineIndex==before.run.story.lineIndex,label+" pending scene/page preserved");
            }
            File.WriteAllText(Path.Combine(output,label+"-migration.json"),JsonUtility.ToJson(after,true));
        }
        IEnumerator V5Migrations(GameSaveData source)
        {
            var loaded=new GameSession(catalog,source);V5MigrationCheck(source,loaded,"v5-prior-qa-file");
            foreach(int stage in new[]{7,15,20})
            {
                var legacy=V5BattleSave("rin",stage,true);legacy.schemaVersion=8;legacy.phase=SessionPhase.StoryDialogue;
                foreach(var c in legacy.characters)c.statLevels=c.statLevels.Take(4).ToArray();
                var run=legacy.run;run.statLevels=run.statLevels.Take(4).ToArray();run.enemyHpMilli=0;run.stageRewardApplied=true;run.enemyHitAtMs=run.attackHitAtMs=-1;
                var definition=catalog.legacyStories.Single(x=>x.stage==stage);
                run.story=new StoryStateData{sceneId=definition.id,lineIndex=Math.Min(1,definition.lines.Length-1),rewardId=run.runId+":"+run.cycle+":"+stage};
                run.skillCooldownRemainingMs=12345.6789;
                string label="v5-synthetic-legacy-"+stage;var repo=new JsonSaveRepository(Path.Combine(output,"fixtures",label));repo.Save(legacy);
                bool recovered;var fromFile=repo.Load(out recovered);Require(!recovered,label+" primary file");
                V5Bind(new GameSession(catalog,fromFile),label);V5MigrationCheck(fromFile,session,label);yield return Rendered();
                yield return V5ReadStory(label);
            }
        }
        IEnumerator V5Inn()
        {
            scenario="v5-inn";V5Bind(new GameSession(catalog,V5Lobby("on",true)),"future-rule-only-all-five-inn");yield return Rendered();
            V5Click(p.View.StartRun);yield return Rendered();Require(p.View.Reference.UpgradeScreen,"v5 inn opened through saved UI");
            int level=session.GetStatLevel(4);long cost=session.GetUpgradeCost(4),gold=session.Gold;
            V5Click(p.View.Reference.GrowthRows[4].Plus);yield return V5Observe("v5-recovery-purchase",.3f,false);
            Require(session.GetStatLevel(4)==level+1&&session.Gold==gold-cost,"recovery growth actual purchase");yield return V5Capture("v5-inn-growth");
            V5Click(p.View.Reference.CompanionsTab);yield return Rendered();var inn=p.View.Companions;
            Require(inn.IsOpen&&inn.Rows.Length==5,"five saved companion rows");yield return V5Capture("v5-inn-party");
            foreach(string id in new[]{"tori","barun","doto","rina","momo"})
            {
                scenario="v5-service-"+id;int index=session.Companions.ToList().FindIndex(x=>x.Id==id);
                // ScrollRect's public position is a labelled viewport fixture; click still raycasts.
                inn.RosterScroll.verticalNormalizedPosition=index<3?1:0;yield return Rendered();V5Click(inn.Rows[index].Select);yield return Rendered();
                var member=session.Companions.Single(x=>x.Id==id);gold=session.Gold;int fragments=session.Fragments;int oldLevel=member.Level;
                V5Click(inn.Upgrade);yield return V5Observe(scenario+"-upgrade",.25f,false);
                Require(session.CompanionLevel(id)==oldLevel+1&&session.Gold==gold-member.UpgradeCost,"companion UI upgrade "+id);
                Require(!session.PurchaseCompanionUpgrade(new CompanionUpgradeQuote()),"invalid companion quote rejected "+id);
                V5Click(inn.Service);yield return V5Capture(scenario+"-modal");gold=session.Gold;fragments=session.Fragments;
                V5Click(inn.Cancel);yield return Rendered();Require(session.Gold==gold&&session.Fragments==fragments,"service cancel spends nothing "+id);
                if(id=="tori")continue;
                V5Click(inn.Service);yield return Rendered();long price=session.ServicePrice(member.ServiceId);int slotCost=session.NextCompanionSlotCost;
                var serviceQuote=session.QuoteService(member.ServiceId);V5Click(inn.Confirm);yield return V5Observe(scenario+"-accepted",.3f,false);
                if(id=="barun")Require(session.CompanionSlots==2&&session.Fragments==fragments-slotCost,"cart purchase uses gems once");
                else
                {
                    Require(session.Gold==gold-price,"service exact gold charge "+id);long paid=session.Gold;
                    Require(!session.PurchaseService(serviceQuote)&&session.Gold==paid,"stale service confirmation rejected "+id);
                    if(id=="doto")Require(session.WeaponRank==1,"weapon rank actual result");
                    else
                    {
                        Require(session.IsServiceReserved(member.ServiceId),"reservation visible "+id);yield return V5Capture(scenario+"-reserved");
                        V5Click(inn.Service);yield return Rendered();V5Click(inn.Confirm);yield return Rendered();
                        Require(!session.IsServiceReserved(member.ServiceId)&&session.Gold==gold,"reservation cancel refunds exact paid amount "+id);
                        Require(!session.CancelService(member.ServiceId)&&session.Gold==gold,"duplicate refund rejected "+id);
                    }
                }
            }
            inn.RosterScroll.verticalNormalizedPosition=1;yield return Rendered();V5Click(inn.Rows[0].Select);yield return Rendered();
            V5Click(inn.Party);yield return Rendered();V5Click(inn.Confirm);yield return Rendered();Require(session.EquippedCompanionIds.SequenceEqual(new[]{session.Companions[0].Id}),"party confirmation equips selected companion");
            yield return V5Capture("v5-inn-equipped");
        }
        IEnumerator V5Tutorial()
        {
            yield return V5Prepare("on",4,"v5-guided-natural-entry",true,"tori",true);V5Pointer(p.View.Attack);
            yield return V5Until(()=>session.Phase==SessionPhase.ComboHint,15,"Stage4 clear arrives at Stage5 guided intro");yield return V5Capture("v5-guided-intro");
            V5Click(p.View.ModalConfirm);yield return V5Until(()=>session.ComboPractice.AwaitingInput,4,"guided first input hold");
            var before=session.ExportSave();double clock=session.ClockMs;int eventStart=v5Evidence.events.Count;
            V5Pointer(p.View.Dodge[(int)DodgeDirection.Down],false);V5Pointer(p.View.Attack,false);Require(!session.UseSkill(),"guided skill command rejected");
            V5Advance(2);yield return V5Observe("v5-guided-hold",.2f,false);
            var after=session.ExportSave();Require(session.ClockMs==clock&&session.ComboPractice.Step==0&&session.ComboPractice.AwaitingInput,"wrong/disabled input and hold time do not progress");
            Require(before.run.playerHpMilli==after.run.playerHpMilli&&before.run.enemyHpMilli==after.run.enemyHpMilli&&before.gold==after.gold&&before.run.skillCooldownRemainingMs==after.run.skillCooldownRemainingMs&&JsonUtility.ToJson(before.run.companions[0])==JsonUtility.ToJson(after.run.companions[0]),"guided hold preserves HP cooldown companions and rewards");
            if(Screen.width==1280)v5Resume=new V5ResumeExpectation{kind="guided",save=after};
            V5Pointer(p.View.Dodge[(int)DodgeDirection.Up]);yield return V5Until(()=>session.ComboPractice.AwaitingInput&&session.ComboPractice.Step==1,2,"guided second distinct input hold");
            yield return V5Capture("v5-guided-second-hold");V5Pointer(p.View.Dodge[(int)DodgeDirection.Down]);
            yield return V5Observe("v5-guided-completed",.5f);
            Require(session.ExportSave().comboPracticeCompleted&&v5Evidence.events.Skip(eventStart).Count(x=>x.kind=="ComboPracticeCompleted")==1,"two correct UI edges complete guided once");
            Require(!v5Evidence.events.Skip(eventStart).Any(x=>x.kind=="EnemyDamaged"||x.kind=="PlayerDamaged"||x.kind=="PlayerRecovered"||x.kind=="CompanionDamaged"),"guided input causes no combat damage/heal/support reward");
        }
        IEnumerator V5Recovery(int stage)
        {
            string label="v5-recovery-stage-"+stage;var save=V5BattleSave("on",stage,true);save.run.playerHpMilli=Math.Max(1,save.run.combatStats.maxHpMilli-100000);
            V5Bind(new GameSession(catalog,save),label);scenario=label;yield return Rendered();var before=session.ExportSave();int start=v5Evidence.events.Count;
            V5Pointer(p.View.Attack);yield return V5Observe(label,.8f);var after=session.ExportSave();
            long expected=Math.Min(before.run.combatStats.maxHpMilli,before.run.playerHpMilli+before.run.combatStats.recovery*1000L);
            Require(after.run.playerHpMilli==expected&&v5Evidence.events.Skip(start).Count(x=>x.kind=="PlayerRecovered")==1,"one actual victory recovery at stage "+stage);
            Require(v5Evidence.events.Skip(start).Count(x=>x.kind=="EnemyDefeated"&&x.stage==stage)==1,"recovery follows one committed kill "+stage);Save();bool backup;var restored=repository.Load(out backup);
            V5Bind(new GameSession(catalog,restored),label+"-file-restored");yield return Rendered();Require(!backup&&session.ExportSave().run.playerHpMilli==expected,"restoring processed kill does not heal again "+stage);
            yield return V5Capture(label+"-restored");
        }
        IEnumerator V5Skill(string id)
        {
            string label="v5-skill-"+id;yield return V5Prepare(id,11,label);
            if(id=="rin")
            {
                var save=session.ExportSave();save.run.stagger=Math.Max(0,catalog.combat.staggerThreshold-session.CurrentSkill.staggerGain);
                V5Bind(new GameSession(catalog,save),label+"-near-threshold-fixture");yield return Rendered();
            }
            int start=v5Evidence.events.Count,peak=0;V5Pointer(p.View.Skill);Require(!session.UseSkill(),"duplicate skill rejected "+id);
            yield return V5Observe(label+"-contact",.65f,true,()=>peak=Math.Max(peak,p.Presentation.SkillEffects.AttackParticles.particleCount));
            Require(peak>0&&v5Evidence.events.Skip(start).Count(x=>x.kind=="EnemyDamaged"&&x.skill==session.CurrentSkill.id)==1,"actual skill damage and particles "+id);
            V5Click(p.View.Pause);yield return Rendered();double clock=session.ClockMs,cool=session.SkillCooldownRemainingMs,shield=session.SkillShieldRemainingMs;
            string particles=JsonUtility.ToJson(V5Particle(p.Presentation.SkillEffects.ShieldParticles));V5Advance(2);yield return V5Observe(label+"-paused",.2f,false);
            Require(session.ClockMs==clock&&session.SkillCooldownRemainingMs==cool&&session.SkillShieldRemainingMs==shield,"combat/skill/shield freeze "+id);
            Require(particles==JsonUtility.ToJson(V5Particle(p.Presentation.SkillEffects.ShieldParticles)),"shield particle positions freeze "+id);V5Click(p.View.Resume);yield return Rendered();
            if(id=="dam")
            {
                Require(session.SkillShieldMilli>0&&p.Presentation.SkillEffects.ShieldParticles.particleCount>0,"actual shield displayed");
                // Client evidence owns the full3000ms rule. Isolate expiry before the next incoming hit.
                var expiry=session.ExportSave();expiry.run.skillShieldRemainingMs=180;V5Bind(new GameSession(catalog,expiry),"explicit-last180ms-shield-expiry-fixture");yield return Rendered();start=v5Evidence.events.Count;
                yield return V5Observe(label+"-expiry",.4f);
                Require(session.SkillShieldMilli==0&&session.SkillShieldRemainingMs==0&&p.Presentation.SkillEffects.ShieldParticles.particleCount==0&&!v5Evidence.events.Skip(start).Any(x=>x.kind=="SkillShieldAbsorbed"),"shield expiry clears particles without absorption");
                yield return V5Prepare("dam",11,"v5-shield-absorption");V5Pointer(p.View.Skill);yield return V5Observe("v5-shield-contact",.4f);
                var save=session.ExportSave();save.run.skillShieldMilli=1;V5Bind(new GameSession(catalog,save),"explicit-one-milli-shield-absorption-fixture");yield return Rendered();start=v5Evidence.events.Count;
                yield return V5Observe("v5-shield-absorbed",2.2f);
                Require(v5Evidence.events.Skip(start).Any(x=>x.kind=="SkillShieldAbsorbed"&&x.absorbedMilli==1)&&session.SkillShieldMilli==0,"actual impact absorbs final shield milli");
            }
            if(id=="rin")
            {
                Require(session.IsStunReady&&!session.IsEnemyStunned&&p.Presentation.SkillEffects.SealParticles.particleCount>0,"Miel ready seal is not stunned");
                V5Pointer(p.View.Attack);yield return V5Observe(label+"-stunned",.6f);
                Require(session.IsEnemyStunned&&p.Presentation.SkillEffects.SealParticles.particleCount==15,"actual stun replaces ready seal with fifteen star particles");
                Require(p.View.StunStars.All(x=>!x.gameObject.activeInHierarchy),"old UI stars do not duplicate particle stun");yield return V5Capture(label+"-stunned-ui");
                V5Advance(Math.Max(0,session.StunSecondsRemaining-.12));yield return V5Observe(label+"-stun-ended",.3f);Require(!session.IsEnemyStunned&&p.Presentation.SkillEffects.SealParticles.particleCount==0,"stun ends on authoritative clock and clears stars");
            }
        }
        IEnumerator V5Companion(string id)
        {
            string label="v5-companion-"+id;yield return V5Prepare("on",11,label,false,id);v5Fixture="explicit-solo-companion-high-HP-rule-fixture-"+id;
            int start=v5Evidence.events.Count;double first=session.ClockMs+session.RunCompanions[0].RemainingMs;int period=session.RunCompanions[0].PeriodMs;
            V5Click(p.View.Pause);yield return Rendered();double remaining=session.RunCompanions[0].RemainingMs,clock=session.ClockMs;V5Advance(3);
            yield return V5Capture(label+"-paused");Require(session.ClockMs==clock&&session.RunCompanions[0].RemainingMs==remaining,"companion pause freezes "+id);V5Click(p.View.Resume);yield return Rendered();
            for(int hit=0;hit<3;hit++)
            {
                V5Advance(Math.Max(0,(session.RunCompanions[0].RemainingMs-260)/1000));yield return V5Observe(label+"-hit-"+hit,.8f);
                Require(p.Presentation.Companions.FindActor(id)&&p.Presentation.Companions.FindActor(id).gameObject.activeInHierarchy,"saved companion actor visible "+id);
            }
            var hits=v5Evidence.events.Skip(start).Where(x=>x.kind=="CompanionDamaged"&&x.companion==id).ToArray();
            Require(hits.Length==3,"three no-player-input companion contacts "+id);V5Duration(hits[0].clockMs,first,"first companion period "+id);
            for(int i=1;i<hits.Length;i++)V5Duration(hits[i].clockMs-hits[i-1].clockMs,period,"no extra windup added to companion period "+id+"/"+i);
            Require(!v5Evidence.events.Skip(start).Any(x=>x.kind=="AttackStarted"||x.kind=="EnemyStunned"),"support does not trigger player attack/stun "+id);
        }
        IEnumerator V5Story(string id,int stage,bool verifyBarun=false)
        {
            string label="v5-story-"+id+"-"+stage;yield return V5Prepare(id,stage,label,true);V5Pointer(p.View.Attack);
            yield return V5Until(()=>session.Phase==SessionPhase.StoryDialogue,4,"new story reached "+label);
            if(verifyBarun)
            {
                Require(session.Story.PortraitId=="barun"&&session.Story.Speaker=="???","Stage12 first unnamed speaker resolves to Barun "+id);
                var overlay=p.Presentation.Overlay;var barun=overlay.Portraits.Single(x=>x.Id=="barun");var unknown=overlay.Portraits.Single(x=>x.Id=="unknown");
                Require(barun.Sprite&&barun.Sprite!=unknown.Sprite&&overlay.StoryPortrait.gameObject.activeInHierarchy&&overlay.StoryPortrait.sprite==barun.Sprite,"Stage12 actual saved Barun sprite replaces fox silhouette "+id);
            }
            yield return V5ReadStory(label,verifyBarun);
            if(stage==2)Require(session.CompanionUnlocked("tori")&&session.EquippedCompanionIds.Contains("tori"),"first Tori joins empty first slot "+id);
            if(stage==12)Require(session.CompanionUnlocked("barun"),"first Barun unlocked "+id);
        }
        IEnumerator V5ReadStory(string label,bool verifySprite=false)
        {
            int page=0;while(session.Phase==SessionPhase.StoryDialogue&&page<40)
            {
                yield return V5Capture(label+"-page-"+(page++).ToString("D2"));var overlay=p.Presentation.Overlay;var story=session.Story;
                bool player=new[]{"on","dam","rin"}.Contains(story.PortraitId);Require(overlay.PlayerSpeaking==player,"story speaker side follows resolved role");
                Require(overlay.StorySpeaker.text==story.Speaker,"story speaker renders resolved name");
                if(verifySprite)Require(overlay.StoryPortrait.gameObject.activeInHierarchy&&overlay.StoryPortrait.sprite==overlay.Portraits.Single(x=>x.Id==story.PortraitId).Sprite,"target dialogue actual sprite matches resolved portrait "+label+"/"+(page-1));
                if(overlay.StoryPortrait.gameObject.activeInHierarchy)
                {
                    Rect bounds=ScreenBounds(overlay.StoryPortrait.rectTransform),safe=ScreenBounds(overlay.rectTransform);
                    Require(player?bounds.center.x<safe.center.x:bounds.center.x>safe.center.x,"portrait on expected side");
                }
                if(story.AwaitingCompanionChoice)V5Click(overlay.ButtonLater);else V5Click(overlay.ButtonNext);
                yield return Rendered();
            }
            Require(session.Phase!=SessionPhase.StoryDialogue,"bounded story reaches completion "+label);
        }
        IEnumerator V5CompanionCheckpoint()
        {
            var save=V5BattleSave("dam",11,false,"tori");save.run.companions[0].remainingMs=800;
            V5Bind(new GameSession(catalog,save),"explicit-native-resume-timer-fixture");yield return Rendered();V5Pointer(p.View.Skill);
            yield return V5Observe("v5-resume-shield-setup",.4f);V5Advance(Math.Max(0,(session.RunCompanions[0].RemainingMs-100)/1000));yield return Rendered();
            Require(session.SkillShieldMilli>0&&session.RunCompanions[0].IsWindingUp,"checkpoint has shield and pending companion");
            v5Resume=new V5ResumeExpectation{kind="companion-shield",save=session.ExportSave()};yield return V5Capture("v5-checkpoint-companion-shield");
        }
        IEnumerator V5Resume()
        {
            var expected=JsonUtility.FromJson<V5ResumeExpectation>(File.ReadAllText(Path.Combine(output,"v5-resume-expected.json")));var before=expected.save;var after=session.ExportSave();
            v5Fixture="native-v5-resume-"+expected.kind;Require(after.run!=null&&after.run.stage==before.run.stage&&after.run.playerHpMilli==before.run.playerHpMilli&&after.gold==before.gold&&after.fragments==before.fragments,"native restart preserves stage HP wallet");
            V5Duration(after.run.skillCooldownRemainingMs,before.run.skillCooldownRemainingMs,"native skill cooldown");
            V5Duration(after.run.skillShieldRemainingMs,before.run.skillShieldRemainingMs,"native shield remaining");
            yield return V5Capture("v5-native-resume-initial");Require(p.Presentation.SkillEffects.AttackParticles.particleCount==0&&p.Presentation.SkillEffects.SupportParticles.particleCount==0,"native restart does not replay old bursts");
            Require(session.IsPaused,"native combat restoration waits paused");
            if(expected.kind=="guided")
            {
                Require(session.ComboPractice.Active&&session.ComboPractice.Step==0&&!session.ComboPractice.AwaitingInput,"interrupted guided restarts first sequence");
                V5Click(p.View.Resume);yield return V5Until(()=>session.ComboPractice.AwaitingInput,4,"resumed first guided hold");yield return V5Capture("v5-native-guided-first-hold");
                Require(!session.ExportSave().comboPracticeCompleted,"restart did not award tutorial completion");
            }
            else if(expected.kind=="companion-shield")
            {
                Require(after.run.skillShieldMilli==before.run.skillShieldMilli&&p.Presentation.SkillEffects.ShieldParticles.particleCount>0,"native current shield rendered");
                // Final Client contract: same enemy reload preserves remaining time and windup.
                // The350ms minimum applies to a NEW enemy, not to this native resume.
                double remaining=before.run.companions[0].remainingMs;
                V5Duration(session.RunCompanions[0].RemainingMs,remaining,"native same-enemy pending contact remaining");Require(session.RunCompanions[0].IsWindingUp==before.run.companions[0].windingUp,"native same-enemy windup preserved");
                int start=v5Evidence.events.Count;double clock=session.ClockMs;V5Click(p.View.Resume);yield return V5Observe("v5-native-companion-contact",.9f);
                var hits=v5Evidence.events.Skip(start).Where(x=>x.kind=="CompanionDamaged").ToArray();Require(hits.Length==1,"one rescheduled native companion contact");
                V5Duration(hits[0].clockMs-clock,remaining,"native contact uses saved remaining time");Require(!v5Evidence.events.Skip(start).Any(x=>x.kind=="EnemyDamaged"||x.kind=="PlayerRecovered"),"old skill contact/heal not replayed");
            }
            else throw new InvalidDataException("Unknown v5 native checkpoint kind.");
        }
        V5Particles V5Particle(ParticleSystem system)
        {
            var data=new V5Particles{name=system.name,path=system.transform.parent.name+"/"+system.name,instance=system.GetInstanceID()};
            var renderer=system.GetComponent<ParticleSystemRenderer>();var texture=renderer.sharedMaterial.mainTexture;data.texture=texture.name;data.filter=texture.filterMode.ToString();
            var buffer=new ParticleSystem.Particle[system.main.maxParticles];data.count=system.GetParticles(buffer);
            for(int i=0;i<data.count;i++)data.dots.Add(new V5Dot{position=buffer[i].position,size=buffer[i].startSize,life=buffer[i].remainingLifetime,color=buffer[i].startColor});return data;
        }
        V3ActorFrame V5Actor(ActorView actor)=>actor&&actor.Body&&actor.Body.sprite&&actor.GetClip(actor.CurrentMotion)!=null?V3Actor(actor):null;
        void V5Snapshot(string label,string file=null)
        {
            var save=session.ExportSave();var run=save.run;var story=session.Story;var practice=session.ComboPractice;var overlay=p.Presentation.Overlay;var fx=p.Presentation.SkillEffects;
            var value=new V5State{tag=label,file=file,fixture=v5Fixture,clockMode=clockMode,frame=Time.frameCount,realtime=Time.realtimeSinceStartupAsDouble,
                schema=save.schemaVersion,stage=session.Stage,runId=run?.runId??0,character=session.CurrentCharacterId,phase=session.Phase.ToString(),clockMs=session.ClockMs,
                hpMilli=run?.playerHpMilli??0,enemyHpMilli=run?.enemyHpMilli??0,gold=session.Gold,gems=session.Fragments,recovery=session.PlayerStats.recovery,slots=session.CompanionSlots,weaponRank=session.WeaponRank,
                cooldownMs=session.SkillCooldownRemainingMs,shieldMs=session.SkillShieldRemainingMs,shieldMilli=session.SkillShieldMilli,stagger=session.Stagger,stunReady=session.IsStunReady,stunned=session.IsEnemyStunned,immune=session.IsStaggerImmune,stunSeconds=session.StunSecondsRemaining,
                paused=session.IsPaused,suspended=session.CombatSuspended,practiceActive=practice.Active,practiceHold=practice.AwaitingInput,practiceStep=practice.Step,requiredDirection=practice.RequiredDirection.ToString(),practiceCompleted=save.comboPracticeCompleted,practiceSkipped=save.comboPracticeSkipped,
                scene=story?.SceneId,line=story?.LineIndex??-1,lineCount=story?.LineCount??0,speaker=story?.Speaker,text=story?.Text,portrait=story?.PortraitId,playerSpeaking=overlay.PlayerSpeaking,
                portraitBounds=ScreenBounds(overlay.StoryPortrait.rectTransform),speakerBounds=ScreenBounds(overlay.StorySpeaker.rectTransform),bodyBounds=ScreenBounds(overlay.StoryText.rectTransform),speakerFont=overlay.StorySpeaker.fontSize,bodyFont=overlay.StoryText.fontSize,
                rawBounds=ScreenBounds(fx.Root),rawVisible=fx.Surface.enabled,rawRaycast=fx.Surface.raycastTarget,rtStep=fx.ScreenPixelsPerTexel,player=V5Actor(p.PlayerActor),enemy=V5Actor(p.EnemyActor)};
            if(fx.Output)
            {
                var rt=fx.Output;value.rtWidth=rt.width;value.rtHeight=rt.height;value.rtAA=rt.antiAliasing;value.rtFilter=rt.filterMode.ToString();value.rtMips=rt.useMipMap;value.textureBound=fx.Surface.texture==rt&&fx.ParticleCamera.targetTexture==rt;
                Require(rt.filterMode==FilterMode.Point&&!rt.useMipMap&&rt.antiAliasing==1&&value.textureBound,"V5 actual Point RT binding");
            }
            foreach(var system in new[]{fx.AttackParticles,fx.ShieldParticles,fx.SealParticles,fx.SupportParticles})value.particles.Add(V5Particle(system));
            foreach(var member in session.RunCompanions)value.members.Add(new V5Member{id=member.Id,slot=member.Slot,level=member.Level,periodMs=member.PeriodMs,remainingMs=member.RemainingMs,damageMilli=member.DamageMilli,windingUp=member.IsWindingUp,actor=V5Actor(p.Presentation.Companions.FindActor(member.Id))});
            foreach(var threat in session.BellThreats)value.threats.Add(new V5Threat{pattern=threat.PatternId,direction=threat.Direction.ToString(),sequenceIndex=threat.SequenceIndex,cueAtMs=threat.CueAtMs,cueEndMs=threat.CueEndMs,impactAtMs=threat.ImpactAtMs,resolved=threat.Resolved});
            v5States.WriteLine(JsonUtility.ToJson(value));v5Evidence.stateSamples++;
        }
        void ReleaseV5Frames()
        {
            if(!v5Active)return;v5Active=false;if(p)p.TickSession=false;if(session!=null)session.EventRaised-=V5EventRaised;
            foreach(var frame in v3Frames)if(frame.pixels)Destroy(frame.pixels);v3Frames.Clear();v3Index?.Dispose();v3Index=null;v5States?.Dispose();v5Inputs?.Dispose();
            if(Directory.Exists(output)){File.WriteAllText(Path.Combine(output,"v5-evidence.json"),JsonUtility.ToJson(v5Evidence,true));File.WriteAllText(Path.Combine(output,"v5-frame-evidence.json"),JsonUtility.ToJson(v3Evidence,true));}
        }
    }
}
#endif
