using System;
using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    // Saved UI hierarchy. This view holds only selection/quote presentation state.
    public sealed class ReferenceLobbyView : MonoBehaviour
    {
        public GameObject Selection, Upgrade;
        public Text Heading, Summary, UpgradeTitle, UpgradeLevel, Before, After, Price, Note;
        public Text PlayerHealth, EnemyHealth, PlayerName, BattleFragments;
        public Button Back, OneLevel, Maximum, Purchase, Exchange, SettlementContinue;
        public Button[] StatButtons;
        public Text[] StatLabels;
        public Image UpgradePortrait;
        public bool UpgradeScreen {get;private set;}
        public int SelectedStat {get;private set;}
        public bool MaximumMode {get;private set;}
        string inspected;
        UpgradeQuote quote;
        MvpPresenter owner;
        static readonly string[] Names={"공격력","체력","행운","공명"};
        public void Wire(MvpPresenter p)
        {
            owner=p;
            for(int i=0;i<p.View.Characters.Length;i++){int index=i;p.View.Characters[i].onClick.AddListener(()=>{inspected=p.Session.Catalog.characters[index].id;p.Refresh();});}
            for(int i=0;i<StatButtons.Length;i++){int index=i;StatButtons[i].onClick.AddListener(()=>{SelectedStat=index;p.Refresh();});}
            p.View.StartRun.onClick.AddListener(()=>
            {
                if(!UpgradeScreen){if(p.Session.SelectCharacter(inspected)){UpgradeScreen=true;p.Refresh();}}
                else p.Issue(()=>p.Session.StartRun());
            });
            Back.onClick.AddListener(()=>{UpgradeScreen=false;p.Refresh();});
            OneLevel.onClick.AddListener(()=>{MaximumMode=false;p.Refresh();});
            Maximum.onClick.AddListener(()=>{MaximumMode=true;p.Refresh();});
            Purchase.onClick.AddListener(()=>p.Issue(()=>p.Session.PurchaseUpgrade(quote)));
            Exchange.onClick.AddListener(()=>p.Issue(()=>p.Session.RequestUpgradeExchange(SelectedStat)));
            SettlementContinue.onClick.AddListener(()=>p.Issue(()=>p.Session.ContinueSettlement()));
        }
        public void ResetSelection(){UpgradeScreen=false;inspected=null;MaximumMode=false;}
        static string Value(CombatStatsData s,int i)=>i==0?s.attack.ToString("N0"):i==1?s.maxHp.ToString("N0"):i==2?$"치명 {s.critChancePercent}% · 골드 {s.goldBonusPercent}%":s.stunDamagePercent+"%";
        public void Render(GameSession s,ArtCatalog art)
        {
            var v=owner.View;if(string.IsNullOrEmpty(inspected))inspected=s.CurrentCharacterId;
            var c=s.Catalog.GetCharacter(UpgradeScreen?s.CurrentCharacterId:inspected);
            Selection.SetActive(!UpgradeScreen);Upgrade.SetActive(UpgradeScreen);Back.gameObject.SetActive(UpgradeScreen);if(v.ReplayPrologue)v.ReplayPrologue.gameObject.SetActive(!UpgradeScreen);
            Heading.text=UpgradeScreen?"능력치 강화":"종지기 선택";
            v.Wallet.text=$"골드 {MvpPresenter.WalletAmount(s.Gold)}   종 파편 {s.Fragments:N0}";
            v.StartLabel.text=UpgradeScreen?"전투 시작":c.availableInMvp?"선택":"제작 중";
            v.StartRun.interactable=c.availableInMvp;v.CharacterName.text=c.displayName.Split('·')[0].Trim();
            var actor=art?art.Find(c.artId):null;
            var sprite=actor!=null&&actor.idle.Length>0?actor.idle[0]:null;
            v.LobbyPortrait.sprite=sprite;v.LobbyPortrait.color=sprite?Color.white:Color.clear;
            if(UpgradePortrait){UpgradePortrait.sprite=sprite;UpgradePortrait.color=sprite?Color.white:Color.clear;}
            int total=0;for(int i=0;i<4;i++)total+=s.GetStatLevel(i);
            Summary.text=c.availableInMvp?$"강화 Lv.{total}   최고 Stage {s.HighestStage}\n시작 Stage {s.CheckpointStartStage}":"제작 중인 종지기입니다.";
            var stats=s.CharacterStats(c.id);
            for(int i=0;i<v.Characters.Length;i++)
            {
                var ch=s.Catalog.characters[i];v.Characters[i].interactable=true;
                v.CharacterLabels[i].text=(ch.id==c.id?"◆ ":"")+ch.displayName.Split('·')[0].Trim()+(ch.availableInMvp?"":"\n제작 중");
            }
            for(int i=0;i<4;i++)
            {
                var row=v.Stats[i];row.Title.text=Names[i];row.Value.text=Value(stats,i);
                row.Upgrade.gameObject.SetActive(false);row.Level.gameObject.SetActive(false);if(row.Price)row.Price.gameObject.SetActive(false);
                foreach(var b in row.Bells)b.gameObject.SetActive(false);
                for(int j=0;j<5;j++)row.Stars[j].SetFilled(j<c.stars[i]);
                StatLabels[i].text=(SelectedStat==i?"◆ ":"")+Names[i]+"\n"+new string('★',c.stars[i])+new string('☆',5-c.stars[i]);
            }
            v.OpenRelics.gameObject.SetActive(true);if(!UpgradeScreen)return;
            quote=s.QuoteUpgrade(SelectedStat,MaximumMode);int lv=s.GetStatLevel(SelectedStat);
            UpgradeTitle.text=Names[SelectedStat];UpgradeLevel.text=$"Lv.{lv} / {s.Catalog.growth.maxLevel}";
            Before.text="현재  "+Value(quote.Before,SelectedStat);After.text=quote.ToLevel>lv?"강화 후  "+Value(quote.After,SelectedStat):"현재 최대 강화";
            Price.text=lv>=50?"MAX":$"Lv.{lv} → {quote.ToLevel}   {MvpPresenter.WalletAmount(quote.Cost)} G";
            long missing=Math.Max(0,s.GetUpgradeCost(SelectedStat)-s.Gold);
            Note.text=lv>=50?"최대 레벨입니다.":missing>0?$"다음 강화에 {MvpPresenter.WalletAmount(missing)} G 부족":"11 · 21 · 31 · 41레벨에서 돌파합니다.";
            Purchase.interactable=quote.ToLevel>lv&&quote.Cost<=s.Gold;
            Exchange.gameObject.SetActive(lv<50&&missing>0);v.OpenRelics.gameObject.SetActive(!Exchange.gameObject.activeSelf);
            OneLevel.GetComponentInChildren<Text>().text=MaximumMode?"1Lv.":"◆ 1Lv.";
            Maximum.GetComponentInChildren<Text>().text=MaximumMode?"◆ MAX":"MAX";
        }
    }
}
