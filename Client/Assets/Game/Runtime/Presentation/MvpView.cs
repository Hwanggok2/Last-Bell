using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    [Serializable] public sealed class StatRowBinding
    {
        public Text Title, Value, Level;
        public SymbolGraphic[] Stars, Bells;
        public Button Upgrade;
    }
    [Serializable] public sealed class RewardCardBinding
    {
        public Text Title, Level, Before, After, Equipment;
        public Button Receive;
    }

    // All references are saved by MvpSceneBuilder; this class never builds UI at runtime.
    public sealed class MvpView : MonoBehaviour
    {
        public RectTransform SafeRoot;
        public GameObject BattlePanel, LobbyPanel, RelicPanel, RewardPanel, ModalPanel, PausePanel;
        public Text Stage, Tutorial, Wallet, CharacterName, Cycle, StartLabel, Feedback;
        public Button StartRun, OpenRelics, RelicBack, RelicEquip, SlotPurchase, Pause, Resume, ReturnHome;
        public Toggle ReducedShake;
        public PointerCommand[] Dodge;
        public PointerCommand Attack;
        public SymbolGraphic[] DodgeSymbols;
        public SymbolGraphic AttackSymbol;
        public Image PlayerHp, EnemyHp, BossHp, Stagger;
        public RectTransform PlayerHpRoot, EnemyHpRoot, DangerRoot, NextDangerRoot;
        public GameObject BossHpRoot;
        public DangerGraphic Danger, NextDanger;
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
        public Text ModalTitle, ModalBody, ModalConfirmLabel, ModalCancelLabel;
        public Button ModalConfirm, ModalCancel;

        public void Only(GameObject panel)
        {
            BattlePanel.SetActive(panel==BattlePanel);LobbyPanel.SetActive(panel==LobbyPanel);
            RelicPanel.SetActive(panel==RelicPanel);RewardPanel.SetActive(panel==RewardPanel);
        }
        public void SetModal(string title,string body,string confirm,string cancel,bool canConfirm)
        {
            ModalPanel.SetActive(true);ModalTitle.text=title;ModalBody.text=body;
            ModalConfirmLabel.text=confirm;ModalCancelLabel.text=cancel;
            ModalConfirm.interactable=canConfirm;ModalCancel.gameObject.SetActive(!string.IsNullOrEmpty(cancel));
        }
    }
}
