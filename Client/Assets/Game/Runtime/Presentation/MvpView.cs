using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    [Serializable] public sealed class StatRowBinding
    {
        public Text Title, Value, Level, Price;
        public SymbolGraphic[] Stars, Bells;
        public Button Upgrade;
    }
    [Serializable] public sealed class RewardCardBinding
    {
        public Text Title, Level, Before, After, Equipment;
        public Button Receive;
        public RelicIconGraphic Icon;
        public Image Selection;
    }

    // All references are saved by MvpSceneBuilder; this class never builds UI at runtime.
    public sealed class MvpView : MonoBehaviour
    {
        public RectTransform SafeRoot;
        public ReferenceLobbyView Reference;
        public CompanionInnView Companions;
        public DragonUiTheme Dragon;
        public GameObject BattlePanel, LobbyPanel, RelicPanel, RewardPanel, ModalPanel, PausePanel;
        public CanvasGroup LobbyInput;
        public Text EnemyName, BattleGold, HitCombo, StunReady;
        public GoldPickupGraphic GoldPickup;
        public Text Stage, Tutorial, Wallet, CharacterName, Cycle, StartLabel, Feedback;
        public Button StartRun, OpenRelics, RelicBack, RelicEquip, SlotPurchase, Pause, Resume, ReturnHome;
        public Toggle ReducedShake;
        public PointerCommand[] Dodge;
        public PointerCommand Attack;
        public PointerCommand Skill;
        public Text SkillLabel, SkillCooldown;
        public ReferencePanelGraphic SkillFace;
        public Image SkillShieldIcon;
        public SymbolGraphic[] DodgeSymbols;
        public SymbolGraphic AttackSymbol;
        public PlateGraphic AttackPlate;
        public Image PlayerHp, EnemyHp, BossHp, Stagger;
        public HealthBarGraphic PlayerHealthBar, EnemyHealthBar;
        public GameObject StaggerIcon;
        public RectTransform PlayerHpRoot, EnemyHpRoot, DangerRoot, NextDangerRoot;
        public GameObject BossHpRoot;
        public DangerGraphic Danger, NextDanger;
        public TimingGraphic Timing;
        public DodgeFeedback DodgeResult;
        public CombatRhythmGraphic Rhythm;
        public SymbolGraphic[] StunStars;
        public Button[] Characters;
        public Text[] CharacterLabels;
        public StatRowBinding[] Stats;
        public Image LobbyPortrait;
        public Button[] RelicItems;
        public Text[] RelicItemLabels;
        public Image[] EquippedSlotIcons;
        public Text EquippedSlots, RelicName, RelicLevel, RelicEffect, RelicNext, RelicEquipLabel, SlotPurchaseLabel;
        public RewardCardBinding[] Rewards;
        public Text RewardNote;
        public RectTransform RewardDialog;
        public Text RewardDescription;
        public Button RewardConfirm, RewardGold;
        public Text ModalTitle, ModalBody, ModalConfirmLabel, ModalCancelLabel;
        public Button ModalConfirm, ModalCancel;
        public GameObject ResultPanel;
        public Text ResultTitle, ResultScope, ResultGold, ResultFragments, ResultRetryHint;
        public Button ResultRetry, ResultLobby;
        public PrologueView Prologue;
        public GameObject ComboHintBells;
        public Button ReplayPrologue;
        public Button ComboSkip,ComboReplay;
        public AudioCreditsView AudioCredits;

        public void Only(GameObject panel)
        {
            if(panel!=LobbyPanel&&AudioCredits)AudioCredits.Close();
            BattlePanel.SetActive(panel==BattlePanel);LobbyPanel.SetActive(panel==LobbyPanel);
            RelicPanel.SetActive(panel==RelicPanel);RewardPanel.SetActive(panel==RewardPanel);
            if(ResultPanel)ResultPanel.SetActive(panel==ResultPanel);
            if(Prologue)Prologue.gameObject.SetActive(panel==Prologue.gameObject);
        }
        public void SetModal(string title,string body,string confirm,string cancel,bool canConfirm)
        {
            ModalPanel.SetActive(true);ModalTitle.text=Dragon?DragonUiPalette.Copy(title):title;ModalBody.text=Dragon?DragonUiPalette.Copy(body):body;
            ((RectTransform)ModalTitle.transform.parent).sizeDelta=new Vector2(650,400);
            if(ComboHintBells)ComboHintBells.SetActive(false);
            ModalConfirmLabel.text=confirm;ModalCancelLabel.text=cancel;
            ModalConfirm.interactable=canConfirm;ModalCancel.gameObject.SetActive(!string.IsNullOrEmpty(cancel));
            var confirmRect=(RectTransform)ModalConfirm.transform;
            confirmRect.anchorMin=confirmRect.anchorMax=new Vector2(string.IsNullOrEmpty(cancel)?.5f:.25f,0);
        }
    }
}
