using System;
namespace LastBell
{
    [Serializable] public sealed class OnboardingConfig
    {
        public int prologueDurationMs;
        public string prologueTitle;
        public string[] prologueParagraphs;
        public string comboHint;
    }
}
