using System;
using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    [Serializable] public sealed class DragonV5GrowthRow
    {
        public Button Plus;
        public Image[] Pips;
    }
    // Saved UI hierarchy. This view holds only selection/quote presentation state.
    public sealed class ReferenceLobbyView : MonoBehaviour
    {
        public GameObject Selection, Upgrade;
        public Text Heading, Summary, UpgradeTitle, UpgradeLevel, Before, After, Price, Note;
        public Text SelectionStats, RelicCount, StartStage, UpgradeWallet, RelicHeading;
        public Text[] StatRatings;
        public RelicIconGraphic CollectionIcon;
        public Text PlayerHealth, EnemyHealth, PlayerName, BattleFragments;
        public Button Back, OneLevel, Maximum, Purchase, Exchange, SettlementContinue;
        public Button[] StatButtons;
        public Text[] StatLabels;
        public Image UpgradePortrait;
        public Button GrowthTab, CompanionsTab;
        public Text InnWallet;
        public DragonV5GrowthRow[] GrowthRows;
        public bool RestoreUpgradeV6;
        public bool UpgradeScreen {get;private set;}
        public int SelectedStat {get;private set;}
        public bool MaximumMode {get;private set;}
        string inspected;
        UpgradeQuote quote;
        MvpPresenter owner;
        ActorSprites portraitActor;
        float portraitTime;
        static readonly string[] Names={"공격력","체력","행운","기세","회복력"};
        public void Wire(MvpPresenter p)
        {
            owner=p;
            for(int i=0;i<p.View.Characters.Length;i++){int index=i;p.View.Characters[i].onClick.AddListener(()=>{inspected=p.Session.Catalog.characters[index].id;p.Refresh();});}
            for(int i=0;i<StatButtons.Length;i++){int index=i;StatButtons[i].onClick.AddListener(()=>{SelectedStat=index;p.Refresh();});}
            if(GrowthRows!=null)for(int i=0;i<GrowthRows.Length;i++)
            {
                int index=i;GrowthRows[i].Plus.onClick.AddListener(()=>
                {
                    SelectedStat=index;MaximumMode=false;
                    p.Issue(()=>p.Session.GetUpgradeCost(index)>p.Session.Gold?
                        p.Session.RequestUpgradeExchange(index):PurchaseWithFeedback(()=>p.Session.PurchaseUpgrade(p.Session.QuoteUpgrade(index,false)),GrowthRows[index].Plus));
                });
            }
            if(GrowthTab)GrowthTab.onClick.AddListener(()=>{if(p.View.Companions)p.View.Companions.Close();p.Refresh();});
            if(CompanionsTab)CompanionsTab.onClick.AddListener(()=>
            {
                if(!p.View.Companions)return;
                if(p.View.Companions.IsOpen){p.View.Companions.Close();return;}
                if(!UpgradeScreen)
                {
                    if(!RestoreUpgradeV6||!p.Session.SelectCharacter(inspected??p.Session.CurrentCharacterId))return;
                    UpgradeScreen=true;
                }
                p.View.Companions.Open();
            });
            p.View.OpenRelics.onClick.AddListener(()=>{if(p.View.Companions)p.View.Companions.Close();});
            p.View.StartRun.onClick.AddListener(()=>
            {
                if(!UpgradeScreen){if(p.Session.SelectCharacter(inspected)){UpgradeScreen=true;p.Refresh();}}
                else p.Issue(()=>p.Session.StartRun());
            });
            Back.onClick.AddListener(()=>
            {
                if(RestoreUpgradeV6&&p.View.Companions&&p.View.Companions.IsOpen){p.View.Companions.Close();return;}
                UpgradeScreen=false;if(p.View.Companions)p.View.Companions.Close();p.Refresh();
            });
            OneLevel.onClick.AddListener(()=>{MaximumMode=false;p.Refresh();});
            Maximum.onClick.AddListener(()=>{MaximumMode=true;p.Refresh();});
            Purchase.onClick.AddListener(()=>p.Issue(()=>PurchaseWithFeedback(()=>p.Session.PurchaseUpgrade(quote),Purchase)));
            Exchange.onClick.AddListener(()=>p.Issue(()=>p.Session.RequestUpgradeExchange(SelectedStat)));
            SettlementContinue.onClick.AddListener(()=>p.Issue(()=>p.Session.ContinueSettlement()));
        }
        public void ResetSelection(){UpgradeScreen=false;inspected=null;MaximumMode=false;portraitActor=null;portraitTime=0;if(owner&&owner.View.Companions)owner.View.Companions.ResetSelection();}
        bool PurchaseWithFeedback(Func<bool> command,Button button)
        {
            bool success=command();
            if(success&&owner.Presentation&&owner.Presentation.SkillEffects)
                owner.Presentation.SkillEffects.PlaySupport(SkillPixelEffects.SupportKind.Upgrade,(RectTransform)button.transform);
            return success;
        }
        public void SamplePortrait(float deltaTime)
        {
            portraitTime+=Mathf.Max(0,deltaTime);
            var idle=portraitActor?.FindClip(ActorMotion.Idle);
            var sprite=idle!=null&&idle.frames.Length>0?idle.frames[idle.FrameIndex(portraitTime)]:
                portraitActor!=null&&portraitActor.idle.Length>0?portraitActor.idle[0]:null;
            owner.View.LobbyPortrait.sprite=sprite;owner.View.LobbyPortrait.color=sprite?Color.white:Color.clear;
            if(UpgradePortrait){UpgradePortrait.sprite=sprite;UpgradePortrait.color=sprite?Color.white:Color.clear;}
        }
        static string Value(CombatStatsData s,int i)=>i==0?s.attack.ToString("N0"):i==1?s.maxHp.ToString("N0"):i==2?$"치명 {s.critChancePercent}% · 골드 {s.goldBonusPercent}%":i==3?s.stunDamagePercent+"%":$"승리 후 HP +{s.recovery:N0}";
        public void Render(GameSession s,ArtCatalog art)
        {
            var v=owner.View;if(string.IsNullOrEmpty(inspected))inspected=s.CurrentCharacterId;
            var c=s.Catalog.GetCharacter(UpgradeScreen?s.CurrentCharacterId:inspected);
            bool inn=GrowthTab&&!RestoreUpgradeV6,companions=UpgradeScreen&&v.Companions&&v.Companions.IsOpen;
            v.StartRun.gameObject.SetActive(!companions);
            Selection.SetActive(!UpgradeScreen);Upgrade.SetActive(UpgradeScreen&&!companions);Back.gameObject.SetActive(UpgradeScreen);if(v.ReplayPrologue)v.ReplayPrologue.gameObject.SetActive(!UpgradeScreen);
            if(GrowthTab){GrowthTab.gameObject.SetActive(UpgradeScreen&&!RestoreUpgradeV6);CompanionsTab.gameObject.SetActive(RestoreUpgradeV6||UpgradeScreen);DragonUiTheme.Select(GrowthTab,!companions);DragonUiTheme.Select(CompanionsTab,companions);}
            if(InnWallet){InnWallet.gameObject.SetActive(RestoreUpgradeV6?companions:UpgradeScreen);InnWallet.text=$"골드 {s.Gold:N0}    보석 {s.Fragments:N0}";}
            RelicCount.gameObject.SetActive(!UpgradeScreen);if(v.AudioCredits)v.AudioCredits.OpenButton.gameObject.SetActive(!UpgradeScreen);
            Heading.text=RestoreUpgradeV6&&companions?"동료":UpgradeScreen&&inn?"여관 · "+c.displayName.Split('·')[0].Trim():UpgradeScreen?"업그레이드":v.Dragon?"모험가 선택":"용사 선택";
            v.Wallet.text=v.Dragon?s.Fragments.ToString("N0"):$"♦ {s.Fragments:N0}";
            v.StartLabel.text=UpgradeScreen&&(inn||RestoreUpgradeV6)?"Stage "+s.CheckpointStartStage:UpgradeScreen?"전투 시작":c.availableInMvp?"선택":"제작 중";
            v.StartRun.interactable=c.availableInMvp;v.CharacterName.text=c.displayName.Split('·')[0].Trim();
            var actor=art?art.Find(c.artId):null;
            if(portraitActor!=actor){portraitActor=actor;portraitTime=0;}
            SamplePortrait(0);
            int count=Math.Min(Names.Length,c.stars.Length),total=0,rating=0;for(int i=0;i<count;i++){total+=s.GetStatLevel(i);rating+=c.stars[i];}
            var skill=s.Catalog.FindSkill(c.id);
            Summary.text=$"{v.CharacterName.text} - 평가 : {rating} / {count*5}\n"+
                (c.availableInMvp?$"스킬 : {(skill==null?"—":skill.displayName+" · "+(skill.cooldownMs/1000)+"초")}\n레벨 : {total} - 스테이지 도달 : {s.HighestStage}":"스킬 : 제작 중\n아직 선택할 수 없습니다.");
            var stats=s.CharacterStats(c.id);
            SelectionStats.text=$"공격력 : {stats.attack:N0}\n체력 : {stats.maxHp:N0}\n행운 : 치명 {stats.critChancePercent}% / 골드 {stats.goldBonusPercent}%\n기세 : {stats.stunDamagePercent}%\n회복력 : 승리 후 HP +{stats.recovery:N0}\n소지 골드 : {s.Gold:N0}";
            if(v.Dragon)SelectionStats.text=DragonUiPalette.Copy(SelectionStats.text);
            RelicCount.text=$"보유 {s.OwnedRelicCount} / {s.StorageCapacity}";
            StartStage.text="Stage "+s.CheckpointStartStage;StartStage.gameObject.SetActive(UpgradeScreen&&!inn&&!RestoreUpgradeV6);
            for(int i=0;i<v.Characters.Length;i++)
            {
                var ch=s.Catalog.characters[i];v.Characters[i].interactable=true;
                v.CharacterLabels[i].text=ch.displayName.Split('·')[0].Trim()+(ch.availableInMvp?"":" (제작 중)");
                v.CharacterLabels[i].color=v.Dragon?(ch.availableInMvp?DragonUiPalette.Ink:DragonUiPalette.Empty):ch.availableInMvp?new Color32(239,218,176,255):new Color32(143,141,129,255);
                if(v.Dragon)DragonUiTheme.Select(v.Characters[i],ch.id==c.id);
            }
            for(int i=0;i<Math.Min(v.Stats.Length,count);i++)
            {
                var row=v.Stats[i];row.Title.text=Names[i];row.Value.text=Value(stats,i);
                row.Upgrade.gameObject.SetActive(false);row.Level.gameObject.SetActive(false);if(row.Price)row.Price.gameObject.SetActive(false);
                foreach(var b in row.Bells)b.gameObject.SetActive(false);
                for(int j=0;j<5;j++)row.Stars[j].SetFilled(j<c.stars[i]);
            }
            for(int i=0;i<Math.Min(StatButtons.Length,count);i++)
            {
                int level=s.GetStatLevel(i);
                StatLabels[i].text=Names[i]+(inn?$" Lv.{level}":"");StatRatings[i].text=new string('★',c.stars[i]);
                if(v.Dragon)StatRatings[i].color=DragonUiPalette.Gold;
                StatLabels[i].color=v.Dragon?DragonUiPalette.Ink:SelectedStat==i?new Color32(255,232,174,255):new Color32(195,182,151,255);
                if(v.Dragon)DragonUiTheme.Select(StatButtons[i],SelectedStat==i);
                if(GrowthRows!=null&&i<GrowthRows.Length)
                {
                    int filled=level==0?0:(level-1)%10+1;
                    GrowthRows[i].Plus.gameObject.SetActive(!RestoreUpgradeV6);
                    foreach(var pip in GrowthRows[i].Pips){pip.gameObject.SetActive(!RestoreUpgradeV6);pip.color=Array.IndexOf(GrowthRows[i].Pips,pip)<filled?DragonUiPalette.Gold:DragonUiPalette.Disabled;}
                    GrowthRows[i].Plus.interactable=level<s.Catalog.growth.maxLevel&&s.Phase==SessionPhase.Lobby;
                }
            }
            v.OpenRelics.gameObject.SetActive(!RestoreUpgradeV6||!companions);
            bool showWallet=RestoreUpgradeV6?!companions:!inn||!UpgradeScreen;
            v.Wallet.gameObject.SetActive(showWallet);if(v.Dragon)v.Dragon.WalletGem.gameObject.SetActive(showWallet);
            UpgradeWallet.gameObject.SetActive(RestoreUpgradeV6||!inn);
            if(!UpgradeScreen||companions)return;
            quote=s.QuoteUpgrade(SelectedStat,MaximumMode);int lv=s.GetStatLevel(SelectedStat);
            UpgradeTitle.text=$"{Names[SelectedStat]} lv.{lv} / {s.Catalog.growth.maxLevel}";UpgradeLevel.text="";
            if(v.Dragon)UpgradeTitle.text=DragonUiPalette.Copy(UpgradeTitle.text);
            Before.text=quote.ToLevel>lv?Value(quote.Before,SelectedStat)+" → "+Value(quote.After,SelectedStat):Value(quote.Before,SelectedStat)+" (MAX)";
            After.text=SelectedStat==0?"공격 피해":SelectedStat==1?"최대 HP":SelectedStat==2?"치명타 확률 · 클리어 골드":SelectedStat==3?"기절한 적에게 주는 피해":"적을 처치하면 HP 회복";
            Price.text=lv>=50?"최대 레벨":$"{quote.Cost:N0} 골드 필요";
            UpgradeWallet.text=$"소지 골드 :\n{s.Gold:N0}";
            long missing=Math.Max(0,s.GetUpgradeCost(SelectedStat)-s.Gold);
            Note.text=missing>0?$"골드 {missing:N0} 부족":MaximumMode&&quote.ToLevel>lv?$"lv.{lv} → {quote.ToLevel}":"";
            Purchase.interactable=quote.ToLevel>lv&&quote.Cost<=s.Gold;
            Exchange.gameObject.SetActive(lv<s.Catalog.growth.maxLevel&&missing>0);v.OpenRelics.gameObject.SetActive(inn||!Exchange.gameObject.activeSelf);
            OneLevel.GetComponentInChildren<Text>().text=v.Dragon?"+1":"↑\nLv";
            Maximum.GetComponentInChildren<Text>().text=v.Dragon?"최대":"↑\nMax";
            if(v.Dragon){DragonUiTheme.Select(OneLevel,!MaximumMode);DragonUiTheme.Select(Maximum,MaximumMode);}
        }
    }
}
