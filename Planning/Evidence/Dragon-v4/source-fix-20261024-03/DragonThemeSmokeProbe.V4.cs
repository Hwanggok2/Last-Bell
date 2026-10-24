#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastBell
{
    public sealed partial class DragonThemeSmokeProbe
    {
        [Serializable] sealed class V4Evidence
        {
            public string scope="v0.9.3 changed-scope observation",visualReview="NOT_RUN",audioListening="NOT_RUN",osInput="DEFERRED",userDefaultProfile="NOT_ACCESSED";
            public string coreTests="Client-owned; reuse handed-over results, not rerun by this probe";
            public string audioLimits="Session trigger events and audio bindings are observed. Source-filter PCM is optional engine output, not device loopback. Exact PlayOneShot voice count is not exposed by the current API; hashes/events do not certify listening or no overlap.";
            public List<V4Event> events=new List<V4Event>();
            public List<V4Audio> audio=new List<V4Audio>();
            public List<V4DurationComparison> restoredDurations=new List<V4DurationComparison>();
        }
        [Serializable] sealed class V4DurationComparison
        {public string label;public double expectedMs,actualMs,deltaMs,toleranceMs=0.000001;public bool passed;}
        [Serializable] sealed class V4Event
        {
            public string scenario,kind,skillId,pattern,expectedDispatchClip;public int frame,stage;public double combatMs,realtime;
            public long damageMilli,absorbedMilli;public bool muted;
        }
        [Serializable] sealed class V4State
        {
            public string tag,file,character,skill,phase,speaker,text,portraitId,portraitSprite,skillLabel,cooldownLabel;
            public int frame,stage,line,lineCount;public double combatMs,realtime,cooldownMs,shieldMs;public long shieldMilli;
            public bool paused,canUse,skillVisible,skillInteractable,shieldVisible,portraitVisible,muted,sfxPlaying;
            public Rect skillBounds,attackBounds,barBody,speakerBounds,portraitBounds;public Color barFill;
            public V3ActorFrame player,enemy;
        }
        [Serializable] sealed class V4Audio
        {
            public string scenario,touchClip,contactClip,file,captureStatus,captureReason,voiceMultiplicity="NOT_OBSERVABLE",listening="NOT_RUN";
            public bool muted;public int attackEvents,contactEvents,sampleRate,channels,samples,audioSourceCount;public float touchLength,contactLength;
        }
        [Serializable] sealed class V4ResumeExpectation
        {
            public GameSaveData save;public string text,speaker,portraitId;
        }
        readonly V4Evidence v4Evidence=new V4Evidence();
        StreamWriter v4States;bool v4Active;V4ResumeExpectation v4Resume;
        DragonV4AudioProbeTap v4AudioTap;

        IEnumerator DragonV4(bool resume,bool continuation=false)
        {
            v4Active=true;v4States=new StreamWriter(new FileStream(Path.Combine(output,"v4-state.jsonl"),FileMode.CreateNew));
            report.limits="V4 synthetic UI and valid changed-scope fixtures. Core suite is Client-owned. Explicit isolated save override only; no default-profile access, OS input, mobile or normal-speed performance certification. Visual review and audio listening are separate manual statuses.";
            Require(Application.version=="0.9.3"&&catalog.dataVersion=="mvp1.16.0","V4 version 0.9.3 / mvp1.16.0");
            Require((Screen.width==1280&&Screen.height==720)||(Screen.width==640&&Screen.height==360),"V4 target resolution");
#if LASTBELL_PLAYTEST_092 && UNITY_STANDALONE_WIN && !UNITY_EDITOR
            v3Evidence.playtestDefine=true;
#endif
            Require(v3Evidence.playtestDefine,"V4 retains Windows LASTBELL_PLAYTEST_092 selector define");
            if(resume){yield return V4Resume();yield break;}
            v3Index=new StreamWriter(new FileStream(Path.Combine(output,"frames.jsonl"),FileMode.CreateNew));
            v3Evidence.coverage="V4 shared rendered-frame collector; no V3 campaign executed";
            var expected=JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(output,"expected-catalog.json")));
            Require(JsonUtility.ToJson(catalog)==JsonUtility.ToJson(expected),"loaded V4 catalog equals handed-over generated catalog");
            var baseline=JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(output,"timing-baseline-catalog.json")));
            Require(JsonUtility.ToJson(catalog.combat)==JsonUtility.ToJson(baseline.combat),"existing combat timing and rules unchanged from observed 0.9.2 baseline");
            foreach(int stage in new[]{19,20})
                Require(JsonUtility.ToJson(catalog.GetStage(stage))==JsonUtility.ToJson(baseline.stages.Single(x=>x.number==stage)),"existing stage timing/geometry data unchanged: "+stage);
            var dash=catalog.GetStage(11);var oldDash=baseline.stages.Single(x=>x.number==11);
            Require(dash.randomPatterns.SequenceEqual(new[]{"frontal_dash"})&&dash.patternSteps.All(x=>x.danger=="frontal_dash"&&x.requiredDodge=="back"),"stage11 authorized frontal dash pool");
            Require(dash.maxHp==oldDash.maxHp&&dash.damage==oldDash.damage&&dash.sequenceWeights.SequenceEqual(oldDash.sequenceWeights)&&dash.patternSteps.Length==oldDash.patternSteps.Length&&
                dash.patternSteps.Zip(oldDash.patternSteps,(a,b)=>a.telegraphMs==b.telegraphMs&&a.intervalMs==b.intervalMs&&a.comboStep==b.comboStep).All(x=>x),"stage11 retains timing and balance despite authorized pattern replacement");
            Require(session.Phase==SessionPhase.Prologue,"fresh explicit V4 profile starts prologue");
            if(continuation)
            {
                v4Evidence.scope="V4 continuation: prior Daren save restore/shield then unrun 1280 tail; passed Luka/roster segments adopted separately";
                yield return V4DarenContinuation();yield return V4Character("rin");yield return V4Skill("rin");
            }
            else
            {
                yield return V4Capture("v4-fresh-prologue");
                foreach(string id in new[]{"on","dam","rin"}){yield return V4Character(id);yield return V4Skill(id);}
            }
            yield return V4Hud();
            foreach(int stage in new[]{11,19,20})yield return V4EnemyCombo(stage);
            foreach(string id in new[]{"on","dam","rin"})
                foreach(int stage in new[]{2,7,10,15,20})yield return V4Story(id,stage);
            yield return V4AudioCase(false);yield return V4AudioCase(true);
            Require(v4Resume!=null,"native resume checkpoint prepared independently of later fixtures");
            repository.Save(v4Resume.save);
            File.WriteAllText(Path.Combine(output,"v4-resume-expected.json"),JsonUtility.ToJson(v4Resume,true));
            Require(v3Evidence.writtenFrames==v3Evidence.segments.Sum(x=>x.frames),"all V4 continuous captures written");
        }
        void V4Bind(GameSession value,bool unpause=true)
        {
            if(session!=null)session.EventRaised-=V4EventRaised;
            Bind(value,false);if(unpause)value.SetPaused(false);session.EventRaised+=V4EventRaised;
        }
        void V4EventRaised(SessionEvent e)
        {
            var audio=p.Presentation.Audio;AudioClip clip=null;
            if(audio)clip=e.Kind==SessionEventKind.EnemyDamaged?audio.Swing:e.Kind==SessionEventKind.PlayerDamaged?audio.Hit:null;
            v4Evidence.events.Add(new V4Event{scenario=scenario,kind=e.Kind.ToString(),skillId=e.SkillId,pattern=e.PatternId,
                frame=Time.frameCount,stage=e.Stage,combatMs=e.TimeMs,realtime=Time.realtimeSinceStartupAsDouble,
                damageMilli=e.DamageMilli,absorbedMilli=e.AbsorbedMilli,expectedDispatchClip=clip?clip.name:null,muted=audio&&audio.Muted});
        }
        IEnumerator V4Prepare(string id,int stage,bool weak=false)
        {
            var lobby=LobbyFixture();lobby.selectedCharacterId=id;
            lobby.characters.Single(x=>x.id==id).statLevels[1]=catalog.growth.maxLevel;
            var temp=new GameSession(catalog,lobby);Require(temp.StartRun(),"V4 valid fixture starts "+id);
            var save=temp.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;
            save.run.patternStep=save.run.patternIndex=save.run.sequenceCursor=0;save.run.randomSequence=new string[0];
            save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;save.run.enemyHpMilli=weak?1000:save.run.enemyMaxHpMilli;
            V4Bind(new GameSession(catalog,save));yield return new WaitForSecondsRealtime(.7f);yield return Rendered();
        }
        IEnumerator V4Character(string id)
        {
            scenario="v4-select-"+id;V4Bind(new GameSession(catalog,LobbyFixture()));yield return Rendered();
            int index=Array.FindIndex(catalog.characters,x=>x.id==id);var ch=catalog.GetCharacter(id);
            Require(index>=0&&ch.availableInMvp&&catalog.FindSkill(id)!=null,"V4 character available with skill: "+id);
            Click(p.View.Characters[index]);yield return V4Capture(scenario+"-preview");
            Require(p.View.StartRun.interactable&&p.View.CharacterName.text==ch.displayName.Split('·')[0].Trim(),"preview permits selection: "+id);
            Click(p.View.StartRun);yield return Rendered();Require(session.CurrentCharacterId==id,"actual selection: "+id);
            Click(p.View.StartRun);session.Tick(catalog.transitions.arrivalMs/1000f+.01f);yield return V4Capture(scenario+"-departure");
            Require(session.Phase==SessionPhase.Combat&&session.SkillCooldownRemainingMs==0&&session.CanUseSkill,"new departure is skill-ready: "+id);
            Require(p.PlayerActor.ArtId==ch.artId&&p.View.SkillLabel.text==session.CurrentSkill.displayName,"selected actor and skill label: "+id);
        }
        void V4Pointer(PointerCommand control,bool requireInteractable=true)
        {
            Require(control&&control.isActiveAndEnabled&&(!requireInteractable||control.Interactable),"V4 pointer control ready");
            var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=ScreenBounds((RectTransform)control.transform).center};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject)==control.gameObject,"V4 raycast reaches "+control.name);
            ExecuteEvents.Execute(control.gameObject,data,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(control.gameObject,data,ExecuteEvents.pointerUpHandler);
        }
        IEnumerator V4Skill(string id)
        {
            scenario="v4-skill-"+id;yield return V4Prepare(id,11);
            var skill=session.CurrentSkill;Require(skill.cooldownMs==20000,"twenty-second skill: "+id);
            yield return V4Capture(scenario+"-ready");V4SkillLayout();yield return Rendered();
            int start=v4Evidence.events.Count;V4Pointer(p.View.Skill);
            Require(session.SkillCooldownRemainingMs==20000,"accepted skill starts full cooldown: "+id);
            var before=JsonUtility.ToJson(session.ExportSave());
            Require(!session.UseSkill()&&before==JsonUtility.ToJson(session.ExportSave()),"duplicate skill rejected without state consumption: "+id);
            yield return V4Observe(scenario+"-contact",.7f);
            var emitted=v4Evidence.events.Skip(start).ToArray();
            Require(emitted.Count(x=>x.kind=="AttackStarted"&&x.skillId==skill.id)==1&&emitted.Count(x=>x.kind=="EnemyDamaged"&&x.skillId==skill.id)==1,"one accepted skill and one contact event: "+id);
            Require(session.SkillCooldownRemainingMs>0&&session.SkillCooldownRemainingMs<20000,"cooldown advances in actual combat: "+id);
            if(skill.shieldMaxHpPercent>0)Require(session.SkillShieldMilli>0&&session.SkillShieldRemainingMs>0,"shield appears on actual skill contact");
            if(skill.staggerGain>0)Require(session.Stagger>0||session.IsEnemyStunned,"stagger effect appears on actual skill contact");
            double cooldown=session.SkillCooldownRemainingMs,shield=session.SkillShieldRemainingMs;
            session.SetPaused(true);session.Tick(25);Require(!session.UseSkill()&&session.SkillCooldownRemainingMs==cooldown&&session.SkillShieldRemainingMs==shield,"paused skill and shield clocks frozen: "+id);
            yield return V4Capture(scenario+"-paused");session.SetPaused(false);
            Save();bool recovered;var loaded=repository.Load(out recovered);V4Bind(new GameSession(catalog,loaded));yield return V4Capture(scenario+"-file-restored");
            Require(!recovered&&session.CurrentCharacterId==id,"file restore preserves selected character without backup: "+id);
            V4RestoredDuration(session.SkillCooldownRemainingMs,cooldown,"file cooldown: "+id);
            V4RestoredDuration(session.SkillShieldRemainingMs,shield,"file shield: "+id);
            if(skill.shieldMaxHpPercent>0)yield return V4Shield();
            yield return V4LastSecondUi(id);
        }
        void V4RestoredDuration(double actual,double expected,string label)
        {
            var comparison=new V4DurationComparison{label=label,actualMs=actual,expectedMs=expected,deltaMs=actual-expected};
            comparison.passed=Math.Abs(comparison.deltaMs)<=comparison.toleranceMs;v4Evidence.restoredDurations.Add(comparison);
            Require(comparison.passed,"serialized duration within 0.000001ms: "+label+" expected="+expected.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" actual="+actual.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
        }
        IEnumerator V4DarenContinuation()
        {
            scenario="v4-dam-restore-continuation";
            var prior=new JsonSaveRepository(Path.Combine(output,"continuation-source"));bool recovered;
            var loaded=prior.Load(out recovered);
            Require(!recovered&&loaded!=null&&loaded.selectedCharacterId=="dam"&&loaded.phase==SessionPhase.Combat&&loaded.run.skillShieldMilli>0,"prior failed QA save is the Daren restore fixture");
            V4Bind(new GameSession(catalog,loaded));yield return V4Capture("v4-dam-continuation-before-save");
            double cooldown=session.SkillCooldownRemainingMs,shield=session.SkillShieldRemainingMs;long amount=session.SkillShieldMilli;
            Save();V4Bind(new GameSession(catalog,repository.Load(out recovered)));yield return V4Capture("v4-dam-continuation-file-restored");
            Require(!recovered&&session.CurrentCharacterId=="dam"&&session.SkillShieldMilli==amount,"continued Daren restore identity and integer shield amount");
            V4RestoredDuration(session.SkillCooldownRemainingMs,cooldown,"continuation file cooldown: dam");
            V4RestoredDuration(session.SkillShieldRemainingMs,shield,"continuation file shield: dam");
            yield return V4Shield();yield return V4LastSecondUi("dam");
        }
        IEnumerator V4LastSecondUi(string id)
        {
            // A declared last-second fixture checks rendered rounding; Client owns the full Core boundary suite.
            var last=session.ExportSave();last.run.skillCooldownRemainingMs=1000;last.run.skillShieldRemainingMs=0;last.run.skillShieldMilli=0;
            V4Bind(new GameSession(catalog,last));yield return V4Capture("v4-skill-"+id+"-last-second-fixture");
            Require(p.View.SkillCooldown.text=="1초"&&!p.View.Skill.Interactable,"last-second UI blocks skill: "+id);
            session.Tick(1);yield return V4Capture("v4-skill-"+id+"-ready-again");
            Require(session.CanUseSkill&&p.View.Skill.Interactable&&p.View.SkillCooldown.text=="사용","skill UI returns ready: "+id);
        }
        IEnumerator V4Shield()
        {
            Require(p.View.SkillShieldIcon&&p.View.SkillShieldIcon.sprite&&!p.View.SkillShieldIcon.raycastTarget&&p.View.SkillShieldIcon.gameObject.activeInHierarchy,"active shield indicator rendered and non-blocking");
            var active=session.ExportSave();int start=v4Evidence.events.Count;
            var contact=session.EnemyThreats.OrderBy(x=>x.ImpactAtMs).First();
            yield return V4Observe("v4-dam-shield-absorption",(float)((contact.ImpactAtMs-session.ClockMs+100)/1000));
            yield return V4Capture("v4-dam-shield-consumed");
            Require(v4Evidence.events.Skip(start).Count(x=>x.kind=="SkillShieldAbsorbed")==1&&session.SkillShieldMilli==0&&!p.View.SkillShieldIcon.gameObject.activeInHierarchy,"one shield contact clears indicator");
            // Client tests own the full three-second duration; this fixture isolates the final render boundary.
            active.run.skillShieldRemainingMs=100;V4Bind(new GameSession(catalog,active));
            yield return V4Capture("v4-dam-shield-expiry-last100ms-fixture");session.Tick(.101f);
            yield return V4Capture("v4-dam-shield-expired");
            Require(session.SkillShieldMilli==0&&session.SkillShieldRemainingMs==0&&!p.View.SkillShieldIcon.gameObject.activeInHierarchy,"shield expiry clears indicator");
        }
        void V4SkillLayout()
        {
            var box=ScreenBounds((RectTransform)p.View.Skill.transform);var safe=ScreenBounds(p.View.SafeRoot);
            Require(Mathf.Abs(box.center.x-safe.center.x)<1&&box.yMin>=safe.yMin&&box.yMax<safe.center.y,"skill lies at lower centre inside safe area");
            Require(!box.Overlaps(ScreenBounds((RectTransform)p.View.Attack.transform))&&p.View.Dodge.Take(3).All(x=>!box.Overlaps(ScreenBounds((RectTransform)x.transform))),"skill hitbox separate from attack and dodge");
        }
        IEnumerator V4Hud()
        {
            yield return V4Prepare("on",11);scenario="v4-hud";
            var body=(RectTransform)p.View.Stagger.transform.parent;
            Require(Mathf.Abs(body.rect.width/(p.View.SafeRoot.rect.width/1084)-408)<.01f&&Mathf.Abs(body.rect.height/(p.View.SafeRoot.rect.height/540)-7.2f)<.01f,"stagger body 408x7.2; outline excluded from 20 percent measurement");
            Require(p.Presentation.Telegraphs.DragonTheme&&Mathf.Abs(p.Presentation.Telegraphs.WarningStroke-4)<.001f&&Mathf.Abs(p.Presentation.Telegraphs.WarningOpacity-.92f)<.001f,"saved warning stroke and opacity");
            foreach(int amount in new[]{0,50,100})
            {
                var save=session.ExportSave();save.run.stagger=amount;V4Bind(new GameSession(catalog,save));
                yield return V4Capture("v4-stagger-"+amount);
                Require(p.View.Stagger.color==(amount==100?DragonUiPalette.StaggerReady:DragonUiPalette.StaggerFill),"bright stagger fill state "+amount);
            }
            V4Pointer(p.View.Attack);session.Tick(catalog.combat.attackWindupMs/1000f);yield return V4Capture("v4-stagger-stunned");
            Require(session.IsEnemyStunned&&p.View.Stagger.color==DragonUiPalette.StaggerStunned,"bright stunned fill");
            session.Tick(session.StunSecondsRemaining+.01f);yield return V4Capture("v4-stagger-released");
            foreach(int stage in new[]{1,11})
            {
                yield return V4Prepare("on",stage);var threat=session.EnemyThreats.OrderBy(x=>x.ImpactAtMs).First();
                double[] clocks={threat.CueAtMs+10,threat.CueEndMs+10,threat.ImpactAtMs-60};
                for(int i=0;i<clocks.Length;i++)
                {double target=Math.Min(threat.ImpactAtMs-1,clocks[i]);if(target>session.ClockMs)session.Tick((float)((target-session.ClockMs)/1000));yield return V4Capture("v4-warning-"+stage+"-"+i);}
            }
        }
        IEnumerator V4EnemyCombo(int stage)
        {
            yield return V4Prepare("on",stage);var threats=session.EnemyThreats.OrderBy(x=>x.ImpactAtMs).ToArray();
            Require(threats.Length>0,"queued enemy attack exists: "+stage);
            double begin=Math.Max(0,threats[0].ImpactAtMs-350);if(begin>session.ClockMs)session.Tick((float)((begin-session.ClockMs)/1000));
            int start=v4Evidence.events.Count;
            yield return V4Observe("v4-enemy-"+stage+"-combo",(float)((threats.Last().ImpactAtMs-session.ClockMs+650)/1000));
            var hits=v4Evidence.events.Skip(start).Where(x=>x.kind=="PlayerDamaged").ToArray();
            Require(hits.Length>=threats.Length,"actual enemy contacts captured: "+stage);
            for(int i=0;i<threats.Length;i++)Require(Math.Abs(hits[i].combatMs-threats[i].ImpactAtMs)<.01,"enemy contact time retained: "+stage+"/"+i);
            V3GroundIdle();yield return V4Capture("v4-enemy-"+stage+"-returned");
            if(stage==11)
            {
                var next=session.EnemyThreats.OrderBy(x=>x.ImpactAtMs).First(x=>x.ImpactAtMs>session.ClockMs);
                Require(next.PatternId=="frontal_dash","single-pattern pool produces another frontal dash");
                double seek=Math.Max(0,next.ImpactAtMs-session.ClockMs-350);if(seek>0)session.Tick((float)(seek/1000));
                int repeatStart=v4Evidence.events.Count;
                yield return V4Observe("v4-enemy-11-repeat",(float)((next.ImpactAtMs-session.ClockMs+650)/1000));
                Require(v4Evidence.events.Skip(repeatStart).Any(x=>x.kind=="PlayerDamaged"&&x.pattern=="frontal_dash"&&Math.Abs(x.combatMs-next.ImpactAtMs)<.01),"second actual frontal dash retains its scheduled contact");
                V3GroundIdle();
            }
        }
        IEnumerator V4Story(string id,int stage)
        {
            scenario="v4-story-"+id+"-"+stage;yield return V4Prepare(id,stage,true);
            V4Pointer(p.View.Skill);session.Tick(catalog.combat.attackWindupMs/1000f);
            if(session.Phase==SessionPhase.Transition)session.Tick(catalog.transitions.enemyDefeatMs/1000f+.01f);
            yield return Rendered();Require(session.Phase==SessionPhase.StoryDialogue,"actual defeated stage enters story: "+id+"/"+stage);
            var scene=catalog.FindStory(stage);Require(session.Story.LineCount==scene.lines.Length&&(stage!=10||scene.lines.Length==3)&&(stage!=20||scene.lines.Length==6),"story page count contract: "+stage);
            long gold=session.Gold;int fragments=session.Fragments;double cooldown=session.SkillCooldownRemainingMs;
            var skip=session.ExportSave();
            for(int i=0;i<scene.lines.Length;i++)
            {
                yield return V4Capture("v4-story-"+id+"-"+stage+"-page-"+i);V4CheckStory();
                var story=session.Story;Require(story.LineIndex==i,"story line index: "+stage+"/"+i);
                if(scene.lines[i].speaker=="player")Require(story.Speaker==catalog.GetCharacter(id).displayName&&story.PortraitId==id,"Core resolves selected player speaker/portrait: "+id);
                if((stage==2||stage==7||stage==15)&&i==0||stage==20&&i==1)
                    Require(story.Speaker=="???"&&story.PortraitId==(stage==2?"unknown":stage==7?"rina":stage==15?"barun":"doto"),"unknown speaker retains the scene-specific portrait policy");
                session.Tick(25);Require(!session.UseSkill()&&session.SkillCooldownRemainingMs==cooldown&&session.Story.LineIndex==i,"dialogue freezes skill clock and rejects use");
                if(i==1)
                {
                    Save();bool recovered;V4Bind(new GameSession(catalog,repository.Load(out recovered)));yield return V4Capture("v4-story-"+id+"-"+stage+"-restored-"+i);
                    Require(!recovered,"story file restore uses primary save");V4RestoredDuration(session.SkillCooldownRemainingMs,cooldown,"story cooldown: "+id+"/"+stage);
                    cooldown=session.SkillCooldownRemainingMs;V4CheckStory();
                }
                if(stage==20&&id==(Screen.width==1280?"dam":"rin")&&i==scene.lines.Length-2)
                    v4Resume=new V4ResumeExpectation{save=session.ExportSave(),text=session.Story.Text,speaker=session.Story.Speaker,portraitId=session.Story.PortraitId};
                Click(p.Presentation.Overlay.ButtonNext);yield return Rendered();
            }
            Require(session.Gold==gold&&session.Fragments==fragments&&session.SkillCooldownRemainingMs==cooldown,"story completion preserves wallet and skill clock");
            if(stage<20)
            {
                for(int i=0;i<240&&session.Phase==SessionPhase.Transition;i++)session.Tick(.05f);
                Require(session.Stage==stage+1&&session.Phase==SessionPhase.Combat&&session.SkillCooldownRemainingMs<=cooldown&&session.SkillCooldownRemainingMs>cooldown-51,"stage transition retains remaining cooldown");
            }
            else Require(session.Stage==20&&session.Phase==SessionPhase.RunEnd,"stage20 story ends at existing run boundary");
            V4Bind(new GameSession(catalog,skip));yield return Rendered();Click(p.Presentation.Overlay.ButtonSkip);yield return Rendered();
            Require(session.Gold==gold&&session.Fragments==fragments&&session.SkillCooldownRemainingMs==skip.run.skillCooldownRemainingMs,"skip preserves wallet and same-instance cooldown");
        }
        void V4CheckStory()
        {
            var story=session.Story;var overlay=p.Presentation.Overlay;
            Require(story!=null&&overlay.StoryText.gameObject.activeInHierarchy&&overlay.StorySpeaker.gameObject.activeInHierarchy,"story text and speaker actually visible");
            Require(overlay.StoryText.text==story.Text&&overlay.StorySpeaker.text==story.Speaker,"render matches resolved session.Story text/speaker");
            var portrait=Array.Find(overlay.Portraits,x=>x!=null&&x.Id==story.PortraitId);
            Require(portrait!=null&&portrait.Sprite&&overlay.StoryPortrait.gameObject.activeInHierarchy&&overlay.StoryPortrait.sprite==portrait.Sprite,"render matches resolved session.Story portrait");
        }
        IEnumerator V4Resume()
        {
            var expected=JsonUtility.FromJson<V4ResumeExpectation>(File.ReadAllText(Path.Combine(output,"v4-resume-expected.json")));
            var actual=session.ExportSave();var old=expected.save;
            Require(actual.run!=null&&old.run!=null&&actual.phase==old.phase&&actual.run.runId==old.run.runId&&actual.run.stage==old.run.stage,"native V4 resume phase/stage/run identity");
            Require(actual.selectedCharacterId==old.selectedCharacterId&&actual.gold==old.gold&&actual.fragments==old.fragments,"native V4 resume character and wallet");
            Require(actual.run.skillShieldMilli==old.run.skillShieldMilli,"native V4 resume integer shield amount");
            V4RestoredDuration(actual.run.skillCooldownRemainingMs,old.run.skillCooldownRemainingMs,"native cooldown");
            V4RestoredDuration(actual.run.skillShieldRemainingMs,old.run.skillShieldRemainingMs,"native shield");
            Require(actual.characters.Count==old.characters.Count&&actual.characters.Zip(old.characters,(a,b)=>JsonUtility.ToJson(a)==JsonUtility.ToJson(b)).All(x=>x),"native V4 resume growth/checkpoints/seen story");
            yield return V4Capture("v4-native-resume");V4CheckStory();
            Require(session.Story.Text==expected.text&&session.Story.Speaker==expected.speaker&&session.Story.PortraitId==expected.portraitId,"native resume preserves resolved story and portrait");
        }
        IEnumerator V4AudioCase(bool muted)
        {
            scenario="v4-audio-"+(muted?"muted":"touch-contact");yield return V4Prepare("on",11);
            var audio=p.Presentation.Audio;Require(audio&&audio.Sfx&&audio.AttackTouch&&audio.Swing,"V4 attack touch and contact clips bound");
            p.MuteAudio.isOn=muted;yield return Rendered();audio.Sfx.Stop();yield return Rendered();
            int sourceCount=audio.Sfx.gameObject.GetComponents<AudioSource>().Length;
            bool canCapture=sourceCount==1;
            if(canCapture)
            {
                if(!v4AudioTap)v4AudioTap=audio.Sfx.gameObject.AddComponent<DragonV4AudioProbeTap>();
                v4AudioTap.Begin(AudioSettings.outputSampleRate);
            }
            int start=v4Evidence.events.Count;
            V4Pointer(p.View.Attack);V4Pointer(p.View.Attack,false);V4Snapshot(scenario+"-press");
            yield return new WaitForSecondsRealtime(catalog.combat.attackWindupMs/1000f);session.Tick(catalog.combat.attackWindupMs/1000f);
            yield return Rendered();V4Snapshot(scenario+"-contact");yield return new WaitForSecondsRealtime(.5f);
            string file=scenario+"-source-filter.wav";int channels=0,samples=0;
            if(canCapture)v4AudioTap.Finish(Path.Combine(output,file),out channels,out samples);
            var emitted=v4Evidence.events.Skip(start).ToArray();
            var record=new V4Audio{scenario=scenario,muted=muted,touchClip=audio.AttackTouch.name,contactClip=audio.Swing.name,
                touchLength=audio.AttackTouch.length,contactLength=audio.Swing.length,attackEvents=emitted.Count(x=>x.kind=="AttackStarted"),contactEvents=emitted.Count(x=>x.kind=="EnemyDamaged"),
                file=samples>0?file:null,channels=channels,samples=samples,sampleRate=AudioSettings.outputSampleRate,audioSourceCount=sourceCount,
                captureStatus=!canCapture?"PCM_CAPTURE_UNAVAILABLE":samples>0?"SOURCE_FILTER_PCM_ONLY":"UNAVAILABLE_NO_CALLBACK_SAMPLES",
                captureReason=!canCapture?"MULTIPLE_AUDIO_SOURCES":null};
            v4Evidence.audio.Add(record);Require(record.attackEvents==1&&record.contactEvents==1,"double synthetic pointer request creates one accepted attack/contact: "+muted);
            Require(audio.Muted==muted&&audio.Sfx.mute==muted,"mute state reaches audio source: "+muted);
            p.MuteAudio.isOn=false;
        }
        IEnumerator V4Observe(string label,float seconds)
        {
            // Settle prior PNG-encoding delta before turning on the real session clock.
            yield return Rendered();yield return Rendered();
            yield return V3Observe(label,seconds,true,t=>V4Snapshot(label,v3Frames[v3Frames.Count-1].index.file));
        }
        IEnumerator V4Capture(string label){scenario=label;yield return Capture(label);V4Snapshot(label,label+".png");}
        void V4Snapshot(string label,string file=null)
        {
            var overlay=p.Presentation.Overlay;var story=session.Story;var audio=p.Presentation.Audio;
            var state=new V4State{tag=label,file=file,frame=Time.frameCount,stage=session.Stage,phase=session.Phase.ToString(),character=session.CurrentCharacterId,
                combatMs=session.ClockMs,realtime=Time.realtimeSinceStartupAsDouble,skill=session.CurrentSkill?.id,cooldownMs=session.SkillCooldownRemainingMs,
                shieldMs=session.SkillShieldRemainingMs,shieldMilli=session.SkillShieldMilli,paused=session.IsPaused,canUse=session.CanUseSkill,
                skillVisible=p.View.Skill&&p.View.Skill.gameObject.activeInHierarchy,skillInteractable=p.View.Skill&&p.View.Skill.Interactable,
                shieldVisible=p.View.SkillShieldIcon&&p.View.SkillShieldIcon.gameObject.activeInHierarchy,
                skillLabel=p.View.SkillLabel?p.View.SkillLabel.text:null,cooldownLabel=p.View.SkillCooldown?p.View.SkillCooldown.text:null,
                speaker=story?.Speaker,text=story?.Text,portraitId=story?.PortraitId,line=story?.LineIndex??-1,lineCount=story?.LineCount??0,
                portraitVisible=overlay.StoryPortrait&&overlay.StoryPortrait.gameObject.activeInHierarchy,portraitSprite=overlay.StoryPortrait&&overlay.StoryPortrait.sprite?overlay.StoryPortrait.sprite.name:null,
                muted=audio&&audio.Muted,sfxPlaying=audio&&audio.Sfx&&audio.Sfx.isPlaying,barBody=ScreenBounds((RectTransform)p.View.Stagger.transform.parent),barFill=p.View.Stagger.color,
                player=V3Actor(p.PlayerActor),enemy=V3Actor(p.EnemyActor)};
            if(p.View.Skill)state.skillBounds=ScreenBounds((RectTransform)p.View.Skill.transform);
            state.attackBounds=ScreenBounds((RectTransform)p.View.Attack.transform);
            if(overlay.StorySpeaker)state.speakerBounds=ScreenBounds(overlay.StorySpeaker.rectTransform);
            if(overlay.StoryPortrait)state.portraitBounds=ScreenBounds(overlay.StoryPortrait.rectTransform);
            v4States.WriteLine(JsonUtility.ToJson(state));
        }
        void ReleaseV4Frames()
        {
            if(!v4Active)return;v4Active=false;if(p)p.TickSession=false;
            if(session!=null)session.EventRaised-=V4EventRaised;
            if(v4AudioTap){v4AudioTap.Cancel();Destroy(v4AudioTap);}
            foreach(var frame in v3Frames)if(frame.pixels)Destroy(frame.pixels);v3Frames.Clear();
            v3Index?.Dispose();v3Index=null;v4States?.Dispose();v4States=null;
            if(Directory.Exists(output))
            {
                File.WriteAllText(Path.Combine(output,"v4-evidence.json"),JsonUtility.ToJson(v4Evidence,true));
                File.WriteAllText(Path.Combine(output,"v4-frame-evidence.json"),JsonUtility.ToJson(v3Evidence,true));
            }
        }
    }
    // QA-only optional source-filter recording. No mutations to incoming audio.
    // It is upstream engine evidence, never a claim about speakers or direct listening.
    public sealed class DragonV4AudioProbeTap : MonoBehaviour
    {
        readonly object gate=new object();readonly List<float> pcm=new List<float>();bool recording;int rate,channelCount;
        public void Begin(int sampleRate){lock(gate){pcm.Clear();rate=sampleRate;channelCount=0;recording=true;}}
        void OnAudioFilterRead(float[] data,int channels)
        {lock(gate){if(!recording)return;channelCount=channels;int limit=rate*channels*4;for(int i=0;i<data.Length&&pcm.Count<limit;i++)pcm.Add(data[i]);}}
        public void Cancel(){lock(gate)recording=false;}
        public void Finish(string path,out int channels,out int samples)
        {
            float[] values;lock(gate){recording=false;values=pcm.ToArray();channels=channelCount;samples=values.Length;}
            if(samples==0)return;
            using(var writer=new BinaryWriter(new FileStream(path,FileMode.CreateNew)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                foreach(float value in values)writer.Write((short)(Math.Max(-1,Math.Min(1,value))*32767));
            }
        }
    }
}
#endif
