using System;

namespace LastBell
{
    // Presentation accounting only. The session has already committed the reward.
    public sealed class GoldCollection
    {
        // Every reward in the playable 20 stages stays one pixel per gold, including luck.
        // Only ~3/7 are in flight together, keeping this limit below UI's 65k vertices.
        public const int MaxDots = 32768;
        public const double FlightSeconds = .36, EmissionSeconds = .84, Duration = 1.2;
        public long Amount { get; }
        public int Count { get; }
        public GoldCollection(long amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            Amount=amount;Count=(int)Math.Min(amount,MaxDots);
        }
        public double StartAt(int index) => Count<=1?0:EmissionSeconds*index/(Count-1);
        public int Arrived(double seconds)
        {
            if(Count==0||seconds<FlightSeconds)return 0;
            if(Count==1||seconds>=Duration-1e-7)return Count;
            return Math.Min(Count,1+(int)Math.Floor((seconds-FlightSeconds)/EmissionSeconds*(Count-1)+1e-7));
        }
        public long Collected(double seconds)
        {
            if(Count==0)return 0;
            int n=Arrived(seconds);
            // Division before multiplication keeps even Int64.MaxValue lossless.
            return Amount/Count*n+Math.Min(n,Amount%Count);
        }
    }
}
