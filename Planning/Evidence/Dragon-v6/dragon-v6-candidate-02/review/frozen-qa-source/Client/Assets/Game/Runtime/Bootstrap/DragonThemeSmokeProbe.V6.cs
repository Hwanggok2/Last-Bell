#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Development-only observations. Product rules, saved views and effects remain authoritative.
    public sealed partial class DragonThemeSmokeProbe
    {
        [Serializable] sealed class V6Evidence
        {
            public string scope="v0.9.5 representative changed-scope observations, not all44/previous60 criteria";
            public string visualReview="NOT_RUN",osInput="DEFERRED",audioListening="NOT_RUN",userDefaultProfile="NOT_ACCESSED";
            public string limits="Synthetic saved-UI input, explicit DTO/seed fixtures and labelled manual-clock gaps. Core boundary tests are handed-over evidence. PNG inspection is separate. No full acceptance, physical input, listening or performance certification.";
            public int stateSamples,staticCaptures,inputEdges;
            public List<string> fixtures=new List<string>();
            public List<V6Event> events=new List<V6Event>();
            public List<int> routeStages=new List<int>();
            public List<string> fileRoundtrips=new List<string>();
            public string continuationOf;
            public List<V6PopupCheck> popupChecks=new List<V6PopupCheck>();
            public string[] pending={"manual PNG readability/overlap and boss lightning review","full numeric/corruption boundaries use Client evidence","all44/previous60 acceptance","physical input/mobile/audio/performance/long-duration leaks"};
        }
        [Serializable] sealed class V6Event
        {
            public string scenario,fixture,kind,skill,companion;public int frame,stage,runId;public double clockMs;
            public long damageMilli,recoveryMilli,absorbedMilli,gold;public double staggerBefore,staggerGained;public bool critical;
        }
        [Serializable] sealed class V6Input
        {public string scenario,control,kind;public int sequence,frame,eventsBefore,eventsAfter;public double clockBefore,clockAfter;}
        [Serializable] sealed class V6Popup
        {public string name,digits;public bool critical,companion,overlapsStaggerRect;public int lane;public long damageMilli;public Rect bounds;public float alpha,remainingLife;public Color configuredColor,expectedGlyphTint;}
        [Serializable] sealed class V6PopupCheck
        {public string file,fixture;public long companionTotalMilli;public int popupCount;public List<V6Popup> popups;}
        [Serializable] sealed class V6State
        {
            public string tag,file,fixture,clockMode,phase;public int frame,stage,rtWidth,rtHeight,rtAA,totalParticles;public double realtime,clockMs;
            public bool paused,stunned,immune,ready,rtMips,rtPoint,rtBound,rawRaycast;
            public GameSaveData save;public V3ActorFrame player,enemy;public float enemyFlash,playerBarValue,enemyBarValue;
            public Rect playerBar,enemyBar,staggerBar;public Vector3 enemyBodyScreen;public Vector2 enemyEffectPoint;public float lightningHeadroom;
            public List<V5Particles> particles=new List<V5Particles>();public List<V6Popup> popups=new List<V6Popup>();
        }
        [Serializable] sealed class V6Checkpoint {public string kind;public GameSaveData save;}
        [Serializable] sealed class V6Difference {public string path,before,after;public long ulps=-1;public bool accepted;}
        [Serializable] sealed class V6Comparison {public string label;public bool stringEqual,passed;public List<V6Difference> differences=new List<V6Difference>();}
        readonly V6Evidence v6Evidence=new V6Evidence();
        StreamWriter v6States,v6Inputs;bool v6Active;string v6Fixture="native-boot";
        V6Checkpoint v6Checkpoint;
        bool v6ConcurrentPopupChecked;
        // Read-only development observation of the exact renderer text and popup pool state.
        static readonly FieldInfo V6Digits=typeof(PixelNumber).GetField("digits",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly FieldInfo V6Damage=typeof(BattleEffects).GetField("numberDamageMilli",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly FieldInfo V6Life=typeof(BattleEffects).GetField("numberLife",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly FieldInfo V6Lanes=typeof(BattleEffects).GetField("numberLanes",BindingFlags.Instance|BindingFlags.NonPublic);

        static void V6Preflight(string dest,string save,string mode)
        {
            string expected=Path.Combine(dest,"expected-catalog.json");RejectLinks(expected);
            if(!File.Exists(expected))throw new IOException("V6 requires frozen catalog");
            if(mode=="v6-continue")
            {
                string marker=Path.Combine(dest,"continuation-source.json");RejectLinks(marker);
                if(!File.Exists(marker))throw new IOException("V6 continuation requires prior-candidate evidence marker");
            }
            if(mode=="v6-resume")
            {
                string checkpoint=Path.Combine(dest,"v6-resume-expected.json");RejectLinks(checkpoint);
                if(!File.Exists(checkpoint)||!File.Exists(Path.Combine(save,"last-bell-save.json")))throw new IOException("V6 resume requires isolated checkpoint");
            }
            else
            {
                string seed=Path.Combine(dest,"legacy-source","last-bell-save.json");RejectLinks(seed);
                if(!File.Exists(seed))throw new IOException("V6 requires prior-QA schema9 seed");
                if(mode=="v6-legacy"&&(!File.Exists(Path.Combine(save,"last-bell-save.json"))||File.ReadAllText(seed)!=File.ReadAllText(Path.Combine(save,"last-bell-save.json"))))throw new IOException("V6 legacy boot must equal copied prior QA seed");
            }
            foreach(string file in Directory.Exists(save)?Directory.GetFiles(save):Array.Empty<string>())RejectLinks(file);
        }
        IEnumerator DragonV6(bool resume,bool legacy,bool continuation=false)
        {
            v6Active=true;v6States=new StreamWriter(new FileStream(Path.Combine(output,"v6-state.jsonl"),FileMode.CreateNew));
            v6Inputs=new StreamWriter(new FileStream(Path.Combine(output,"v6-input.jsonl"),FileMode.CreateNew));
            v3Index=new StreamWriter(new FileStream(Path.Combine(output,"frames.jsonl"),FileMode.CreateNew));
            v3Evidence.coverage="V6 buffered render collector; no V3/V4/V5 campaign";report.limits=v6Evidence.limits;session.EventRaised+=V6EventRaised;
            Require(Application.version=="0.9.5"&&catalog.dataVersion=="mvp1.18.0","V6 version and data");
            Require((Screen.width==1280&&Screen.height==720)||(Screen.width==640&&Screen.height==360),"V6 target resolution");
#if LASTBELL_PLAYTEST_092 && UNITY_STANDALONE_WIN && !UNITY_EDITOR
            v3Evidence.playtestDefine=true;
#endif
            Require(v3Evidence.playtestDefine,"V6 Windows observed define");
            Require(JsonUtility.ToJson(catalog)==JsonUtility.ToJson(JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(output,"expected-catalog.json")))),"V6 loaded frozen catalog");
            V6References();yield return Rendered();
            if(resume){yield return V6Resume();yield break;}
            if(continuation){Require(Screen.width==1280,"V6 continuation is1280 only");v6Evidence.continuationOf="dragon-v6-candidate-01: completed legacy/inn/HP observations reused";}
            else
            {
                bool recovered;var old=new JsonSaveRepository(Path.Combine(output,"legacy-source")).Load(out recovered);
                Require(!recovered&&old!=null&&old.schemaVersion==9,"V6 prior-QA schema9 source");
                var migrated=new GameSession(catalog,old);V6Migration(old,migrated);
                if(legacy){V6Migration(old,session);yield return V6Capture("v6-native-legacy");}
                yield return V6Inn();yield return V6HpSamples();
            }
            foreach(string id in new[]{"on","dam","rin"})yield return V6PlayerSkill(id);
            foreach(string id in new[]{"tori","barun","doto","rina","momo"})foreach(bool proc in new[]{false,true})yield return V6Companion(id,proc);
            foreach(int stage in new[]{5,10,15,20})yield return V6BossLightning(stage);
            yield return V6Stun();yield return V6Concurrent();yield return V6KillBoundary();
            if(Screen.width==1280)yield return V6Route();
            yield return V6PrepareCheckpoint();repository.Save(v6Checkpoint.save);
            File.WriteAllText(Path.Combine(output,"v6-resume-expected.json"),JsonUtility.ToJson(v6Checkpoint,true));
            Require(v3Evidence.writtenFrames==v3Evidence.segments.Sum(x=>x.frames),"V6 frames fully written");
        }
        void V6References()
        {
            Require(V6Digits!=null&&V6Damage!=null&&V6Life!=null&&V6Lanes!=null,"V6 read-only popup observation fields");
            Require(p.View.Reference.RestoreUpgradeV6&&p.View.Companions.SkillName&&p.View.Companions.SkillEffect&&p.View.Companions.SkillProgress,"saved v6 inn references");
            V5References();var fx=p.Presentation.SkillEffects;var systems=new[]{fx.AttackParticles,fx.ShieldParticles,fx.SealParticles,fx.SupportParticles};int[] caps={256,32,24,64};
            for(int i=0;i<4;i++){Require(systems[i].main.maxParticles==caps[i],"V6 particle capacity "+systems[i].name);Require(systems[i].GetComponent<ParticleSystemRenderer>().sortMode==ParticleSystemSortMode.Distance,"V6 particle depth sorting");}
            Require(systems.Sum(x=>x.main.maxParticles)==376,"V6 total capacity376");
        }
        void V6Bind(GameSession value,string fixture,bool unpause=true)
        {
            if(session!=null)session.EventRaised-=V6EventRaised;scenario=fixture;v6Fixture=fixture;v6Evidence.fixtures.Add(fixture);
            Bind(value,false);if(unpause)value.SetPaused(false);session.EventRaised+=V6EventRaised;
        }
        void V6EventRaised(SessionEvent e)=>v6Evidence.events.Add(new V6Event{scenario=scenario,fixture=v6Fixture,kind=e.Kind.ToString(),skill=e.SkillId,companion=e.CompanionId,
            frame=Time.frameCount,stage=e.Stage,runId=session.ExportSave().run?.runId??0,clockMs=e.TimeMs,damageMilli=e.DamageMilli,recoveryMilli=e.RecoveryMilli,absorbedMilli=e.AbsorbedMilli,gold=session.Gold,staggerBefore=e.StaggerBefore,staggerGained=e.StaggerGained,critical=e.Critical});
        void V6InputEdge(string control,string kind,Action action)
        {
            var row=new V6Input{scenario=scenario,control=control,kind=kind,sequence=++v6Evidence.inputEdges,frame=Time.frameCount,clockBefore=session.ClockMs,eventsBefore=v6Evidence.events.Count};
            action();row.clockAfter=session.ClockMs;row.eventsAfter=v6Evidence.events.Count;v6Inputs.WriteLine(JsonUtility.ToJson(row));
        }
        void V6Click(Button b)=>V6InputEdge(b?b.name:"null","synthetic-click",()=>Click(b));
        void V6Pointer(PointerCommand b,bool enabled=true)=>V6InputEdge(b?b.name:"null","synthetic-down-up",()=>V4Pointer(b,enabled));
        void V6Gap(double seconds)
        {p.TickSession=false;clockMode="manual-v6-gap-no-render";while(seconds>0){double step=Math.Min(.025,seconds);session.Tick((float)step);seconds-=step;}}
        IEnumerator V6Until(Func<bool> predicate,double seconds,string label)
        {p.TickSession=false;clockMode="manual-v6-25ms-per-render";int budget=(int)Math.Ceiling(seconds/.025);while(!predicate()&&budget-->0){session.Tick(.025f);yield return null;}Require(predicate(),label);yield return Rendered();}
        IEnumerator V6Capture(string name)
        {scenario=name;yield return Capture(name);V6Snapshot(name,name+".png");v6Evidence.staticCaptures++;}
        IEnumerator V6Observe(string name,float seconds,bool realtime=true)
        {yield return V3Observe(name,seconds,realtime,t=>{clockMode=realtime?"real-frame-v6":"real-frame-v6-static-session";V6Snapshot(name,v3Frames[v3Frames.Count-1].index.file);});}
        GameSaveData V6Battle(string player,int stage,params string[] members)
        {
            var lobby=V5Lobby(player,members.Length>0);lobby.companionSlots=Math.Max(1,members.Length);
            if(members.Length==3)lobby.highestClearedStage=30; // Explicit future-rule fixture only, never route progression.
            lobby.characters.Single(x=>x.id==player).companionIds=members.ToList();
            var value=new GameSession(catalog,lobby);Require(value.StartRun(),"V6 fixture departure "+player);var save=value.ExportSave();
            save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;save.run.patternStep=save.run.patternIndex=save.run.sequenceCursor=0;save.run.randomSequence=Array.Empty<string>();
            save.run.enemyMaxHpMilli=save.run.enemyHpMilli=1000000000;return save;
        }
        IEnumerator V6Prepare(GameSaveData save,string label)
        {V6Bind(new GameSession(catalog,save),label);yield return Rendered();yield return Rendered();}
        int V6Seed(GameSaveData source,string id,bool proc)
        {
            for(int n=1;n<=128;n++)
            {
                var save=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(source));var c=save.run.companions.Single(x=>x.id==id);c.remainingMs=180;c.windingUp=c.skillReserved=c.reservedSkill=false;c.randomState=n*104729;
                var check=new GameSession(catalog,save);check.SetPaused(false);check.Tick(.001f);var reserved=check.ExportSave().run.companions.Single(x=>x.id==id);
                if(reserved.skillReserved&&reserved.reservedSkill==proc)return n*104729;
            }
            throw new InvalidOperationException("No bounded public-Core seed fixture for "+id+"/"+proc);
        }
        static string V6Value(object value)=>value==null?"<null>":value is double d?d.ToString("R",CultureInfo.InvariantCulture):value is float f?f.ToString("R",CultureInfo.InvariantCulture):Convert.ToString(value,CultureInfo.InvariantCulture);
        static void V6Compare(object original,object expected,object actual,string path,List<V6Difference> differences)
        {
            if(expected==null||actual==null)
            {if(expected!=actual)differences.Add(new V6Difference{path=path,before=V6Value(expected),after=V6Value(actual)});return;}
            Type type=expected.GetType();
            if(type!=actual.GetType()){differences.Add(new V6Difference{path=path,before=type.FullName,after=actual.GetType().FullName});return;}
            object source=original??expected;
            if(type==typeof(double)||type==typeof(float))
            {
                double a=Convert.ToDouble(source,CultureInfo.InvariantCulture),b=Convert.ToDouble(actual,CultureInfo.InvariantCulture);
                bool finite=!double.IsNaN(a)&&!double.IsInfinity(a)&&!double.IsNaN(b)&&!double.IsInfinity(b);long distance=-1;
                if(finite)
                {
                    if(a==b)distance=0;
                    else if(Math.Sign(a)==Math.Sign(b))distance=type==typeof(double)?Math.Abs(BitConverter.DoubleToInt64Bits(a)-BitConverter.DoubleToInt64Bits(b)):
                        Math.Abs((long)BitConverter.SingleToInt32Bits((float)a)-BitConverter.SingleToInt32Bits((float)b));
                }
                if(distance!=0)differences.Add(new V6Difference{path=path,before=V6Value(source),after=V6Value(actual),ulps=distance,accepted=finite&&distance>=0&&distance<=4});
                return;
            }
            if(type==typeof(string)||type.IsEnum||type.IsPrimitive||type==typeof(decimal))
            {
                // Strings use the exact serialized DTO shape: Unity writes null strings as empty.
                object wanted=type==typeof(string)?expected:source;
                if(!Equals(wanted,actual))differences.Add(new V6Difference{path=path,before=V6Value(wanted),after=V6Value(actual)});return;
            }
            if(expected is IList aList&&actual is IList bList)
            {
                var originalList=original as IList;
                if(aList.Count!=bList.Count)differences.Add(new V6Difference{path=path+".Count",before=aList.Count.ToString(),after=bList.Count.ToString()});
                for(int i=0;i<Math.Min(aList.Count,bList.Count);i++)V6Compare(originalList!=null&&i<originalList.Count?originalList[i]:null,aList[i],bList[i],path+"["+i+"]",differences);
                return;
            }
            foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))
                V6Compare(original==null?null:field.GetValue(original),field.GetValue(expected),field.GetValue(actual),path+"."+field.Name,differences);
        }
        bool V6CompareDto<T>(T before,T after,string file,string label)
        {
            string json=JsonUtility.ToJson(before);T wire=JsonUtility.FromJson<T>(json);
            // Keep canonical optional-inline null handling identical to JsonSaveRepository.Load.
            if(wire is GameSaveData save)save.NormalizeOptionalRecords();
            var comparison=new V6Comparison{label=label,stringEqual=json==JsonUtility.ToJson(after)};
            V6Compare(before,wire,after,"$",comparison.differences);comparison.passed=comparison.differences.All(x=>x.accepted);
            File.WriteAllText(file,JsonUtility.ToJson(comparison,true));return comparison.passed;
        }
        void V6Roundtrip(string label)
        {
            var before=session.ExportSave();string folder=Path.Combine(output,"fixtures",label);var repo=new JsonSaveRepository(folder);repo.Save(before);
            File.WriteAllText(Path.Combine(folder,"before.json"),JsonUtility.ToJson(before,true));bool recovered;var disk=repo.Load(out recovered);
            File.WriteAllText(Path.Combine(folder,"loaded.json"),JsonUtility.ToJson(disk,true));
            bool equivalent=V6CompareDto(before,disk,Path.Combine(folder,"payload-comparison.json"),label);
            Require(!recovered&&equivalent,"V6 repository DTO roundtrip (finite floats <=4ULP) "+label);
            var after=new GameSession(catalog,disk).ExportSave();
            File.WriteAllText(Path.Combine(folder,"restored.json"),JsonUtility.ToJson(after,true));
            Require(after.gold==before.gold&&after.fragments==before.fragments&&JsonUtility.ToJson(new V6Characters{items=after.characters})==JsonUtility.ToJson(new V6Characters{items=before.characters}),"V6 wallet and character progression roundtrip "+label);
            if(before.run!=null)
            {
                Require(after.run.playerHpMilli==before.run.playerHpMilli&&after.run.enemyHpMilli==before.run.enemyHpMilli&&after.run.skillContactPending==before.run.skillContactPending,"V6 HP and independent reservation roundtrip "+label);
                V5Duration(after.run.skillContactRemainingMs,before.run.skillContactRemainingMs,"V6 remaining skill "+label);V5Duration(after.run.skillCooldownRemainingMs,before.run.skillCooldownRemainingMs,"V6 cooldown "+label);
                Require(V6CompareDto(new V6CompanionList{items=before.run.companions},new V6CompanionList{items=after.run.companions},Path.Combine(folder,"companion-comparison.json"),label+"-restored-companions"),"V6 companion timer RNG and reserved choice roundtrip "+label);
            }
            v6Evidence.fileRoundtrips.Add(label);
        }
        [Serializable] sealed class V6CompanionList {public List<RunCompanionData> items;}
        [Serializable] sealed class V6Characters {public List<CharacterProgressData> items;}
        void V6Migration(GameSaveData before,GameSession value)
        {
            var after=value.ExportSave();Require(after.schemaVersion==9&&after.gold==before.gold&&after.fragments==before.fragments&&after.selectedCharacterId==before.selectedCharacterId,"V6 migration wallet/schema/selection");
            foreach(var c in before.characters){var a=after.characters.Single(x=>x.id==c.id);Require(a.statLevels.SequenceEqual(c.statLevels)&&a.companionIds.SequenceEqual(c.companionIds)&&a.weaponRank==c.weaponRank,"V6 migration character "+c.id);}
            if(before.run!=null){Require(after.run.skillRulesVersion==1&&after.run.playerHpMilli==before.run.playerHpMilli&&after.run.stage==before.run.stage,"V6 migration run rules/HP/stage");V5Duration(after.run.skillCooldownRemainingMs,before.run.skillCooldownRemainingMs,"V6 migration cooldown");}
        }
        IEnumerator V6Inn()
        {
            V6Bind(new GameSession(catalog,V5Lobby("on",true)),"future-rule-all-five-inn");yield return Rendered();yield return V6Capture("v6-inn-selection");
            V6Click(p.View.StartRun);yield return Rendered();var view=p.View.Reference;
            Require(view.UpgradeScreen&&!view.GrowthTab.gameObject.activeInHierarchy&&view.GrowthRows.All(x=>!x.Plus.gameObject.activeInHierarchy&&x.Pips.All(y=>!y.gameObject.activeInHierarchy)),"restored inn hides v5 tabs plus and pips");
            var button=ScreenBounds((RectTransform)view.CompanionsTab.transform);var safe=ScreenBounds(p.View.SafeRoot);Require(button.center.x>safe.center.x&&button.center.y>safe.center.y&&button.xMax<=safe.xMax+1,"companion entry upper right inside safe area");
            V6Click(view.StatButtons[4]);yield return Rendered();long cost=session.GetUpgradeCost(4),gold=session.Gold;int level=session.GetStatLevel(4);
            yield return V6Capture("v6-inn-recovery-before");V6Click(view.Purchase);yield return Rendered();
            Require(session.Gold==gold-cost&&session.GetStatLevel(4)==level+1,"recovery purchase once through restored detail");V6Roundtrip("inn-recovery");yield return V6Capture("v6-inn-recovery-after");
            V6Click(view.CompanionsTab);yield return Rendered();var inn=p.View.Companions;Require(inn.IsOpen,"upper-right companion entry opens saved panel");
            foreach(string id in new[]{"tori","barun","doto","rina","momo"})
            {
                int index=session.Companions.ToList().FindIndex(x=>x.Id==id);inn.RosterScroll.verticalNormalizedPosition=index<3?1:0;yield return Rendered();V6Click(inn.Rows[index].Select);yield return Rendered();
                var before=session.Companions.Single(x=>x.Id==id);Require(inn.SkillName.text==before.SkillName&&!string.IsNullOrEmpty(inn.SkillEffect.text)&&!string.IsNullOrEmpty(inn.SkillProgress.text),"saved skill texts for "+id);
                yield return V6Capture("v6-inn-"+id+"-before");gold=session.Gold;V6Click(inn.Upgrade);yield return Rendered();var after=session.Companions.Single(x=>x.Id==id);
                Require(after.Level==before.Level+1&&session.Gold==gold-before.UpgradeCost&&after.SkillChancePercent==before.NextSkillChancePercent&&after.PeriodMs==before.NextPeriodMs&&after.ExpectedDpsMilli+1e-6>=before.ExpectedDpsMilli,"companion UI upgrade chance period and expected DPS "+id);
                V6Roundtrip("inn-"+id);yield return V6Capture("v6-inn-"+id+"-after");
            }
            V6Click(view.Back);yield return Rendered();Require(!inn.IsOpen&&view.UpgradeScreen,"companion back returns growth");V6Click(view.Back);yield return Rendered();Require(!view.UpgradeScreen,"growth back returns selection");
            var max=V5Lobby("rin",true);foreach(var c in max.companions)c.level=30;
            V6Bind(new GameSession(catalog,max),"explicit-level30-cap-inn-fixture");yield return Rendered();V6Click(view.CompanionsTab);yield return Rendered();
            Require(session.Companions.All(x=>x.SkillChancePercent==41&&x.PeriodMs==catalog.FindCompanion(x.Id).periodMs*71/100&&!x.CanUpgrade),"level30 uses41percent and71percent period, not future guards");
            yield return V6Capture("v6-inn-level30-cap");V6Roundtrip("inn-level30-cap");
        }
        IEnumerator V6HpSamples()
        {
            foreach(int percent in new[]{100,50,1})
            {
                var save=V6Battle("on",11);save.run.playerHpMilli=percent==1?1:save.run.combatStats.maxHpMilli*percent/100;save.run.enemyHpMilli=percent==1?1:save.run.enemyMaxHpMilli*percent/100;
                yield return V6Prepare(save,"explicit-hp-bar-"+percent);yield return V6Capture("v6-hp-"+percent);
                Require(p.View.PlayerHealthBar.Value>0&&p.View.EnemyHealthBar.Value>0,"living minimum HP remains visible");
                if(percent==1){V6Pointer(p.View.Skill);yield return V6Observe("v6-hp-zero-on-defeat",.7f);}
            }
        }
        IEnumerator V6PlayerSkill(string id)
        {
            yield return V6Prepare(V6Battle(id,11),"v6-skill-"+id);V6Roundtrip("player-"+id+"-idle");int start=v6Evidence.events.Count;
            V6Pointer(p.View.Attack);var normal=session.ExportSave().run;Require(normal.attackHitAtMs>session.ClockMs,"normal hit pending "+id);V6Pointer(p.View.Skill);
            Require(session.SkillContactPending&&session.ExportSave().run.attackHitAtMs==normal.attackHitAtMs,"independent skill preserves normal hit deadline "+id);
            V5Duration(session.SkillCooldownRemainingMs,20000,"V6 skill CD at acceptance");V5Duration(session.SkillContactRemainingMs,200,"V6 independent200ms");V6Roundtrip("player-"+id+"-pending");
            Require(!session.UseSkill(),"duplicate pending skill rejected "+id);V6Click(p.View.Pause);yield return Rendered();var paused=session.ExportSave().run;V6Gap(1);
            Require(session.ClockMs==paused.clockMs&&!session.UseSkill()&&session.SkillContactRemainingMs==paused.skillContactRemainingMs,"pause freezes pending skill and rejects input "+id);yield return V6Capture("v6-skill-"+id+"-paused");
            V6Click(p.View.Resume);yield return Rendered();yield return V6Observe("v6-skill-"+id+"-overlap",.65f);
            var contacts=v6Evidence.events.Skip(start).ToArray();Require(contacts.Count(x=>x.kind=="EnemyDamaged")==1&&contacts.Count(x=>x.kind=="PlayerSkillContact")==1&&contacts.Count(x=>x.kind=="AttackStarted")==1&&contacts.Count(x=>x.kind=="PlayerSkillStarted")==1,"one ordinary and one independent contact "+id);
            var skill=contacts.Single(x=>x.kind=="PlayerSkillContact");V5Duration(skill.clockMs-contacts.Single(x=>x.kind=="PlayerSkillStarted").clockMs,200,"V6 contact exact200ms "+id);Require(!skill.critical,"independent skill noncritical "+id);V6Roundtrip("player-"+id+"-after");
            var boundary=session.ExportSave();boundary.run.skillCooldownRemainingMs=100;yield return V6Prepare(boundary,"explicit-last100ms-CD-"+id);Require(!session.UseSkill(),"CD boundary before rejected "+id);
            V6Gap(.099);Require(!session.UseSkill(),"CD last1ms rejected "+id);V6Gap(.002);Require(session.CanUseSkill,"CD crossed boundary ready "+id);
            yield return V6Prepare(V6Battle(id,11),"v6-skill-reverse-"+id);start=v6Evidence.events.Count;V6Pointer(p.View.Skill);V6Pointer(p.View.Attack);yield return V6Observe("v6-skill-"+id+"-reverse",.5f);
            Require(v6Evidence.events.Skip(start).Count(x=>x.kind=="EnemyDamaged")==1&&v6Evidence.events.Skip(start).Count(x=>x.kind=="PlayerSkillContact")==1,"reverse input preserves two contacts "+id);
        }
        IEnumerator V6Companion(string id,bool proc)
        {
            string label="v6-companion-"+id+(proc?"-proc":"-basic");var save=V6Battle("on",11,id);var c=save.run.companions[0];c.randomState=V6Seed(save,id,proc);c.remainingMs=230;
            if(id=="momo")save.run.playerHpMilli-=Math.Min(100000,save.run.playerHpMilli/2);
            yield return V6Prepare(save,label+"-seed-fixture");int start=v6Evidence.events.Count;V6Gap(.051);var reserved=session.ExportSave().run.companions[0];Require(reserved.skillReserved&&reserved.reservedSkill==proc,"actual Core seeded reservation "+label);
            V6Roundtrip(label+"-reserved");V6Click(p.View.Pause);yield return Rendered();V6Gap(2);Require(JsonUtility.ToJson(session.ExportSave().run.companions[0])==JsonUtility.ToJson(reserved),"pause preserves companion RNG/choice/timer "+label);V6Click(p.View.Resume);yield return Rendered();
            yield return V6Observe(label+"-contact",.65f);var hits=v6Evidence.events.Skip(start).Where(x=>x.kind=="CompanionDamaged").ToArray();Require(hits.Length==1&&hits[0].companion==id&&hits[0].damageMilli>0&&(string.IsNullOrEmpty(hits[0].skill)!=proc),"one replacement/basic contact "+label);
            Require(!v6Evidence.events.Skip(start).Any(x=>x.kind=="AttackStarted"||x.kind=="PlayerSkillContact"),"support independent of player action "+label);V6Roundtrip(label+"-after");
            if(id=="momo"&&proc)Require(v6Evidence.events.Skip(start).Count(x=>x.kind=="PlayerRecovered"&&x.companion==id&&x.recoveryMilli>0)==1,"Momo actual heal once");
        }
        IEnumerator V6BossLightning(int stage)
        {
            yield return V6Prepare(V6Battle("rin",stage,"rina"),"explicit-large-enemy-lightning-"+stage);var save=session.ExportSave();var c=save.run.companions[0];c.remainingMs=200;c.randomState=V6Seed(save,"rina",true);
            yield return V6Prepare(save,"v6-boss-lightning-"+stage);V6Pointer(p.View.Skill);yield return V6Observe("v6-boss-lightning-"+stage,.7f);yield return V6Capture("v6-boss-lightning-"+stage+"-late");
            Require(v6Evidence.events.Any(x=>x.fixture==v6Fixture&&x.kind=="PlayerSkillContact")&&v6Evidence.events.Any(x=>x.fixture==v6Fixture&&x.kind=="CompanionDamaged"&&!string.IsNullOrEmpty(x.skill)),"both Miel and Rina lightning contacts stage"+stage);
        }
        IEnumerator V6Stun()
        {
            var save=V6Battle("rin",11);save.run.stagger=catalog.combat.staggerThreshold-10;yield return V6Prepare(save,"explicit-near-stun-threshold");V6Pointer(p.View.Skill);yield return V6Observe("v6-miel-ready",.4f);
            Require(session.IsStunReady&&!session.IsEnemyStunned,"Miel skill fills stagger without consuming stun");V6Pointer(p.View.Attack);yield return V6Observe("v6-actual-stun",.7f);
            Require(session.IsEnemyStunned&&p.Presentation.SkillEffects.SealParticles.particleCount==21&&p.View.StunStars.All(x=>!x.gameObject.activeInHierarchy),"actual21particle stars and no old duplicate");yield return V6Capture("v6-stun-late-no-hitflash");
            V6Click(p.View.Pause);yield return Rendered();double clock=session.ClockMs;V6Gap(2);Require(session.ClockMs==clock,"stun pause clock frozen");yield return V6Capture("v6-stun-paused");V6Click(p.View.Resume);yield return Rendered();
            V6Gap(Math.Max(0,session.StunSecondsRemaining-.08));yield return V6Observe("v6-stun-ended",.2f);Require(!session.IsEnemyStunned&&p.Presentation.SkillEffects.SealParticles.particleCount==0,"stun stars removed at end");
        }
        IEnumerator V6Concurrent()
        {
            var save=V6Battle("rin",11,"rina","tori","doto");foreach(var c in save.run.companions){c.remainingMs=200;c.randomState=V6Seed(save,c.id,true);}
            yield return V6Prepare(save,"explicit-future-three-slot-simultaneous-proc");int start=v6Evidence.events.Count;V6Pointer(p.View.Attack);V6Pointer(p.View.Skill);yield return V6Observe("v6-three-companion-plus-player-contact",1.15f);
            Require(v6Evidence.events.Skip(start).Count(x=>x.kind=="CompanionDamaged"&&!string.IsNullOrEmpty(x.skill))==3&&v6Evidence.events.Skip(start).Count(x=>x.kind=="PlayerSkillContact")==1,"three reserved skills and player skill contact once");
            Require(v6ConcurrentPopupChecked,"actual3 popups preserve normal/skill and combined companion total");
            Require(!p.Effects.Numbers.Any(x=>x.isActiveAndEnabled&&x.Companion),"combined companion popup clears after original lifetime");
            yield return V6Prepare(V6Battle("on",11),"explicit-rebind-clears-past-effects");yield return V6Observe("v6-rebind-no-past-contact",.4f,false);
            Require(p.Presentation.SkillEffects.AttackParticles.particleCount==0&&p.Presentation.SkillEffects.SupportParticles.particleCount==0,"new binding clears old bursts");
        }
        IEnumerator V6KillBoundary()
        {
            foreach(string id in new[]{"on","dam","rin"})
            {
                var save=V6Battle(id,3);save.run.enemyHpMilli=1;yield return V6Prepare(save,"explicit-one-HP-double-contact-"+id);int start=v6Evidence.events.Count;
                V6Pointer(p.View.Attack);V6Pointer(p.View.Skill);yield return V6Observe("v6-double-contact-kill-"+id,.6f);
                var events=v6Evidence.events.Skip(start).ToArray();Require(events.Count(x=>x.kind=="EnemyDefeated")==1&&events.Count(x=>x.kind=="EnemyDamaged"||x.kind=="PlayerSkillContact")==1,"first lethal contact suppresses second damage/reward "+id);
                Require(!session.SkillContactPending,"death transition clears independent pending "+id);Require(!session.UseSkill(),"transition skill rejected "+id);
            }
        }
        IEnumerator V6Route()
        {
            var lobby=V5Lobby("on");lobby.comboPracticeSkipped=false;var hero=lobby.characters.Single(x=>x.id=="on");hero.statLevels[0]=30;hero.statLevels[4]=20;
            V6Bind(new GameSession(catalog,lobby),"explicit-growth-preparation-continuous-stage1-to20-no-stage-jumps");yield return Rendered();V6Click(p.View.StartRun);yield return Rendered();V6Click(p.View.StartRun);yield return Rendered();
            int start=v6Evidence.events.Count,lastStage=0,iteration=0,storyCapture=0;
            while(session.Phase!=SessionPhase.MvpComplete&&iteration++<24000)
            {
                Require(session.PlayerHp>0&&session.Phase!=SessionPhase.DeathResult,"continuous route remains alive; no rescue fixture");
                if(lastStage!=session.Stage)
                {
                    Require(session.Stage==lastStage+1,"continuous route advances one stage at a time");lastStage=session.Stage;v6Evidence.routeStages.Add(lastStage);
                    yield return V6Capture("v6-route-stage-"+lastStage.ToString("D2"));
                }
                if(session.Phase==SessionPhase.StoryDialogue)
                {
                    Require(!session.UseSkill(),"route dialogue rejects skill");yield return V6Capture("v6-route-story-"+(storyCapture++).ToString("D2"));
                    if(session.Story.AwaitingCompanionChoice)V6Click(p.Presentation.Overlay.ButtonLater);else V6Click(p.Presentation.Overlay.ButtonNext);yield return Rendered();continue;
                }
                if(session.Phase==SessionPhase.ComboHint){V6Click(p.View.ModalConfirm);yield return Rendered();continue;}
                if(session.ComboPractice.AwaitingInput){V6Pointer(p.View.Dodge[(int)session.ComboPractice.RequiredDirection]);yield return Rendered();continue;}
                if(session.Phase==SessionPhase.RunEnd||session.Phase==SessionPhase.RewardAnnouncement){V6Click(p.View.Reference.SettlementContinue);yield return Rendered();continue;}
                if(session.Phase==SessionPhase.RewardChoice){V6Click(p.View.Rewards[0].Receive);yield return Rendered();continue;}
                if(session.Phase==SessionPhase.Combat&&!session.ComboPractice.Active)
                {
                    var cue=session.EnemyIntent;
                    if(cue.Visible&&cue.SecondsToHit<=.08f&&p.View.Dodge[(int)cue.Direction].Interactable)V6Pointer(p.View.Dodge[(int)cue.Direction]);
                    else if((!cue.Visible||cue.SecondsToHit>.4f)&&p.View.Attack.Interactable)V6Pointer(p.View.Attack);
                    if(session.CanUseSkill&&p.View.Skill.Interactable)V6Pointer(p.View.Skill);
                }
                p.TickSession=false;clockMode="manual-v6-route-50ms-per-render";session.TickInputFrame(.05f);yield return null;
            }
            Require(session.Phase==SessionPhase.MvpComplete&&v6Evidence.routeStages.SequenceEqual(Enumerable.Range(1,20)),"one continuous1-to20 route reaches MVP complete");
            var defeats=v6Evidence.events.Skip(start).Where(x=>x.kind=="EnemyDefeated").ToArray();Require(defeats.Length==20&&defeats.Select(x=>x.stage).OrderBy(x=>x).SequenceEqual(Enumerable.Range(1,20)),"route each enemy defeated/rewarded once");
            Require(session.CompanionUnlocked("tori")&&session.CompanionUnlocked("barun")&&session.ExportSave().comboPracticeCompleted,"route Tori/Barun and guided completed");
            V6Roundtrip("continuous-route-complete");yield return V6Capture("v6-route-mvp-complete");
        }
        IEnumerator V6PrepareCheckpoint()
        {
            bool pending=Screen.width==1280;string player=pending?"dam":"rin";var save=V6Battle(player,11,"tori");save.run.companions[0].remainingMs=200;save.run.companions[0].randomState=V6Seed(save,"tori",true);
            yield return V6Prepare(save,"explicit-native-checkpoint-"+(pending?"pending":"post-contact"));V6Pointer(p.View.Attack);V6Pointer(p.View.Skill);
            if(pending){V6Gap(.05);yield return Rendered();Require(session.SkillContactPending&&session.ExportSave().run.companions[0].skillReserved,"native pending checkpoint includes both reservations");}
            else{yield return V6Observe("v6-checkpoint-after-contact",.45f);Require(!session.SkillContactPending&&session.SkillCooldownRemainingMs>0,"native post-contact checkpoint has cooldown only");}
            V6Roundtrip("native-checkpoint-"+(pending?"pending":"post"));v6Checkpoint=new V6Checkpoint{kind=pending?"pending":"post-contact",save=session.ExportSave()};yield return V6Capture("v6-checkpoint-"+v6Checkpoint.kind);
        }
        IEnumerator V6Resume()
        {
            var expected=JsonUtility.FromJson<V6Checkpoint>(File.ReadAllText(Path.Combine(output,"v6-resume-expected.json")));var before=expected.save;var after=session.ExportSave();v6Fixture="native-v6-"+expected.kind;
            Require(session.IsPaused&&after.run.stage==before.run.stage&&after.run.playerHpMilli==before.run.playerHpMilli&&after.gold==before.gold&&after.fragments==before.fragments,"native resume paused with HP/stage/wallet");
            Require(after.run.skillContactPending==before.run.skillContactPending,"native independent reservation flag");V5Duration(after.run.skillContactRemainingMs,before.run.skillContactRemainingMs,"native independent remaining");V5Duration(after.run.skillCooldownRemainingMs,before.run.skillCooldownRemainingMs,"native cooldown");
            Require(V6CompareDto(new V6CompanionList{items=before.run.companions},new V6CompanionList{items=after.run.companions},Path.Combine(output,"native-companion-comparison.json"),"native-companions"),"native companion RNG/reservation exact and finite time within4ULP");
            Require(p.Presentation.SkillEffects.AttackParticles.particleCount==0,"native restore does not replay past preparation/burst");yield return V6Capture("v6-native-resume-initial");int start=v6Evidence.events.Count;double clock=session.ClockMs;
            V6Click(p.View.Resume);yield return Rendered();yield return V6Observe("v6-native-resume-first-window",.6f);
            var events=v6Evidence.events.Skip(start).ToArray();Require(events.Count(x=>x.kind=="PlayerSkillContact")== (expected.kind=="pending"?1:0),"native pending resumes once / completed never replays");
            Require(!events.Any(x=>x.kind=="AttackStarted"||x.kind=="EnemyDamaged"||x.kind=="PlayerSkillStarted"),"ordinary canceled reservation and old starts do not replay");
            if(expected.kind=="pending")
            {
                V5Duration(events.Single(x=>x.kind=="PlayerSkillContact").clockMs-clock,before.run.skillContactRemainingMs,"native skill saved remaining time");
                var hit=events.Single(x=>x.kind=="CompanionDamaged");Require(hit.skill==catalog.FindCompanion("tori").skillId,"native companion reserved proc preserved");V5Duration(hit.clockMs-clock,before.run.companions[0].remainingMs,"native companion remaining time");
            }
            else Require(!events.Any(x=>x.kind=="CompanionDamaged"),"post-contact companion does not replay within first window");
            V6Roundtrip("native-resume-final");yield return V6Capture("v6-native-resume-final");
        }
        void V6Snapshot(string tag,string file)
        {
            var fx=p.Presentation.SkillEffects;var rt=fx.Output;var value=new V6State{tag=tag,file=file,fixture=v6Fixture,clockMode=clockMode,phase=session.Phase.ToString(),frame=Time.frameCount,stage=session.Stage,
                realtime=Time.realtimeSinceStartupAsDouble,clockMs=session.ClockMs,paused=session.IsPaused,stunned=session.IsEnemyStunned,immune=session.IsStaggerImmune,ready=session.IsStunReady,save=session.ExportSave(),
                player=V5Actor(p.PlayerActor),enemy=V5Actor(p.EnemyActor),enemyFlash=p.EnemyActor.HitFlashAmount,playerBarValue=p.View.PlayerHealthBar.Value,enemyBarValue=p.View.EnemyHealthBar.Value,
                playerBar=ScreenBounds(p.View.PlayerHealthBar.rectTransform),enemyBar=ScreenBounds(p.View.EnemyHealthBar.rectTransform),staggerBar=ScreenBounds((RectTransform)p.View.Stagger.transform.parent),rawRaycast=fx.Surface.raycastTarget};
            value.enemyBodyScreen=p.WorldCamera.WorldToScreenPoint(p.EnemyActor.CanonicalBodyPoint(.5f));RectTransformUtility.ScreenPointToLocalPointInRectangle(fx.Root,value.enemyBodyScreen,UiCamera,out value.enemyEffectPoint);
            value.lightningHeadroom=fx.Root.rect.yMax-116-value.enemyEffectPoint.y;
            if(rt){value.rtWidth=rt.width;value.rtHeight=rt.height;value.rtAA=rt.antiAliasing;value.rtMips=rt.useMipMap;value.rtPoint=rt.filterMode==FilterMode.Point;value.rtBound=fx.Surface.texture==rt&&fx.ParticleCamera.targetTexture==rt;}
            Require(rt&&value.rtPoint&&!value.rtMips&&value.rtAA==1&&value.rtBound&&!value.rawRaycast,"V6 actual Point RT and nonblocking binding");
            foreach(var system in new[]{fx.AttackParticles,fx.ShieldParticles,fx.SealParticles,fx.SupportParticles}){var ps=V5Particle(system);Require(ps.count<=system.main.maxParticles,"V6 particle bound "+system.name);value.totalParticles+=ps.count;value.particles.Add(ps);}
            Require(value.totalParticles<=376,"V6 total actual particles <=376");
            var damage=(long[])V6Damage.GetValue(p.Effects);var lives=(float[])V6Life.GetValue(p.Effects);var lanes=(int[])V6Lanes.GetValue(p.Effects);
            for(int i=0;i<p.Effects.Numbers.Length;i++)
            {
                var number=p.Effects.Numbers[i];if(!number.isActiveAndEnabled)continue;var bounds=ScreenBounds(number.rectTransform);
                value.popups.Add(new V6Popup{name=number.name,digits=(string)V6Digits.GetValue(number),critical=number.Critical,companion=number.Companion,bounds=bounds,alpha=number.color.a,
                    damageMilli=damage[i],remainingLife=lives[i],lane=lanes[i],configuredColor=number.color,expectedGlyphTint=number.Critical?DragonUiPalette.Warning:number.Companion?DragonUiPalette.StaggerFill:DragonUiPalette.Light,
                    overlapsStaggerRect=p.View.Stagger.transform.parent.gameObject.activeInHierarchy&&bounds.Overlaps(value.staggerBar)});
            }
            v6States.WriteLine(JsonUtility.ToJson(value));v6Evidence.stateSamples++;
            if(tag=="v6-three-companion-plus-player-contact"&&!v6ConcurrentPopupChecked)
            {
                var hits=v6Evidence.events.Where(x=>x.fixture==v6Fixture&&(x.kind=="EnemyDamaged"||x.kind=="PlayerSkillContact"||x.kind=="CompanionDamaged")).ToArray();
                if(hits.Length==5)
                {
                    long total=hits.Where(x=>x.kind=="CompanionDamaged").Sum(x=>x.damageMilli);
                    v6Evidence.popupChecks.Add(new V6PopupCheck{file=file,fixture=v6Fixture,companionTotalMilli=total,popupCount=value.popups.Count,popups=value.popups});
                    Require(value.popups.Count==3&&value.popups.Count(x=>x.companion)==1,"concurrent actual popup count3 / companion count1");
                    var combined=value.popups.Single(x=>x.companion);Require(combined.damageMilli==total&&combined.digits==Math.Min(99999,Math.Round(total/1000d,MidpointRounding.AwayFromZero)).ToString(),"companion popup sums raw milli before rounding");
                    Require(value.popups.Where(x=>!x.companion).Select(x=>x.damageMilli).OrderBy(x=>x).SequenceEqual(hits.Where(x=>x.kind!="CompanionDamaged").Select(x=>x.damageMilli).OrderBy(x=>x)),"ordinary and independent skill popup amounts stay separate");
                    v6ConcurrentPopupChecked=true;
                }
            }
        }
        void ReleaseV6Frames()
        {
            if(!v6Active)return;v6Active=false;if(p)p.TickSession=false;if(session!=null)session.EventRaised-=V6EventRaised;
            foreach(var frame in v3Frames)if(frame.pixels)Destroy(frame.pixels);v3Frames.Clear();v3Index?.Dispose();v3Index=null;v6States?.Dispose();v6Inputs?.Dispose();
            if(Directory.Exists(output)){File.WriteAllText(Path.Combine(output,"v6-evidence.json"),JsonUtility.ToJson(v6Evidence,true));File.WriteAllText(Path.Combine(output,"v6-frame-evidence.json"),JsonUtility.ToJson(v3Evidence,true));}
        }
    }
}
#endif
