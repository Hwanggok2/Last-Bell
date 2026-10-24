using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    [Serializable] public sealed class DragonV5CompanionRow
    {
        public Button Select;
        public Image Portrait;
        public Text Name, State;
    }
    [Serializable] public sealed class DragonV5CompanionArt
    {
        public string Id, ArtId;
        public Sprite Portrait;
    }

    // Displays Core snapshots and confirmation dialogs. All spending, prices and
    // eligibility checks belong to GameSession. Every node is saved by the migration.
    public sealed class CompanionInnView : MonoBehaviour
    {
        public MvpView View;
        public RectTransform ListPanel, DetailPanel, RosterViewport, RosterContent;
        public ScrollRect RosterScroll;
        public Text PartyTitle, Name, Attack, Damage, ServiceTitle, ServiceSummary, UpgradeLabel, PartyLabel, DepartLabel;
        public Text SkillName, SkillEffect, SkillProgress;
        public Image Portrait;
        public Button Upgrade, Party, Service, Depart;
        public DragonV5CompanionRow[] Rows;
        public Button[] Slots;
        public Image[] SlotPortraits;
        public Text[] SlotLabels;
        public DragonV5CompanionArt[] ArtBindings;
        public GameObject Modal;
        public RectTransform ModalCard;
        public Text ModalTitle, ModalBody, ConfirmLabel;
        public Button Confirm, Cancel;
        public Button[] ReplaceSlots;
        public Text[] ReplaceLabels;
        public bool IsOpen {get;private set;}
        MvpPresenter owner;
        string selectedId;
        Action confirmation;
        ActorSprites selectedActor;
        Sprite selectedPortrait;
        float portraitTime;
        bool wired,portraitUnlocked;
        Vector2 lastSize;

        public void Wire(MvpPresenter presenter)
        {
            owner=presenter;if(wired)return;wired=true;
            for(int i=0;i<Rows.Length;i++){int index=i;Rows[i].Select.onClick.AddListener(()=>Select(index));}
            for(int i=0;i<Slots.Length;i++){int index=i;Slots[i].onClick.AddListener(()=>SelectSlot(index));}
            Upgrade.onClick.AddListener(UpgradeSelected);
            Party.onClick.AddListener(ChangeParty);
            Service.onClick.AddListener(OpenService);
            Depart.onClick.AddListener(()=>owner.Issue(()=>owner.Session.StartRun()));
            Confirm.onClick.AddListener(()=>{var action=confirmation;CloseModal();action?.Invoke();});
            Cancel.onClick.AddListener(CloseModal);
        }
        public void Open(){IsOpen=true;owner?.Refresh();}
        public void Close(){IsOpen=false;CloseModal();gameObject.SetActive(false);owner?.Refresh();}
        public void ResetSelection(){selectedId=null;selectedActor=null;portraitTime=0;Close();}
        void Select(int index)
        {
            if(owner.Session==null||index>=owner.Session.Companions.Count)return;
            selectedId=owner.Session.Companions[index].Id;portraitTime=0;CloseModal();owner.Refresh();
        }
        CompanionView Selected(GameSession session)
        {
            foreach(var companion in session.Companions)if(companion.Id==selectedId)return companion;
            return session.Companions.Count>0?session.Companions[0]:null;
        }
        DragonV5CompanionArt ArtFor(string id)
        {
            foreach(var binding in ArtBindings)if(binding.Id==id)return binding;
            return null;
        }
        CompanionView InSlot(GameSession s,int slot)
        {
            foreach(var companion in s.Companions)if(companion.EquippedSlot==slot)return companion;
            return null;
        }
        public void Render(GameSession session,ArtCatalog art)
        {
            bool visible=IsOpen&&View.Reference.UpgradeScreen;
            gameObject.SetActive(visible);if(!visible)return;
            var chosen=Selected(session);if(chosen==null)return;selectedId=chosen.Id;
            bool unlocked=chosen.Level>0;
            for(int i=0;i<Rows.Length;i++)
            {
                var row=Rows[i];row.Select.gameObject.SetActive(i<session.Companions.Count);if(i>=session.Companions.Count)continue;
                var item=session.Companions[i];var binding=ArtFor(item.Id);
                row.Name.text=item.Name;row.State.text=item.Level>0?$"Lv.{item.Level}"+(item.EquippedSlot>=0?" · 출전중":""):$"미발견 · Stage {item.JoinStage}";
                row.Portrait.sprite=binding?.Portrait;row.Portrait.color=binding!=null&&binding.Portrait?(item.Level>0?Color.white:DragonUiPalette.Ink):Color.clear;
                DragonUiTheme.Select(row.Select,item.Id==selectedId);
            }
            PartyTitle.text=$"출전 {session.EquippedCompanionIds.Count} / {session.CompanionSlots}";
            DepartLabel.text="Stage "+session.CheckpointStartStage;Depart.interactable=session.Phase==SessionPhase.Lobby;
            for(int i=0;i<Slots.Length;i++)
            {
                var member=InSlot(session,i);bool open=i<session.CompanionSlots;var binding=member==null?null:ArtFor(member.Id);
                SlotPortraits[i].sprite=binding?.Portrait;SlotPortraits[i].color=binding!=null&&binding.Portrait?Color.white:Color.clear;
                SlotLabels[i].text=member!=null?"":open?"+":"잠김";
                Slots[i].interactable=session.Phase==SessionPhase.Lobby&&(!open||member!=null||i==session.EquippedCompanionIds.Count);
                DragonUiTheme.Select(Slots[i],member!=null&&member.Id==selectedId);
            }
            Name.text=chosen.Name+(unlocked?$"  Lv.{chosen.Level}":"");
            Attack.text=unlocked?$"{chosen.AttackName} · {chosen.PeriodMs/1000d:0.#}초마다":$"Stage {chosen.JoinStage}에서 만나요";
            Damage.text=unlocked?$"지원 피해 {chosen.DamageMilli/1000d:#,0.##}"+(chosen.Level<chosen.LevelCap?$" → {chosen.NextDamageMilli/1000d:#,0.##}":""):"아직 만나지 못한 동료";
            bool skillDetails=View.Reference.RestoreUpgradeV6;
            if(skillDetails)
            {
                bool grows=unlocked&&chosen.Level<chosen.LevelCap;
                Attack.text=unlocked?chosen.AttackName:$"Stage {chosen.JoinStage}에서 만나요";
                Damage.text=unlocked?$"초당 지원 피해 {chosen.ExpectedDpsMilli/1000d:#,0.###}"+(grows?$" → {chosen.NextExpectedDpsMilli/1000d:#,0.###}":"")+
                    $"\n공격 주기 {chosen.PeriodMs/1000d:0.###}초"+(grows?$" → {chosen.NextPeriodMs/1000d:0.###}초":""):"아직 만나지 못한 동료";
                SkillName.text=chosen.SkillName;
                SkillEffect.text=chosen.SkillDescription;
                SkillProgress.text=unlocked?$"발동 확률 {chosen.SkillChancePercent}%"+(grows?$" → {chosen.NextSkillChancePercent}%":""):"동료를 만나면 사용할 수 있어요";
            }
            if(!skillDetails&&unlocked&&chosen.Level>=chosen.LevelCap&&chosen.LevelCap<session.Catalog.adventure.companionMaxLevel)
            {
                var rules=session.Catalog.adventure;int next=Array.IndexOf(rules.levelCaps,chosen.LevelCap)+1;
                if(next>0&&next<rules.levelCaps.Length)Damage.text+=$"\nStage {rules.levelCapBosses[next]} 보스 처치 후 Lv.{rules.levelCaps[next]}";
            }
            var selectedBinding=ArtFor(selectedId);selectedPortrait=selectedBinding?.Portrait;portraitUnlocked=unlocked;
            var actor=art&&selectedBinding!=null&&!string.IsNullOrEmpty(selectedBinding.ArtId)?art.Find(selectedBinding.ArtId):null;
            if(actor!=selectedActor){selectedActor=actor;portraitTime=0;}
            SamplePortrait(0);if(!unlocked&&Portrait.sprite)Portrait.color=DragonUiPalette.Ink;
            UpgradeLabel.text=!unlocked?"미발견":chosen.Level>=chosen.LevelCap?(chosen.LevelCap<session.Catalog.adventure.companionMaxLevel?"레벨 한도":"최대 레벨"):$"강화 {chosen.UpgradeCost:N0}G";
            if(skillDetails&&unlocked&&chosen.Level>=chosen.LevelCap&&chosen.LevelCap<session.Catalog.adventure.companionMaxLevel)
            {
                var rules=session.Catalog.adventure;int next=Array.IndexOf(rules.levelCaps,chosen.LevelCap)+1;
                if(next>0&&next<rules.levelCaps.Length)UpgradeLabel.text=$"{rules.levelCapBosses[next]} 보스 후 강화";
            }
            Upgrade.interactable=unlocked&&chosen.Level<chosen.LevelCap&&session.Phase==SessionPhase.Lobby;
            PartyLabel.text=chosen.EquippedSlot>=0?"빼기":"출전";
            Party.interactable=unlocked&&session.Phase==SessionPhase.Lobby;
            ServiceTitle.text=session.ServiceName(chosen.ServiceId);
            ServiceSummary.text=!unlocked?"동료를 만나면 열려요":ServiceState(session,chosen.ServiceId);
            Service.interactable=unlocked&&session.Phase==SessionPhase.Lobby;
            if(session.Phase!=SessionPhase.Lobby)CloseModal();
            Layout(View.SafeRoot.rect.width,View.SafeRoot.rect.height);
        }
        public void SamplePortrait(float deltaTime)
        {
            if(!IsOpen)return;portraitTime+=Mathf.Max(0,deltaTime);
            var idle=selectedActor?.FindClip(ActorMotion.Idle);
            var sprite=idle!=null&&idle.frames.Length>0?idle.frames[idle.FrameIndex(portraitTime)]:selectedPortrait;
            Portrait.sprite=sprite;Portrait.color=sprite?Color.white:Color.clear;
            if(!portraitUnlocked&&sprite)Portrait.color=DragonUiPalette.Ink;
        }
        static string ServiceState(GameSession session,string id)
        {
            return session.ServiceEffect(id)+"\n"+(session.IsServiceReserved(id)?"예약됨 · 취소 가능":"자세히 보기");
        }
        void UpgradeSelected()
        {
            var s=owner.Session;var selected=Selected(s);if(selected==null)return;
            if(selected.UpgradeCost>s.Gold){Exchange(selected.UpgradeCost);return;}
            string id=selected.Id;owner.Issue(()=>PurchaseWithFeedback(()=>s.UpgradeCompanion(id),Upgrade,SkillPixelEffects.SupportKind.Upgrade));
        }
        void SelectSlot(int slot)
        {
            var s=owner.Session;if(slot>=s.CompanionSlots){ShowSlotPurchase();return;}
            var current=InSlot(s,slot);
            if(current!=null){selectedId=current.Id;owner.Refresh();return;}
            var chosen=Selected(s);if(chosen==null||chosen.Level<=0||chosen.EquippedSlot>=0)
            {ShowModal("빈 자리","출전할 동료를 왼쪽 목록에서 골라 주세요.","",false,null);return;}
            ConfirmEquip(chosen,slot);
        }
        void ChangeParty()
        {
            var s=owner.Session;var chosen=Selected(s);if(chosen==null||chosen.Level<=0)return;
            if(chosen.EquippedSlot>=0)
            {
                int slot=chosen.EquippedSlot;
                ShowModal("동료 빼기",$"{chosen.Name}를 출전 편성에서 뺄까요?\n서비스는 계속 이용할 수 있어요.","빼기",true,()=>owner.Issue(()=>s.RemoveCompanion(slot)));return;
            }
            for(int i=0;i<s.CompanionSlots;i++)if(InSlot(s,i)==null){ConfirmEquip(chosen,i);return;}
            ShowModal("누구와 교대할까요?",$"{chosen.Name}와 교대할 동료를 골라 주세요.","",false,null);
            for(int i=0;i<ReplaceSlots.Length;i++)
            {
                int slot=i;var current=InSlot(s,i);ReplaceSlots[i].gameObject.SetActive(i<s.CompanionSlots);
                ReplaceLabels[i].text=current?.Name??"빈 자리";ReplaceSlots[i].onClick.RemoveAllListeners();
                ReplaceSlots[i].onClick.AddListener(()=>ConfirmEquip(chosen,slot));
            }
        }
        void ConfirmEquip(CompanionView chosen,int slot)
        {
            var s=owner.Session;var current=InSlot(s,slot);string id=chosen.Id;
            ShowModal(current==null?"함께 출전":"동료 교대",current==null?$"{slot+1}번째 자리에 {chosen.Name}를 편성할까요?":$"{current.Name} 대신 {chosen.Name}와 함께 갈까요?","출전",true,()=>owner.Issue(()=>s.EquipCompanion(id,slot)));
        }
        void ShowSlotPurchase()
        {
            var s=owner.Session;var quote=s.QuoteCompanionSlot();
            string body=s.CompanionSlots>=Slots.Length?"모든 출전 자리를 열었어요.":$"{quote.TargetSlots}번째 자리 · 보석 {quote.Cost:N0}개\n현재 보석 {s.Fragments:N0}개\n바룬 영입 · Stage {quote.RequiredBossStage} 보스 처치 필요";
            if(!quote.CanPurchase&&s.CompanionSlots<Slots.Length)body+="\n"+s.CompanionSlotLockReason;
            ShowModal("짐마차",body,"자리 열기",quote.CanPurchase,()=>owner.Issue(()=>s.PurchaseCompanionSlot(quote)));
        }
        void OpenService()
        {
            var s=owner.Session;var chosen=Selected(s);if(chosen==null||chosen.Level<=0)return;string id=chosen.ServiceId;
            if(id=="pack_cart"){ShowSlotPurchase();return;}
            if(id=="route_guide")
            {
                string regions="";
                foreach(int start in s.VisitedRegionStarts)
                {
                    int end=Math.Min(start+9,s.Catalog.mvpLastStage);var boss=s.Catalog.GetEnemy(s.Catalog.GetStage(end).enemyId);
                    regions+=$"\nStage {start}~{end} · {boss.displayName}";
                }
                ShowModal(s.ServiceName(id),s.ServiceEffect(id)+" · 적용 중\n"+s.ServiceDuration(id)+" · 편성 무관\n방문 지역"+regions,"",false,null);return;
            }
            if(id=="weapon_craft")
            {
                var quote=s.QuoteService(id);var rules=s.Catalog.adventure;int current=s.WeaponRank;
                string body=current>=rules.weaponAttackPercents.Length?"무기를 끝까지 강화했어요.":$"무기 {current}단계 → {current+1}단계\n{s.ServiceEffect(id)} · {s.ServiceDuration(id)}\n제작 비용 {quote.Cost:N0}G · 현재 {s.Gold:N0}G"+(rules.weaponBossRequirements[current]>0?$"\nStage {rules.weaponBossRequirements[current]} 보스 처치 필요":"");
                bool shortage=s.CanCraftWithoutGold&&quote.Cost>s.Gold;
                if(!quote.CanPurchase&&!shortage)body+="\n"+s.WeaponLockReason;
                ShowModal(s.ServiceName(id),body,shortage?"골드 교환":"제작",quote.CanPurchase||shortage,()=>{if(shortage)Exchange(quote.Cost);else owner.Issue(()=>PurchaseWithFeedback(()=>s.PurchaseService(quote),Service,SkillPixelEffects.SupportKind.Upgrade));});return;
            }
            bool reserved=s.IsServiceReserved(id);long price=s.ServicePrice(id);
            if(reserved){ShowModal(s.ServiceName(id),$"{s.ServiceEffect(id)}\n다음 출전에 적용하도록 예약했어요.\n취소하면 {s.ReservedServicePaidGold(id):N0}G를 돌려받아요.","예약 취소",true,()=>owner.Issue(()=>s.CancelService(id)));return;}
            bool missing=price>s.Gold;
            var reservation=s.QuoteService(id);
            ShowModal(s.ServiceName(id),$"{s.ServiceEffect(id)}\n{s.ServiceDuration(id)}\n가격 {price:N0}G · 현재 {s.Gold:N0}G",missing?"골드 교환":"예약",reservation.CanPurchase||missing,()=>{if(missing)Exchange(price);else owner.Issue(()=>PurchaseWithFeedback(()=>s.PurchaseService(reservation),Service,SkillPixelEffects.SupportKind.Blessing));});
        }
        bool PurchaseWithFeedback(Func<bool> command,Button button,SkillPixelEffects.SupportKind kind)
        {
            bool success=command();
            if(success&&owner.Presentation&&owner.Presentation.SkillEffects)
                owner.Presentation.SkillEffects.PlaySupport(kind,(RectTransform)button.transform);
            return success;
        }
        void Exchange(long cost){CloseModal();owner.Issue(()=>owner.Session.RequestPurchaseExchange(cost));}
        void ShowModal(string title,string body,string confirm,bool enabled,Action action)
        {
            Modal.SetActive(true);Modal.transform.SetAsLastSibling();ModalTitle.text=title;ModalBody.text=body;ConfirmLabel.text=confirm;
            Confirm.gameObject.SetActive(!string.IsNullOrEmpty(confirm));Confirm.interactable=enabled;confirmation=action;
            foreach(var button in ReplaceSlots)button.gameObject.SetActive(false);
            Layout(View.SafeRoot.rect.width,View.SafeRoot.rect.height);
        }
        void CloseModal(){if(Modal)Modal.SetActive(false);confirmation=null;}
        public void Layout(float width,float height)
        {
            if(width<=0||height<=0)return;lastSize=new Vector2(width,height);float sx=width/1280f,sy=height/720f;
            bool v6=View&&View.Reference&&View.Reference.RestoreUpgradeV6;
            Place(ListPanel,24,v6?112:208,376,v6?584:488,sx,sy);Place(DetailPanel,424,v6?112:208,832,v6?584:488,sx,sy);
            Place(PartyTitle.transform,24,14,328,40,sx,sy);
            for(int i=0;i<Slots.Length;i++){Place(Slots[i].transform,24+112*i,64,96,96,sx,sy);Place(SlotPortraits[i].transform,24,16,48,48,sx,sy);FillLabel(SlotLabels[i],sx,sy,24);}
            Place(RosterViewport,16,176,344,v6?400:304,sx,sy);RosterContent.anchorMin=RosterContent.anchorMax=RosterContent.pivot=new Vector2(0,1);
            RosterContent.sizeDelta=new Vector2(344*sx,Rows.Length*100*sy);
            for(int i=0;i<Rows.Length;i++)
            {
                var row=Rows[i];Place(row.Select.transform,0,i*100,344,96,sx,sy);Place(row.Portrait.transform,28,24,48,48,sx,sy);
                Place(row.Name.transform,108,8,224,36,sx,sy);Place(row.State.transform,108,44,224,44,sx,sy);Font(row.Name,28,sy);Font(row.State,20,sy);
            }
            Place(Portrait.transform,32,32,144,144,sx,sy);Place(Name.transform,208,28,576,48,sx,sy);Place(Attack.transform,208,86,576,40,sx,sy);
            Place(Damage.transform,208,138,576,88,sx,sy);Place(Service.transform,32,234,768,96,sx,sy);
            Place(ServiceTitle.transform,20,12,200,32,sx,sy);Place(ServiceSummary.transform,232,8,516,80,sx,sy);
            Place(Upgrade.transform,32,364,264,96,sx,sy);Place(Party.transform,328,364,200,96,sx,sy);
            Place(Depart.transform,560,364,240,96,sx,sy);FillLabel(DepartLabel,sx,sy,30);
            Font(PartyTitle,28,sy);Font(Name,36,sy);Font(Attack,28,sy);Font(Damage,26,sy);Font(ServiceTitle,28,sy);Font(ServiceSummary,26,sy);
            if(v6)
            {
                Place(Name.transform,208,24,576,48,sx,sy);Place(Attack.transform,208,82,576,40,sx,sy);Place(Damage.transform,208,130,576,80,sx,sy);Font(Damage,26,sy);
                Place(SkillName.transform,32,216,768,36,sx,sy);Place(SkillEffect.transform,32,256,768,40,sx,sy);Place(SkillProgress.transform,32,302,768,36,sx,sy);
                Font(SkillName,28,sy);Font(SkillEffect,26,sy);Font(SkillProgress,26,sy);
                Place(Service.transform,32,356,768,96,sx,sy);
                Place(Upgrade.transform,32,464,264,96,sx,sy);Place(Party.transform,328,464,200,96,sx,sy);Place(Depart.transform,560,464,240,96,sx,sy);
            }
            FillLabel(UpgradeLabel,sx,sy,28);FillLabel(PartyLabel,sx,sy,28);
            Place(ModalCard,160,96,960,536,sx,sy);Place(ModalTitle.transform,32,24,896,56,sx,sy);Place(ModalBody.transform,40,100,880,242,sx,sy);
            Place(Confirm.transform,512,400,400,96,sx,sy);Place(Cancel.transform,48,400,400,96,sx,sy);
            for(int i=0;i<ReplaceSlots.Length;i++){Place(ReplaceSlots[i].transform,48+i*296,276,272,96,sx,sy);FillLabel(ReplaceLabels[i],sx,sy,28);}
            Font(ModalTitle,36,sy);Font(ModalBody,30,sy);FillLabel(ConfirmLabel,sx,sy,30);FillLabel(Cancel.GetComponentInChildren<Text>(),sx,sy,30);
            foreach(var panel in GetComponentsInChildren<ReferencePanelGraphic>(true))panel.Cell=4*sy;
        }
        void OnRectTransformDimensionsChange(){if(View&&ListPanel&&ModalCard&&Depart&&Rows!=null&&ReplaceSlots!=null&&View.SafeRoot.rect.size!=lastSize)Layout(View.SafeRoot.rect.width,View.SafeRoot.rect.height);}
        static void Place(Transform t,float x,float y,float w,float h,float sx,float sy)=>FeedbackLayout.Box(t,x*sx,y*sy,w*sx,h*sy);
        static void Font(Text text,int size,float scale){text.fontSize=Mathf.Max(12,Mathf.RoundToInt(size*scale));text.resizeTextForBestFit=false;text.transform.localScale=Vector3.one;}
        static void FillLabel(Text text,float sx,float sy,int fontSize){var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(8*sx,4*sy);r.offsetMax=new Vector2(-8*sx,-4*sy);Font(text,fontSize,sy);}
    }
}
