using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    // Measured from the reference game viewport (1084 x 540, excluding Android navigation).
    public sealed class FeedbackLayout : MonoBehaviour
    {
        public MvpView View;
        Vector2 lastSize;
        bool lastUpgrade,lastCompanions;string lastResultTitle;
        void LateUpdate(){if(View&&(View.SafeRoot.rect.size!=lastSize||View.Reference&&lastUpgrade!=View.Reference.UpgradeScreen||View.Companions&&lastCompanions!=View.Companions.IsOpen||lastResultTitle!=View.ResultTitle.text))Apply(View.SafeRoot.rect.width,View.SafeRoot.rect.height,false);}
        public void Apply(float width,float height,bool compact)
        {
            if(!View||width<=0||height<=0)return;lastSize=new Vector2(width,height);lastUpgrade=View.Reference&&View.Reference.UpgradeScreen;lastCompanions=View.Companions&&View.Companions.IsOpen;lastResultTitle=View.ResultTitle.text;
            var v=View;float f=Mathf.Min(width/960,height/540),w=width/f,h=height/f,x=(w-960)/2,y=(h-540)/2;
            if(v.AudioCredits)v.AudioCredits.Layout(width,height,false);
            // Reference viewport x=44..1128, y=0..540. Normalize within the safe area.
            float[,] centers={{122f/1084,380f/540},{60f/1084,460f/540},{161f/1084,481f/540}};
            for(int i=0;i<3;i++){float halfHeight=v.Dragon?39:42;Box(v.Dodge[i].transform,w*centers[i,0]-39,h*centers[i,1]-halfHeight,78,halfHeight*2,f);Center(v.DodgeSymbols[i].rectTransform,42,42,f);}
            v.Dodge[3].gameObject.SetActive(false);
            Box(v.Attack.transform,w*(983f/1084)-56,h*(440f/540)-60,112,120,f);Center(v.AttackSymbol.rectTransform,54,62,f);
            R(v.Pause.transform,974,8,102,54,width,height);var pauseLabel=v.Pause.GetComponentInChildren<Text>();pauseLabel.text="일시정지";R(pauseLabel.transform,0,0,102,54,width,height);
            Box(v.Stage.transform,20,16,210,32,f);
            R(v.BattleGold.transform,16,78,440,38,width,height);v.BattleGold.alignment=TextAnchor.MiddleLeft;
            v.HitCombo.gameObject.SetActive(false);
            if(v.GoldPickup&&v.GoldPickup.Reward)R(v.GoldPickup.Reward.transform,382,160,320,44,width,height);
            Box(v.Tutorial.transform,w*.5f-250,166,500,44,f);
            R(v.Stagger.transform.parent,v.Dragon?340:374,124,v.Dragon?408:340,v.Dragon?7.2f:6,width,height);
            if(v.StunReady)R(v.StunReady.transform,v.Dragon?340:374,137,v.Dragon?408:340,28,width,height);
            if(v.StaggerIcon)v.StaggerIcon.SetActive(false);
            Box(v.PlayerHpRoot,x+312,y+390,126,16,f);Box(v.EnemyHpRoot,x+603,y+390,144,16,f);
            R(v.EnemyName.transform,640,428,244,44,width,height);v.EnemyName.alignment=TextAnchor.UpperCenter;
            v.BossHpRoot.SetActive(false);
            var wave=v.DodgeResult.rectTransform;wave.anchorMin=Vector2.zero;wave.anchorMax=Vector2.one;wave.offsetMin=wave.offsetMax=Vector2.zero;
            Center(v.DodgeResult.Label.rectTransform,240,44,f);
            if(v.Reference)
            {
                var r=v.Reference;
                R(r.Heading.transform,304,17,476,54,width,height);r.Heading.alignment=TextAnchor.MiddleCenter;
                R(v.Wallet.transform,930,12,138,38,width,height);v.Wallet.alignment=TextAnchor.MiddleRight;
                R(r.Back.transform,4,12,104,52,width,height);
                R(v.ReplayPrologue.transform,4,12,104,52,width,height);
                R(v.Characters[0].transform.parent,16,100,272,340,width,height);
                var title=v.Characters[0].transform.parent.Find("RosterTitle");if(title)title.gameObject.SetActive(false);
                for(int i=0;i<3;i++)
                {
                    R(v.Characters[i].transform,16,18+i*60,240,56,width,height);
                    R(v.CharacterLabels[i].transform,16,0,218,48,width,height);v.CharacterLabels[i].alignment=TextAnchor.MiddleLeft;
                }
                R(v.LobbyPortrait.transform.parent,304,100,764,340,width,height);
                R(v.LobbyPortrait.transform,520,124,140,150,width,height);v.CharacterName.gameObject.SetActive(false);v.Cycle.gameObject.SetActive(false);
                R(r.Summary.transform,338,135,680,92,width,height);r.Summary.alignment=TextAnchor.UpperLeft;r.Summary.lineSpacing=29f/24;
                R(r.SelectionStats.transform,348,236,640,196,width,height);r.SelectionStats.alignment=TextAnchor.UpperLeft;r.SelectionStats.lineSpacing=29f/24;
                v.Stats[0].Upgrade.transform.parent.parent.gameObject.SetActive(false);
                R(v.OpenRelics.transform,r.UpgradeScreen?16:472,r.UpgradeScreen?452:466,r.UpgradeScreen?236:140,r.UpgradeScreen?76:54,width,height);
                v.OpenRelics.GetComponentInChildren<Text>().text="유물 보기";
                R(r.RelicCount.transform,472,516,140,24,width,height);
                R(v.StartRun.transform,r.UpgradeScreen?832:930,452,r.UpgradeScreen?236:138,76,width,height);
                R(v.StartLabel.transform,12,r.UpgradeScreen?8:14,r.UpgradeScreen?212:114,42,width,height);
                R(r.StartStage.transform,12,48,212,22,width,height);
                var startFrame=v.StartRun.GetComponentInChildren<ReferencePanelGraphic>();if(startFrame){startFrame.Underline=!r.UpgradeScreen;startFrame.SetVerticesDirty();}
                R(r.StatButtons[0].transform.parent,16,100,272,340,width,height);
                for(int i=0;i<r.StatButtons.Length;i++)
                {
                    R(r.StatButtons[i].transform,6,32+i*68,260,56,width,height);
                    R(r.StatLabels[i].transform,12,0,150,48,width,height);r.StatLabels[i].alignment=TextAnchor.MiddleLeft;
                    R(r.StatRatings[i].transform,162,0,90,48,width,height);r.StatRatings[i].alignment=TextAnchor.MiddleLeft;
                }
                R(r.UpgradeTitle.transform.parent,304,100,764,340,width,height);
                R(r.UpgradeTitle.transform,34,64,530,32,width,height);r.UpgradeLevel.gameObject.SetActive(false);r.UpgradePortrait.gameObject.SetActive(false);
                R(r.After.transform,44,94,680,34,width,height);
                R(r.Before.transform,44,150,680,56,width,height);
                R(r.Price.transform,44,210,456,38,width,height);
                R(r.UpgradeWallet.transform,478,213,162,72,width,height);r.UpgradeWallet.alignment=TextAnchor.UpperRight;r.UpgradeWallet.lineSpacing=1.25f;
                R(r.Note.transform,44,270,640,38,width,height);
                R(r.OneLevel.transform,598,16,68,72,width,height);R(r.Maximum.transform,680,16,68,72,width,height);
                R(r.Purchase.transform,472,466,140,54,width,height);R(r.Exchange.transform,16,452,236,76,width,height);
                if(v.AudioCredits)R(v.AudioCredits.OpenButton.transform,16,452,236,76,width,height);
                // Inventory uses the same two-panel grid as character selection.
                R(r.RelicHeading.transform,304,17,476,54,width,height);R(v.RelicBack.transform,4,12,104,52,width,height);
                var inventory=v.EquippedSlots.transform.parent;R(inventory,16,100,272,340,width,height);
                v.EquippedSlots.gameObject.SetActive(true);R(v.EquippedSlots.transform,16,294,240,36,width,height);foreach(var slot in v.EquippedSlotIcons)slot.gameObject.SetActive(false);
                var scroll=inventory.Find("InventoryScroll");R(scroll,12,16,248,272,width,height);
                var content=v.RelicItems[0].transform.parent;
                for(int i=0;i<v.RelicItems.Length;i++)
                {
                    R(v.RelicItems[i].transform,0,i*60,248,56,width,height);
                    R(v.RelicItemLabels[i].transform,14,0,220,48,width,height);v.RelicItemLabels[i].alignment=TextAnchor.MiddleLeft;
                }
                R(v.RelicName.transform.parent,304,100,764,340,width,height);v.RelicName.gameObject.SetActive(false);
                R(v.RelicEffect.transform,34,24,690,56,width,height);R(v.RelicLevel.transform,34,90,690,40,width,height);
                R(v.RelicNext.transform,34,148,690,72,width,height);R(r.CollectionIcon.transform,574,142,80,80,width,height);
                R(v.SlotPurchase.transform,304,466,764,60,width,height);v.RelicEquip.gameObject.SetActive(false);

            }
            if(v.Prologue)
            {
                var p=v.Prologue;Box(p.Scroll,x+184,y+38,592,390,f);Box(p.Lantern,x+58,y-4,200,200,f);
                Box(p.Title.transform,x+220,y+58,520,36,f);Box(p.ReadingWindow,x+228,y+110,504,286,f);
                foreach(var t in p.Paragraphs)t.rectTransform.sizeDelta=new Vector2(504,120)*f;
                Box(p.Skip.transform,x+730,y+460,198,56,f);p.Pause.gameObject.SetActive(false);p.Next.gameObject.SetActive(false);p.ReadingMode.gameObject.SetActive(false);
            }
            if(v.RewardDialog)
            {
                R(v.RewardDialog,206,54,672,390,width,height);R(v.RewardNote.transform,20,14,632,52,width,height);
                for(int i=0;i<3;i++)
                {
                    var c=v.Rewards[i];float cardTop=v.Dragon?80:92,cardHeight=v.Dragon?144:112;
                    R(c.Receive.transform.parent,102+i*176,cardTop,116,cardHeight,width,height);R(c.Receive.transform,0,0,116,cardHeight,width,height);
                    R(c.Selection.transform,16,0,84,84,width,height);R(c.Icon.transform,20,4,76,76,width,height);
                    // Keep the existing font; long names wrap inside a padded title area.
                    R(c.Title.transform,v.Dragon?10:-28,84,v.Dragon?96:172,v.Dragon?48:28,width,height);
                }
                R(v.RewardDescription.transform,24,v.Dragon?232:208,624,v.Dragon?80:104,width,height);R(v.RewardConfirm.transform,32,320,340,58,width,height);
                if(v.RewardGold)R(v.RewardGold.transform,392,320,248,58,width,height);
            }
            if(v.ResultPanel)
            {
                R(v.ResultTitle.transform,160,142,764,86,width,height);R(v.ResultScope.transform,160,228,764,32,width,height);
                R(v.ResultGold.transform,160,264,764,32,width,height);R(v.ResultFragments.transform,160,300,764,32,width,height);
                R(v.ResultRetryHint.transform,160,402,764,32,width,height);
                R(v.ResultRetry.transform,752,466,148,60,width,height);R(v.ResultLobby.transform,926,466,142,60,width,height);
                if(v.Reference)R(v.Reference.SettlementContinue.transform,930,466,138,60,width,height);
            }

            // Match rendered ink: the Neo font needs 44/24 to produce reference 35/20px glyphs.
            float typeScale=Mathf.Min(width/1084,height/540);
            foreach(var t in v.GetComponentsInChildren<Text>(true))
            {
                if(t.GetComponentInParent<CompanionInnView>(true))continue;
                var story=t.GetComponentInParent<PresentationOverlay>(true);if(story&&story.StoryV5)continue;
                t.fontSize=Mathf.RoundToInt(TypeSize(t)*typeScale);t.resizeTextForBestFit=false;t.fontStyle=FontStyle.Normal;
                if(t==v.Reference.Heading||t==v.Reference.RelicHeading||t==v.RewardNote||t==v.ResultTitle)
                {t.rectTransform.localScale=new Vector3(.75f,1,1);var size=t.rectTransform.sizeDelta;size.x/=.75f;t.rectTransform.sizeDelta=size;}
            }
            if(v.Dragon)v.Dragon.Layout(width,height);
            ApplyInnLayout(width,height);
        }
        // The migration can apply only the saved lobby geometry without touching
        // battle HUD, HP, dialogue, or other presentation owners.
        public void ApplyInnLayout(float width,float height)
        {
            if(!View||width<=0||height<=0)return;
            LayoutInn(width,height);if(View.Companions)View.Companions.Layout(width,height);
        }
        void LayoutInn(float width,float height)
        {
            var v=View;var r=v.Reference;if(!r)return;
            if(r.RestoreUpgradeV6){LayoutReferenceV6(width,height);return;}
            if(!r.GrowthTab||!r.UpgradeScreen)return;
            float sx=width/1280,sy=height/720;
            InnBox(r.Heading.transform,208,16,440,72,sx,sy);r.Heading.alignment=TextAnchor.MiddleLeft;
            InnBox(r.Back.transform,24,8,160,96,sx,sy);InnBox(r.InnWallet.transform,672,8,584,96,sx,sy);
            InnBox(r.GrowthTab.transform,24,108,200,96,sx,sy);InnBox(r.CompanionsTab.transform,240,108,200,96,sx,sy);InnBox(v.OpenRelics.transform,456,108,200,96,sx,sy);
            InnBox(r.StatButtons[0].transform.parent,24,208,376,488,sx,sy);
            for(int i=0;i<r.StatButtons.Length;i++)
            {
                InnBox(r.StatButtons[i].transform,16,4+i*96,232,96,sx,sy);InnBox(r.StatLabels[i].transform,12,6,208,36,sx,sy);
                InnBox(r.StatRatings[i].transform,12,40,208,24,sx,sy);InnFont(r.StatLabels[i],28,sy);InnFont(r.StatRatings[i],20,sy);
                if(r.GrowthRows==null||i>=r.GrowthRows.Length)continue;
                var row=r.GrowthRows[i];InnBox(row.Plus.transform,264,4+i*96,96,96,sx,sy);InnFont(row.Plus.GetComponentInChildren<Text>(),40,sy);
                for(int j=0;j<row.Pips.Length;j++)InnBox(row.Pips[j].transform,12+j*21,74,17,12,sx,sy);
            }
            InnBox(r.UpgradeTitle.transform.parent,424,208,832,488,sx,sy);
            InnBox(r.UpgradeTitle.transform,32,24,520,56,sx,sy);InnBox(r.After.transform,32,96,768,44,sx,sy);
            InnBox(r.Before.transform,32,152,768,64,sx,sy);InnBox(r.Price.transform,32,228,768,44,sx,sy);InnBox(r.Note.transform,32,280,768,44,sx,sy);
            InnBox(r.OneLevel.transform,568,20,96,96,sx,sy);InnBox(r.Maximum.transform,680,20,120,96,sx,sy);
            InnBox(r.Purchase.transform,456,572,264,96,sx,sy);InnBox(r.Exchange.transform,752,572,200,96,sx,sy);
            InnBox(v.StartRun.transform,984,572,240,96,sx,sy);var start=v.StartLabel.rectTransform;start.anchorMin=Vector2.zero;start.anchorMax=Vector2.one;start.offsetMin=start.offsetMax=Vector2.zero;
            r.UpgradeWallet.gameObject.SetActive(false);r.StartStage.gameObject.SetActive(false);
            InnFont(r.Heading,40,sy);InnFont(r.InnWallet,28,sy);InnFont(r.UpgradeTitle,36,sy);InnFont(r.After,28,sy);InnFont(r.Before,36,sy);InnFont(r.Price,28,sy);InnFont(r.Note,26,sy);InnFont(v.StartLabel,30,sy);
            foreach(var button in new[]{r.GrowthTab,r.CompanionsTab,v.OpenRelics,r.Back,r.Purchase,r.Exchange,r.OneLevel,r.Maximum})InnFont(button.GetComponentInChildren<Text>(),28,sy);
            v.OpenRelics.GetComponentInChildren<Text>().text="유물";
        }
        void LayoutReferenceV6(float width,float height)
        {
            var v=View;var r=v.Reference;float sx=width/1280f,sy=height/720f;
            bool companions=v.Companions&&v.Companions.IsOpen;
            InnBox(r.Heading.transform,360,16,companions?280:440,72,sx,sy);r.Heading.alignment=TextAnchor.MiddleCenter;InnFont(r.Heading,44,sy);
            InnBox(r.Back.transform,20,8,144,96,sx,sy);InnBox(v.ReplayPrologue.transform,20,8,144,96,sx,sy);
            InnBox(r.CompanionsTab.transform,1096,8,160,96,sx,sy);InnBox(r.InnWallet.transform,672,8,400,96,sx,sy);InnFont(r.InnWallet,24,sy);
            InnBox(v.Wallet.transform,864,16,208,72,sx,sy);InnFont(v.Wallet,28,sy);
            if(v.Dragon)InnBox(v.Dragon.WalletGem.transform,820,30,36,36,sx,sy);
            InnBox(v.StartRun.transform,984,608,272,96,sx,sy);FillInnLabel(v.StartLabel,30,sy);
            InnBox(v.OpenRelics.transform,r.UpgradeScreen?20:560,608,r.UpgradeScreen?280:176,96,sx,sy);
            // Keep the ownership count inside the lower part of the selection
            // screen's relic control; at 640px it still has a 16px text line.
            InnBox(r.RelicCount.transform,560,672,176,32,sx,sy);InnFont(r.RelicCount,16,sy);
            if(v.AudioCredits){InnBox(v.AudioCredits.OpenButton.transform,20,608,280,96,sx,sy);FillInnLabel(v.AudioCredits.OpenButton.GetComponentInChildren<Text>(true),28,sy);}
            foreach(var button in new[]{r.Back,v.ReplayPrologue,r.CompanionsTab})FillInnLabel(button.GetComponentInChildren<Text>(true),28,sy);
            InnFont(v.OpenRelics.GetComponentInChildren<Text>(true),28,sy);
            // Five rows keep the original horizontal name/rating list. V5 pips
            // and instant + controls stay serialized but do not occupy this UI.
            InnBox(r.StatButtons[0].transform.parent,20,112,320,480,sx,sy);
            for(int i=0;i<r.StatButtons.Length;i++)
            {
                InnBox(r.StatButtons[i].transform,8,96*i,304,96,sx,sy);
                InnBox(r.StatLabels[i].transform,12,20,152,56,sx,sy);InnFont(r.StatLabels[i],30,sy);
                InnBox(r.StatRatings[i].transform,172,20,120,56,sx,sy);InnFont(r.StatRatings[i],18,sy);
                r.StatRatings[i].fontSize=Mathf.Max(9,Mathf.RoundToInt(18*sy));
            }
            InnBox(r.UpgradeTitle.transform.parent,360,112,900,480,sx,sy);
            InnBox(r.UpgradeTitle.transform,40,72,600,48,sx,sy);InnBox(r.After.transform,52,128,780,44,sx,sy);
            InnBox(r.Before.transform,52,200,780,64,sx,sy);InnBox(r.Price.transform,52,280,450,48,sx,sy);
            InnBox(r.UpgradeWallet.transform,650,280,200,88,sx,sy);InnBox(r.Note.transform,52,366,780,48,sx,sy);
            InnBox(r.OneLevel.transform,676,16,96,96,sx,sy);InnBox(r.Maximum.transform,788,16,96,96,sx,sy);
            InnBox(r.Purchase.transform,560,608,176,96,sx,sy);InnBox(r.Exchange.transform,20,608,280,96,sx,sy);
            InnFont(r.UpgradeTitle,32,sy);InnFont(r.After,28,sy);InnFont(r.Before,30,sy);InnFont(r.Price,28,sy);InnFont(r.UpgradeWallet,28,sy);InnFont(r.Note,28,sy);
            foreach(var button in new[]{r.OneLevel,r.Maximum,r.Purchase,r.Exchange})FillInnLabel(button.GetComponentInChildren<Text>(true),28,sy);
        }
        static void FillInnLabel(Text text,int size,float scale)
        {
            text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;text.alignment=TextAnchor.MiddleCenter;InnFont(text,size,scale);
        }
        static void InnBox(Transform t,float x,float y,float w,float h,float sx,float sy)=>Box(t,x*sx,y*sy,w*sx,h*sy);
        static void InnFont(Text t,int size,float scale){t.fontSize=Mathf.Max(12,Mathf.RoundToInt(size*scale));t.transform.localScale=Vector3.one;t.resizeTextForBestFit=false;}
        public int TypeSize(Text t)
        {
            var v=View;var r=v.Reference;
            if(r&&(t==r.Heading||t==r.RelicHeading)||t==v.RewardNote)return 44;
            if(t==v.ResultTitle)return t.text.StartsWith("스테이지")?44:84;
            if(v.GoldPickup&&t==v.GoldPickup.Reward)return 32;
            if(t==v.StunReady)return 24;
            if(t==v.StartLabel)return r&&r.UpgradeScreen?30:24;
            if(r&&(t==r.RelicCount||t==r.StartStage))return 18;
            if(r&&r.StatRatings!=null&&System.Array.IndexOf(r.StatRatings,t)>=0)return 16;
            if(t.transform.IsChildOf(v.ReplayPrologue.transform)||r&&t.transform.IsChildOf(r.Back.transform)||t.transform.IsChildOf(v.RelicBack.transform))return 20;
            return 24;
        }
        static void R(Transform t,float x,float y,float w,float h,float width,float height)
        {Box(t,x*width/1084,y*height/540,w*width/1084,h*height/540);}
        public static void Box(Transform transform,float x,float y,float width,float height,float factor=1)
        {
            var r=(RectTransform)transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y)*factor;r.sizeDelta=new Vector2(width,height)*factor;
        }
        static void Center(RectTransform r,float width,float height,float f)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(width,height)*f;}
    }
}
