using NUnit.Framework;
using UnityEngine;

namespace LastBell.Tests
{
    public sealed class ActorClipTimingTests
    {
        static ActorClip Run() => new ActorClip { frames=new Sprite[8], fps=20, loop=true,
            frameDurationsMs=new[]{40,40,60,60,40,40,60,60} };

        [Test]
        public void AuthoredHoldsSwitchAtEachBoundaryAndRepeatWithoutLosingLongPoses()
        {
            var run=Run();Assert.That(run.Duration,Is.EqualTo(.4f));
            float[] starts={0,.04f,.08f,.14f,.20f,.24f,.28f,.34f};
            for(int cycle=0;cycle<5;cycle++)for(int i=0;i<8;i++)
            {
                float t=cycle*.4f+starts[i];
                Assert.That(run.FrameIndex(t),Is.EqualTo(i),"at "+t);
                Assert.That(run.FrameIndex(t+.019f),Is.EqualTo(i),"hold "+t);
                if(t>0)Assert.That(run.FrameIndex(t-.00001f),Is.EqualTo((i+7)%8),"before "+t);
            }
            Assert.That(run.FrameIndex(-1),Is.EqualTo(0));Assert.That(run.FrameIndex(2),Is.EqualTo(0));
        }

        [Test]
        public void ExplicitDurationScalesHoldsAndNonLoopClampsAtLastPose()
        {
            var run=Run();Assert.That(run.FrameIndex(.279f,.8f),Is.EqualTo(2));
            Assert.That(run.FrameIndex(.28f,.8f),Is.EqualTo(3));Assert.That(run.FrameIndex(.8f,.8f),Is.EqualTo(0));
            run.loop=false;Assert.That(run.FrameIndex(.4f),Is.EqualTo(7));Assert.That(run.FrameIndex(10),Is.EqualTo(7));
        }

        [Test]
        public void ReviewedAttackUsesAuthoredContactAndRecoveryHolds()
        {
            var clip=new ActorClip{frames=new Sprite[10],contactFrame=6,
                frameDurationsMs=new[]{80,90,100,55,45,45,70,65,70,100}};
            Assert.That(clip.ContactTime,Is.EqualTo(.415f));Assert.That(clip.Duration,Is.EqualTo(.72f));
            Assert.That(clip.FrameIndex(clip.ContactTime-.001f),Is.EqualTo(5));
            Assert.That(clip.FrameIndex(clip.ContactTime),Is.EqualTo(6));
            Assert.That(clip.FrameIndex(clip.ContactTime+.069f),Is.EqualTo(6));
            Assert.That(clip.FrameIndex(clip.ContactTime+.070f),Is.EqualTo(7));
            Assert.That(clip.FrameIndex(.719f),Is.EqualTo(9));
        }

        [Test]
        public void LegacyUniformFramesAndAttackContactTimingAreUnchanged()
        {
            var clip=new ActorClip{frames=new Sprite[8],fps=20,loop=true};
            Assert.That(clip.FrameIndex(.049f),Is.EqualTo(0));Assert.That(clip.FrameIndex(.05f),Is.EqualTo(1));
            clip.loop=false;clip.contactFrame=3;
            Assert.That(clip.FrameIndex(.199f,.5f,.2f),Is.EqualTo(2));Assert.That(clip.FrameIndex(.2f,.5f,.2f),Is.EqualTo(3));
            Assert.That(clip.FrameIndex(.5f,.5f,.2f),Is.EqualTo(7));
        }
    }
}
