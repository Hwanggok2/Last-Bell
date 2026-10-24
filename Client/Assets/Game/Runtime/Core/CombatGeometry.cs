using System;

namespace LastBell
{
    // Authored v5.5 combat space: 960x540, top-left origin, home foot (375,442).
    // Both the collision resolver and the projected gold rectangles use this data.
    public readonly struct CombatRect
    {
        public readonly float X, Y, Width, Height;
        public CombatRect(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
        public CombatRect Offset(float x,float y)=>new CombatRect(X+x,Y+y,Width,Height);
        public bool Overlaps(CombatRect b)=>X<b.X+b.Width&&X+Width>b.X&&Y<b.Y+b.Height&&Y+Height>b.Y;
    }
    public static class CombatGeometry
    {
        public const float HomeX=375,HomeY=442,EnemyX=675;
        public static CombatRect Threat(string pattern)
        {
            switch(pattern)
            {
                case "thrust": case "frontal_dash": return new CombatRect(340,306,284,136);
                case "low_sweep": return new CombatRect(248,406,376,36);
                case "high_sweep": return new CombatRect(248,270,376,88);
                case "outer_arc": return new CombatRect(248,218,165,224);
                default: throw new ArgumentOutOfRangeException(nameof(pattern));
            }
        }
        public static float DodgeX(DodgeDirection direction)=>direction==DodgeDirection.Back?-90:direction==DodgeDirection.Forward?90:0;
        public static float DodgeY(DodgeDirection direction)=>direction==DodgeDirection.Up?-108:0;
        public static float AttackX(string enemyKind,double ageMs)
        {
            if(ageMs<0||ageMs>=500)return HomeX;
            double q=ageMs<120?EaseOut(ageMs/120):ageMs<230?1:1-EaseOut((ageMs-230)/270);
            return HomeX+165*(float)q;
        }
        public static double EaseOut(double value){value=Math.Max(0,Math.Min(1,value));return 1-Math.Pow(1-value,3);}
        public static float DodgeAmount(double ageMs,double enterMs,double holdMs)
        {
            if(ageMs<0)return 0;
            return (float)(ageMs<enterMs?EaseOut(ageMs/enterMs):ageMs<holdMs?1:1-EaseOut((ageMs-holdMs)/130));
        }
        public static CombatRect Body(DodgeDirection direction,bool active,float amount=1)
        {
            if(!active)return new CombatRect(353,340,44,100);
            var body=new CombatRect(353,340,44,100);
            return body.Offset(DodgeX(direction)*amount,DodgeY(direction)*amount);
        }
    }
    public sealed class EnemyThreatView
    {
        public string PatternId {get;internal set;}
        public DodgeDirection Direction {get;internal set;}
        public int Step {get;internal set;}
        public int SequenceIndex {get;internal set;}
        public int SequenceCount {get;internal set;}=1;
        public double CueAtMs {get;internal set;}
        public double CueEndMs {get;internal set;}
        public double ReleaseAtMs {get;internal set;}
        public double ImpactAtMs {get;internal set;}
        public bool RequiresParry=>PatternId=="high_sweep";
        public double BeatMs=>(CueEndMs-CueAtMs)/3;
        public double WaveStartMs=>ImpactAtMs-BeatMs*2.5;
        public CombatRect Bounds=>CombatGeometry.Threat(PatternId);
        public bool Resolved {get;internal set;}
        internal bool CueEmitted,ReleaseEmitted;
        internal EnemyThreatView Copy()=>(EnemyThreatView)MemberwiseClone();
    }
}
