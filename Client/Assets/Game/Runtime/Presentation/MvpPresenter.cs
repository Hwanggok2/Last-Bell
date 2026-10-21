using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastBell
{
    [DefaultExecutionOrder(100)]
    public sealed class MvpPresenter : MonoBehaviour
    {
        public MvpView View;
        public Camera WorldCamera;
        public Transform WorldRoot;
        public SpriteRenderer Background;
        public ActorView PlayerActor, EnemyActor;
        public BattleEffects Effects;
        public ArtCatalog Art;
        // Bootstrap creates/saves the session. Only this Update advances its clock by default.
        public bool TickSession = true;
        public GameSession Session { get; private set; }
        readonly List<string> relicIds = new List<string>();
        string selectedRelic;
        string[] rewardIds = new string[3];
        bool relicScreen,dirty=true,wired,blocking;
        float feedbackTime;
        Action modalConfirm;
        Canvas viewCanvas;
        Color cream=new Color(.94f,.83f,.62f),amber=new Color(1,.65f,.17f),muted=new Color(.35f,.43f,.49f);

        void Awake(){Wire();}
        public void Bind(GameSession session)
        {
            Unsubscribe();Session=session;Wire();
            if(Session!=null){Session.Changed+=MarkDirty;Session.EventRaised+=OnSessionEvent;}
            dirty=true;Render();
        }
        void Unsubscribe(){if(Session!=null){Session.Changed-=MarkDirty;Session.EventRaised-=OnSessionEvent;}}
        void OnDestroy(){Unsubscribe();}
        void MarkDirty(){dirty=true;}
        void Wire()
        {
            if(wired||!View)return;wired=true;viewCanvas=View.GetComponentInParent<Canvas>();
            for(int i=0;i<View.Dodge.Length;i++){int index=i;View.Dodge[i].Pressed+=()=>CombatCommand(()=>Session.Dodge((DodgeDirection)index));}
            View.Attack.Pressed+=()=>CombatCommand(()=>Session.Attack());
            View.StartRun.onClick.AddListener(()=>{relicScreen=false;Command(()=>Session.StartRun());});
            View.OpenRelics.onClick.AddListener(()=>{relicScreen=true;dirty=true;});
            View.RelicBack.onClick.AddListener(()=>{relicScreen=false;dirty=true;});
            for(int i=0;i<View.Stats.Length;i++){int index=i;View.Stats[i].Upgrade.onClick.AddListener(()=>Command(()=>Session.Upgrade(index)));}
            for(int i=0;i<View.Characters.Length;i++){int index=i;View.Characters[i].onClick.AddListener(()=>{if(Session!=null&&index<Session.Catalog.characters.Length)Command(()=>Session.SelectCharacter(Session.Catalog.characters[index].id));});}
            for(int i=0;i<View.RelicItems.Length;i++){int index=i;View.RelicItems[i].onClick.AddListener(()=>{if(index<relicIds.Count){selectedRelic=relicIds[index];dirty=true;}});}
            for(int i=0;i<View.Rewards.Length;i++){int index=i;View.Rewards[i].Receive.onClick.AddListener(()=>{if(!string.IsNullOrEmpty(rewardIds[index]))Command(()=>Session.ChooseRelic(rewardIds[index]));});}
            View.RelicEquip.onClick.AddListener(()=>{if(string.IsNullOrEmpty(selectedRelic))return;Command(()=>IsEquipped(selectedRelic)?Session.UnequipRelic(selectedRelic):Session.EquipRelic(selectedRelic));});
            View.SlotPurchase.onClick.AddListener(()=>Command(()=>Session.BuyNextSlot()));
            View.Pause.onClick.AddListener(TogglePause);View.Resume.onClick.AddListener(TogglePause);
            View.ReturnHome.onClick.AddListener(()=>{Session?.SetPaused(false);Command(()=>Session.ReturnToLobby());});
            View.ModalConfirm.onClick.AddListener(()=>modalConfirm?.Invoke());
            View.ModalCancel.onClick.AddListener(()=>Command(()=>Session.CancelExchange()));
            View.ReducedShake.onValueChanged.AddListener(value=>{Effects.ReducedShake=value;PlayerPrefs.SetInt("LastBell.ReducedShake",value?1:0);});
            View.ReducedShake.isOn=PlayerPrefs.GetInt("LastBell.ReducedShake",0)!=0;
        }
        void Command(Func<bool> command)
        {
            if(Session==null||blocking)return;if(!command())Toast(Session.LastMessage);dirty=true;
        }
        void CombatCommand(Func<bool> command){if(Session==null||blocking||Session.IsPaused)return;command();dirty=true;}
        public void ShowBlockingMessage(string title,string body,Action retry)
        {
            blocking=true;if(!View)return;View.PausePanel.SetActive(false);
            View.SetModal(title,body,"다시 시도","",retry!=null);View.ModalPanel.transform.SetAsLastSibling();modalConfirm=retry;
        }
        public void ClearBlockingMessage(){blocking=false;if(View)View.ModalPanel.SetActive(false);dirty=true;Render();}
        void TogglePause()
        {
            if(Session==null||blocking||Session.Phase==SessionPhase.RewardChoice||Session.Phase==SessionPhase.ExchangeConfirmation)return;
            Session.SetPaused(!Session.IsPaused);dirty=true;
        }
        void Update()
        {
            if(Session==null||blocking)return;
            if(Input.GetKeyDown(KeyCode.Escape))TogglePause();
            if(!Session.IsPaused&&(Session.Phase==SessionPhase.Combat||Session.Phase==SessionPhase.TutorialDodge||Session.Phase==SessionPhase.TutorialAttack))
            {
                if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))CombatCommand(()=>Session.Dodge(DodgeDirection.Up));
                if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))CombatCommand(()=>Session.Dodge(DodgeDirection.Back));
                if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))CombatCommand(()=>Session.Dodge(DodgeDirection.Down));
                if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow))CombatCommand(()=>Session.Dodge(DodgeDirection.Forward));
                if(Input.GetKeyDown(KeyCode.Space))CombatCommand(()=>Session.Attack());
            }
            if(TickSession)Session.Tick(Time.unscaledDeltaTime);
            if(dirty)Render();
            if(View.BattlePanel.activeSelf)RenderBattle();
            feedbackTime-=Time.unscaledDeltaTime;if(feedbackTime<=0)View.Feedback.gameObject.SetActive(false);
        }
        void Render()
        {
            if(Session==null||!View||blocking)return;dirty=false;
            var phase=Session.Phase;
            bool inBattle=phase==SessionPhase.Combat||phase==SessionPhase.TutorialDodge||phase==SessionPhase.TutorialAttack;
            if(phase!=SessionPhase.Lobby&&phase!=SessionPhase.ExchangeConfirmation)relicScreen=false;
            View.Only(inBattle?View.BattlePanel:phase==SessionPhase.RewardChoice?View.RewardPanel:relicScreen?View.RelicPanel:View.LobbyPanel);
            WorldRoot.gameObject.SetActive(inBattle);Effects.gameObject.SetActive(inBattle);
            View.ModalPanel.SetActive(false);View.PausePanel.SetActive(Session.IsPaused&&inBattle);
            if(inBattle)RenderBattle();else if(phase==SessionPhase.RewardChoice)RenderRewards();else if(relicScreen)RenderRelics();else RenderLobby();
            if(phase==SessionPhase.ExchangeConfirmation)
            {
                var q=Session.PendingExchange;if(q!=null)
                {
                    View.SetModal("골드 교환",$"종 파편 {q.fragments}개를 소모해\n{q.gold}골드를 획득하시겠습니까?\n골드 {Session.Gold} → {Session.Gold+q.gold}\n종 파편 {Session.Fragments} → {Math.Max(0,Session.Fragments-q.fragments)}\n"+(Session.CanConfirmExchange?"강화는 교환 후 +를 다시 누르세요.":"종 파편이 부족해요."),"교환","취소",Session.CanConfirmExchange);
                    modalConfirm=()=>Command(()=>Session.ConfirmExchange());
                }
            }
            else if(phase==SessionPhase.MvpComplete)
            {
                View.SetModal("MVP 플레이 완료","1~10층과 11층 연결 전투를 마쳤습니다.\n다음 구역은 제작 중입니다.","로비로","",true);
                modalConfirm=()=>Command(()=>Session.ReturnToLobby());
            }
        }
        void RenderBattle()
        {
            var stage=Session.Catalog.GetStage(Session.Stage);bool boss=stage.kind!="normal";
            ApplyArt();
            View.Stage.text="Stage "+Session.Stage;
            View.PlayerHp.fillAmount=Session.PlayerMaxHp>0?Session.PlayerHp/Session.PlayerMaxHp:0;
            View.EnemyHp.fillAmount=View.BossHp.fillAmount=Session.EnemyMaxHp>0?Session.EnemyHp/Session.EnemyMaxHp:0;
            View.EnemyHpRoot.gameObject.SetActive(!boss);View.BossHpRoot.SetActive(boss);
            View.Stagger.fillAmount=Session.IsEnemyStunned?Session.StunSecondsRemaining/(Session.Catalog.GetStunDurationMs(stage.kind)/1000f):Session.Stagger/Session.Catalog.combat.staggerThreshold;
            Vector2 player=ScreenPoint(PlayerActor.transform.position-Effects.WorldOffset),enemy=ScreenPoint(EnemyActor.transform.position-Effects.WorldOffset);
            View.PlayerHpRoot.anchoredPosition=player+Vector2.down*12;View.EnemyHpRoot.anchoredPosition=enemy+Vector2.down*12;
            View.DangerRoot.anchoredPosition=player+Vector2.up*53;View.NextDangerRoot.anchoredPosition=player+Vector2.up*53;
            var intent=Session.EnemyIntent;View.Danger.gameObject.SetActive(intent.Visible);View.NextDanger.gameObject.SetActive(intent.Visible&&intent.HasNext);
            if(intent.Visible){View.Danger.Show(DangerRegion(intent.Direction),intent.Progress);if(intent.HasNext)View.NextDanger.Show(DangerRegion(intent.NextDirection),.1f);}
            bool dodgeTutorial=Session.Phase==SessionPhase.TutorialDodge,attackTutorial=Session.Phase==SessionPhase.TutorialAttack;
            View.Tutorial.gameObject.SetActive(dodgeTutorial||attackTutorial);View.Tutorial.text=dodgeTutorial?"▲를 눌러 피해!":"공격해!";
            for(int i=0;i<4;i++){View.Dodge[i].Interactable=!Session.IsPaused&&!attackTutorial&&(!dodgeTutorial||i==0);View.DodgeSymbols[i].color=dodgeTutorial&&i==0?amber:View.Dodge[i].Interactable?cream:muted;}
            View.Attack.Interactable=!Session.IsPaused&&!dodgeTutorial;View.AttackSymbol.color=attackTutorial?amber:View.Attack.Interactable?cream:muted;
            EnemyActor.SetStunned(Session.IsEnemyStunned);
            for(int i=0;i<View.StunStars.Length;i++){var star=View.StunStars[i];star.gameObject.SetActive(Session.IsEnemyStunned);star.rectTransform.anchoredPosition=enemy+new Vector2((i-1)*18,150+Mathf.Sin(Time.unscaledTime*8+i)*5);}
        }
        static int DangerRegion(DodgeDirection dodge){return dodge==DodgeDirection.Up?2:dodge==DodgeDirection.Down?0:dodge==DodgeDirection.Back?3:1;}
        Vector2 ScreenPoint(Vector3 world)
        {
            Camera canvasCamera=viewCanvas&&viewCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?viewCanvas.worldCamera:null;
            Vector2 point;RectTransformUtility.ScreenPointToLocalPointInRectangle(View.SafeRoot,WorldCamera.WorldToScreenPoint(world),canvasCamera,out point);return point;
        }
        void ApplyArt()
        {
            if(!Art)return;var c=Session.Catalog.GetCharacter(Session.CurrentCharacterId);PlayerActor.ApplyArt(Art.Find(c.artId),true);
            var stage=Session.Catalog.GetStage(Session.Stage);EnemyActor.ApplyArt(Art.Find(Session.Catalog.GetEnemy(stage.enemyId).artId),false);
            if(Background&&Art.gateBackground&&Background.sprite!=Art.gateBackground)
            {
                Background.sprite=Art.gateBackground;Background.color=Color.white;
                var size=Background.sprite.bounds.size;Background.transform.localScale=new Vector3(11.1f/Mathf.Max(.01f,size.x),6.2f/Mathf.Max(.01f,size.y),1);
            }
        }
        void RenderLobby()
        {
            var c=Session.Catalog.GetCharacter(Session.CurrentCharacterId);var stats=Session.PlayerStats;
            View.Wallet.text=$"골드 {Session.Gold:N0} · 종 파편 {Session.Fragments:N0}";View.StartLabel.text="Stage "+Session.CheckpointStartStage;
            View.CharacterName.text=c.displayName;View.Cycle.text="NG+ "+Session.Cycle;
            if(Art){var art=Art.Find(c.artId);if(art!=null&&art.idle.Length>0){View.LobbyPortrait.sprite=art.idle[0];View.LobbyPortrait.color=Color.white;}}
            for(int i=0;i<View.Characters.Length;i++)
            {
                bool exists=i<Session.Catalog.characters.Length;View.Characters[i].gameObject.SetActive(exists);if(!exists)continue;
                var ch=Session.Catalog.characters[i];View.CharacterLabels[i].text=ch.displayName;View.Characters[i].interactable=ch.availableInMvp;
            }
            string[] values={stats.attack.ToString("0.00"),(stats.maxHpMilli/1000f).ToString("0"),$"치명 {stats.critChance:P1} · 골드 +{stats.goldMultiplier-1:P1}",$"기절 적 ×{stats.stunDamageMultiplier:0.00}"};
            for(int i=0;i<4;i++)
            {
                int lv=Session.GetStatLevel(i),tier=lv==0?1:(lv-1)/10+1,filled=lv==0?0:(lv-1)%10+1;var row=View.Stats[i];
                row.Value.text=values[i]+(lv>=Session.Catalog.growth.maxLevel?" · 최대":$" · {Session.GetUpgradeCost(i)}G");row.Level.text=$"Lv.{lv} · {tier}단계";
                row.Upgrade.interactable=lv<Session.Catalog.growth.maxLevel;
                for(int s=0;s<5;s++)row.Stars[s].SetFilled(s<c.stars[i]);for(int b=0;b<10;b++)row.Bells[b].SetFilled(b<filled);
            }
        }
        bool IsEquipped(string id){foreach(var equipped in Session.EquippedRelicIds)if(equipped==id)return true;return false;}
        string Effect(RelicDefinition r,int level)
        {
            if(level<=0)return "미보유";float value=r.EffectAtLevel(level);
            if(r.effect=="normalStaggerPercent")return "일반 누적 "+(Session.Catalog.combat.normalStagger*(1+value)).ToString("0");
            if(r.effect=="perfectStaggerPercent")return "완벽 누적 "+(Session.Catalog.combat.perfectStagger*(1+value)).ToString("0");
            if(r.effect=="stunDamageBonus")return "기절 적 피해 +"+value.ToString("0.###");
            return r.effectLabel+" +"+(value*100).ToString("0.#")+(r.effect=="critChanceBonus"?"%p":"%");
        }
        void RenderRelics()
        {
            relicIds.Clear();foreach(var r in Session.Catalog.relics)if(Session.GetRelicLevel(r.id)>0)relicIds.Add(r.id);
            relicIds.Sort((a,b)=>{int owned=Session.GetRelicLevel(b).CompareTo(Session.GetRelicLevel(a));return owned!=0?owned:string.CompareOrdinal(a,b);});
            if(!relicIds.Contains(selectedRelic))selectedRelic=relicIds.Count>0?relicIds[0]:null;
            for(int i=0;i<View.RelicItems.Length;i++){bool exists=i<relicIds.Count;View.RelicItems[i].gameObject.SetActive(exists);if(exists){var r=Session.Catalog.GetRelic(relicIds[i]);View.RelicItemLabels[i].text=r.displayName+"\nLv."+Session.GetRelicLevel(r.id);}}
            if(View.RelicItems.Length>0)
            {
                var content=(RectTransform)View.RelicItems[0].transform.parent;int visible=Math.Min(relicIds.Count,View.RelicItems.Length);
                float height=visible>0?-((RectTransform)View.RelicItems[visible-1].transform).offsetMin.y:0;
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            }
            View.EquippedSlots.text=$"장착 {Session.EquippedRelicIds.Count} / {Session.UnlockedSlots}";
            if(View.EquippedSlotIcons!=null)for(int i=0;i<View.EquippedSlotIcons.Length;i++)View.EquippedSlotIcons[i].color=i<Session.EquippedRelicIds.Count?amber:i<Session.UnlockedSlots?cream:muted;
            int slot=Session.UnlockedSlots-1;bool max=slot>=Session.Catalog.economy.slotCosts.Length;
            View.SlotPurchaseLabel.text=max?"슬롯 모두 열림":$"슬롯 + · {Session.Catalog.economy.slotCosts[slot]}파편";View.SlotPurchase.interactable=!max;
            if(selectedRelic==null)
            {
                View.RelicName.text="유물이 없습니다";View.RelicLevel.text="";
                View.RelicEffect.text="5·10층에서 획득";View.RelicNext.text="";
                View.RelicEquipLabel.text="장착";View.RelicEquip.interactable=false;return;
            }
            var selected=Session.Catalog.GetRelic(selectedRelic);int level=Session.GetRelicLevel(selectedRelic);bool equipped=IsEquipped(selectedRelic);
            View.RelicName.text=selected.displayName;View.RelicLevel.text=$"Lv.{level} / {Session.Catalog.relicRules.maxLevel}"+(equipped?" · 장착 중":"");
            View.RelicEffect.text=Effect(selected,level);
            View.RelicNext.text=level>=Session.Catalog.relicRules.maxLevel?"최대 레벨":"다음: "+Effect(selected,level+1);
            View.RelicEquipLabel.text=equipped?"해제":"장착";View.RelicEquip.interactable=equipped||Session.EquippedRelicIds.Count<Session.UnlockedSlots;
        }
        void RenderRewards()
        {
            var choices=Session.RelicChoices;
            for(int i=0;i<View.Rewards.Length;i++)
            {
                var card=View.Rewards[i];bool exists=i<choices.Count;card.Receive.transform.parent.gameObject.SetActive(exists);rewardIds[i]=exists?choices[i]:null;if(!exists)continue;
                var relic=Session.Catalog.GetRelic(choices[i]);int lv=Session.GetRelicLevel(relic.id);bool max=lv>=Session.Catalog.relicRules.maxLevel;
                card.Title.text=relic.displayName;card.Level.text=max?$"MAX · 골드 +{Session.Catalog.GetMaxRelicGold(Session.Stage)}":lv==0?"신규 · Lv.1":$"Lv.{lv} → Lv.{lv+1}";
                card.Before.text=Effect(relic,lv);card.After.text=max?"최대 레벨 보상":"→ "+Effect(relic,lv+1);
                card.Equipment.text=max?"":IsEquipped(relic.id)?"장착 중 · 다음 적 적용":lv==0?"새 유물 · 로비에서 장착":"미장착";
            }
            View.RewardNote.text="받으면 다음 적과 전투합니다.";
        }
        void OnSessionEvent(SessionEvent e)
        {
            if(e.Kind==SessionEventKind.DodgeStarted)PlayerActor.PlayDodge((int)e.Direction);
            if(e.Kind==SessionEventKind.AttackStarted)PlayerActor.PlayAttack();
            if(e.Kind==SessionEventKind.PlayerDamaged||e.Kind==SessionEventKind.Dodged||e.Kind==SessionEventKind.PerfectDodge)EnemyActor.PlayAttack();
            if(e.Kind==SessionEventKind.EnemyDamaged||e.Kind==SessionEventKind.PlayerDamaged)
            {
                bool player=e.Kind==SessionEventKind.PlayerDamaged;var actor=player?PlayerActor:EnemyActor;actor.PlayHit();
                Vector2 point=ScreenPoint(actor.transform.position-Effects.WorldOffset),number=point+Vector2.up*(player?170:215);
                var bounds=View.SafeRoot.rect;float halfWidth=e.Critical?98:75;number.x=Mathf.Clamp(number.x,bounds.xMin+300+halfWidth,bounds.xMax-180-halfWidth);
                Effects.Hit((int)Math.Round(e.DamageMilli/1000d,MidpointRounding.AwayFromZero),e.Critical,number,point+Vector2.up*55);
            }
            if(e.Kind==SessionEventKind.StageStarted)Effects.Clear();dirty=true;
        }
        void Toast(string message){if(string.IsNullOrWhiteSpace(message))return;View.Feedback.text=message;View.Feedback.gameObject.SetActive(true);feedbackTime=3;}
        public string[] ValidateReferences()
        {
            var errors=new List<string>();if(!View)errors.Add("View");if(!WorldCamera)errors.Add("WorldCamera");if(!WorldRoot)errors.Add("WorldRoot");if(!PlayerActor)errors.Add("PlayerActor");if(!EnemyActor)errors.Add("EnemyActor");if(!Effects)errors.Add("Effects");
            if(View&&(View.Stats==null||View.Stats.Length!=4||View.Rewards==null||View.Rewards.Length!=3||View.Dodge==null||View.Dodge.Length!=4))errors.Add("View arrays");return errors.ToArray();
        }
    }
}
