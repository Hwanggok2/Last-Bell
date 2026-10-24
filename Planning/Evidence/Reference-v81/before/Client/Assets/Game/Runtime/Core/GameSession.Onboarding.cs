using System;

namespace LastBell
{
    public sealed partial class GameSession
    {
        double prologueElapsedMs;
        int prologueParagraph;
        bool prologuePaused, prologueManual;
        public float PrologueProgress => (float)Math.Min(1,prologueElapsedMs/catalog.onboarding.prologueDurationMs);
        public int PrologueParagraph => prologueParagraph;
        public bool ProloguePaused => prologuePaused;
        public bool PrologueManual => prologueManual;
        public bool CombatSuspended => IsPaused||Phase==SessionPhase.ComboHint||Phase==SessionPhase.Prologue;
        public bool SetPrologueManual(bool value)
        {
            prologueManual=value;
            prologueParagraph=Math.Min(catalog.onboarding.prologueParagraphs.Length-1,(int)(PrologueProgress*catalog.onboarding.prologueParagraphs.Length));
            Changed?.Invoke();return true;
        }
        public bool ToggleProloguePause()
        {
            if(Phase!=SessionPhase.Prologue)return false;
            prologuePaused=!prologuePaused;Changed?.Invoke();return true;
        }
        public bool NextPrologueParagraph()
        {
            if(Phase!=SessionPhase.Prologue||!prologueManual||paused)return false;
            if(++prologueParagraph>=catalog.onboarding.prologueParagraphs.Length)return FinishPrologue();
            Changed?.Invoke();return true;
        }
        public bool FinishPrologue()
        {
            if(Phase!=SessionPhase.Prologue)return false;
            bool replay=state.prologueSeen;
            state.prologueSeen=true;prologuePaused=false;paused=false;
            if(replay)state.phase=SessionPhase.Lobby;else ContinueOpening();Publish();return true;
        }
        public bool ReplayPrologue()
        {
            if(Phase!=SessionPhase.Lobby||!state.prologueSeen)return false;
            prologueElapsedMs=0;prologueParagraph=0;prologuePaused=false;paused=false;
            state.phase=SessionPhase.Prologue;Publish();return true;
        }
        void AdvancePrologue(float seconds)
        {
            if(paused||prologuePaused||prologueManual)return;
            prologueElapsedMs=Math.Min(catalog.onboarding.prologueDurationMs,prologueElapsedMs+seconds*1000d);
            if(prologueElapsedMs>=catalog.onboarding.prologueDurationMs)FinishPrologue();
        }
        public bool ConfirmComboHint()
        {
            if(Phase!=SessionPhase.ComboHint||paused)return false;
            state.comboHintSeen=true;state.run.forceTwoHitSequence=true;CombatReady();Publish();return true;
        }
    }
}
