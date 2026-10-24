using UnityEngine;
namespace LastBell
{
    public sealed class EnemyAttackEffects : MonoBehaviour
    {
        public EnemyEffectBurst[] Windups,Releases,Impacts,Counters;
        public EnemyImpactGraphic GroundRings;
        MvpPresenter owner;GameSession session;double impactAt=-10000,lastBeatAt=-10000;
        public int ActiveParticles {get;private set;}
        public int ParticleCapacity=>Capacity(Windups)+Capacity(Releases)+Capacity(Impacts)+Capacity(Counters);
        static int Capacity(EnemyEffectBurst[] slots){int n=0;foreach(var s in slots)n+=s.Capacity;return n;}
        public void Bind(MvpPresenter p){owner=p;session=p.Session;Clear();}
        public void Clear(){foreach(var group in new[]{Windups,Releases,Impacts,Counters})foreach(var fx in group)fx.Clear();impactAt=lastBeatAt=-10000;if(GroundRings)GroundRings.Clear();}
        public void OnEvent(SessionEvent e)
        {
            if(!owner)return;
            if(e.Kind==SessionEventKind.EnemyStunned){foreach(var fx in Windups)fx.Clear();foreach(var fx in Releases)fx.Clear();return;}
            if(e.Kind==SessionEventKind.EnemyDefeated||e.Kind==SessionEventKind.PlayerDefeated||e.Kind==SessionEventKind.StagePrepared){Clear();return;}
            bool reduced=owner.Effects.ReducedShake;var audio=owner.Presentation.Audio;
            if(e.Kind==SessionEventKind.EnemyCueStarted)
            {
                float life=.78f;foreach(var t in session.EnemyThreats)if(t.PatternId==e.PatternId&&System.Math.Abs(t.CueAtMs-e.TimeMs)<.01)life=(float)(t.CueEndMs-t.CueAtMs)/1000;
                if(!reduced)EmitPooled(Windups,e,life);
            }
            if(e.Kind==SessionEventKind.EnemyReleased){float flight=.09f;foreach(var t in session.EnemyThreats)if(System.Math.Abs(t.ReleaseAtMs-e.TimeMs)<.01)flight=(float)(t.ImpactAtMs-t.ReleaseAtMs)/1000;if(!reduced)EmitPooled(Releases,e,flight);if(audio)audio.Play(audio.EnemySwing,.7f);}
            if(e.Kind==SessionEventKind.Dodged||e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.Parried||e.Kind==SessionEventKind.PerfectParry||e.Kind==SessionEventKind.PlayerDamaged)
            {
                if(session.Phase==SessionPhase.TutorialAttack)return;
                EmitPooled(Impacts,e,.22f);impactAt=e.TimeMs;if(GroundRings)GroundRings.Confirm(e);
                if(audio){audio.Play(audio.Spike,.45f);audio.Play(audio.EnemyImpact,.4f);}
            }
            if(!reduced&&e.Kind==SessionEventKind.AttackStarted&&e.PerfectCounter)EmitPooled(Counters,e,.12f);
        }
        void EmitPooled(EnemyEffectBurst[] pool,SessionEvent e,float life)
        {
            var slot=pool[0];foreach(var item in pool)if(!item.Active){slot=item;break;}else if(item.StartedMs<slot.StartedMs)slot=item;
            int stage=session.Stage,tier=stage<5?0:stage<10?1:stage<15?2:stage<20?3:4;
            int budget=Mathf.Max(0,48-ActiveParticles+slot.Particles.particleCount);
            int count=slot.Kind==EnemyEffectKind.Impact?Mathf.Min(24,new[]{0,6,10,14,18}[tier]+(stage%5==0?6:0)):Mathf.Min(slot.Capacity,2+tier*2);
            if(owner.Effects.ReducedShake)count=Mathf.Min(4,count);
            slot.StartBurst(owner.WorldRoot,e.PatternId,e.TimeMs,life,e.Kind==SessionEventKind.Parried||e.Kind==SessionEventKind.PerfectParry,Mathf.Min(count,budget),tier,stage>=11);
            ActiveParticles=0;foreach(var group in new[]{Windups,Releases,Impacts,Counters})foreach(var fx in group)ActiveParticles+=fx.Particles.particleCount;
        }
        public void Sample()
        {
            if(!owner)return;bool reduced=owner.Effects.ReducedShake;
            if(!session.IsPaused&&session.Phase==SessionPhase.Combat)
                foreach(var t in session.EnemyThreats)if(session.ClockMs>=t.CueAtMs&&session.ClockMs<t.CueEndMs)
                {
                    double beatAt=t.CueAtMs+System.Math.Floor((session.ClockMs-t.CueAtMs)/t.BeatMs)*t.BeatMs;
                    if(beatAt!=lastBeatAt){lastBeatAt=beatAt;var audio=owner.Presentation.Audio;}
                    break;
                }
            if(session.Phase!=SessionPhase.Combat&&session.Phase!=SessionPhase.TutorialDodge&&session.Phase!=SessionPhase.TutorialAttack)Clear();
            ActiveParticles=0;
            foreach(var group in new[]{Windups,Releases,Impacts,Counters})foreach(var fx in group){fx.Sample(session.ClockMs,reduced);ActiveParticles+=fx.Particles.particleCount;}
            if(GroundRings)GroundRings.Sample(session.ClockMs,reduced);
            owner.Effects.SampleEnemyShake((float)((session.ClockMs-impactAt)/1000),owner.WorldCamera);
        }
    }
}
