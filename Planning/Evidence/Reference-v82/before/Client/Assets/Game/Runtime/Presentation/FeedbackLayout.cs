using UnityEngine;
using UnityEngine.UI;
namespace LastBell
{
    // One owner for all responsive rectangles and the common 24px type scale.
    public sealed class FeedbackLayout : MonoBehaviour
    {
        public MvpView View;
        Vector2 lastSize;
        void LateUpdate(){if(View&&View.SafeRoot.rect.size!=lastSize)Apply(View.SafeRoot.rect.width,View.SafeRoot.rect.height,false);}
        public void Apply(float width,float height,bool compact)
        {
            if(!View||width<=0||height<=0)return;lastSize=new Vector2(width,height);
            var v=View;float f=Mathf.Min(width/960,height/540),w=width/f,h=height/f,x=(w-960)/2,y=(h-540)/2;
            if(v.AudioCredits)v.AudioCredits.Layout(width,height,false);
            // Reference viewport x=44..1128, y=0..540. Normalize within the safe area.
            float[,] centers={{122f/1084,380f/540},{60f/1084,460f/540},{161f/1084,481f/540}};
            for(int i=0;i<3;i++){Box(v.Dodge[i].transform,w*centers[i,0]-39,h*centers[i,1]-42,78,84,f);Center(v.DodgeSymbols[i].rectTransform,42,42,f);}
            v.Dodge[3].gameObject.SetActive(false);
            Box(v.Attack.transform,w*(983f/1084)-56,h*(440f/540)-60,112,120,f);Center(v.AttackSymbol.rectTransform,54,62,f);
            Box(v.Pause.transform,w-144,12,124,48,f);v.Pause.GetComponentInChildren<Text>().text="일시정지";
            Box(v.Stage.transform,20,16,210,32,f);
            Box(v.BattleGold.transform,20,52,270,32,f);v.BattleGold.alignment=TextAnchor.MiddleLeft;
            Box(v.HitCombo.transform,w-240,66,220,32,f);
            Box(v.Tutorial.transform,w*.5f-250,166,500,44,f);
            Box(v.Stagger.transform.parent,w*.5f-180,78,360,10,f);
            if(v.StaggerIcon)Box(v.StaggerIcon.transform,w*.5f-216,68,28,28,f);
            Box(v.PlayerHpRoot,x+300,y+394,150,12,f);Box(v.EnemyHpRoot,x+600,y+394,150,12,f);
            Box(v.EnemyName.transform,x+534,y+416,282,64,f);v.EnemyName.alignment=TextAnchor.UpperCenter;
            v.BossHpRoot.SetActive(false);
            var wave=v.DodgeResult.rectTransform;wave.anchorMin=Vector2.zero;wave.anchorMax=Vector2.one;wave.offsetMin=wave.offsetMax=Vector2.zero;
            Center(v.DodgeResult.Label.rectTransform,240,44,f);
            if(v.Reference)
            {
                var r=v.Reference;
                Box(r.PlayerHealth.transform,x+255,y+358,240,32,f);Box(r.EnemyHealth.transform,x+555,y+358,240,32,f);
                Box(r.PlayerName.transform,x+300,y+416,150,32,f);Box(r.BattleFragments.transform,20,86,240,32,f);
                Box(r.Heading.transform,x+32,y+24,250,40,f);Box(v.Wallet.transform,x+290,y+24,638,40,f);
                Box(r.Back.transform,x+32,y+458,160,58,f);
                Box(v.Characters[0].transform.parent,x+24,y+92,196,338,f);
                var title=v.Characters[0].transform.parent.Find("RosterTitle");if(title)title.gameObject.SetActive(false);
                for(int i=0;i<3;i++)Box(v.Characters[i].transform,12,12+i*104,172,92,f);
                Box(v.LobbyPortrait.transform.parent,x+236,y+92,220,338,f);
                Box(v.LobbyPortrait.transform,12,16,196,220,f);Box(v.CharacterName.transform,12,244,196,40,f);v.Cycle.gameObject.SetActive(false);
                Box(r.Summary.transform,x+480,y+94,448,74,f);
                var stats=v.Stats[0].Upgrade.transform.parent.parent;Box(stats,x+480,y+180,448,250,f);
                for(int i=0;i<4;i++)
                {
                    var row=v.Stats[i];Box(row.Title.transform.parent,0,i*62,448,58,f);
                    Box(row.Title.transform,12,4,100,30,f);Box(row.Value.transform,118,27,318,28,f);
                    for(int j=0;j<5;j++)Box(row.Stars[j].transform,118+j*24,7,19,19,f);
                }
                Box(v.OpenRelics.transform,x+236,y+458,200,58,f);v.OpenRelics.GetComponentInChildren<Text>().text="유물 보기";
                Box(v.StartRun.transform,x+692,y+458,236,58,f);
                if(v.ReplayPrologue){Box(v.ReplayPrologue.transform,x+24,y+458,196,58,f);v.ReplayPrologue.gameObject.SetActive(!r.UpgradeScreen);}
                for(int i=0;i<4;i++)Box(r.StatButtons[i].transform,x+24,y+92+i*84,208,76,f);
                Box(r.UpgradeTitle.transform.parent,x+248,y+92,680,338,f);
                Box(r.UpgradeTitle.transform,24,16,190,32,f);Box(r.UpgradeLevel.transform,24,56,190,32,f);
                Box(r.UpgradePortrait.transform,18,104,132,150,f);
                Box(r.OneLevel.transform,418,16,110,48,f);Box(r.Maximum.transform,540,16,116,48,f);
                Box(r.Before.transform,172,106,474,46,f);Box(r.After.transform,172,158,474,46,f);
                Box(r.Price.transform,24,244,632,36,f);Box(r.Note.transform,24,286,632,36,f);
                Box(r.Purchase.transform,x+452,y+458,224,58,f);Box(r.Exchange.transform,x+236,y+458,200,58,f);
                if(v.AudioCredits)Box(v.AudioCredits.OpenButton.transform,x+764,y+64,164,28,f);
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
                Box(v.RewardDialog,w*.5f-314,h*.5f-194,628,388,f);Box(v.RewardNote.transform,16,16,596,36,f);
                for(int i=0;i<3;i++)
                {
                    var c=v.Rewards[i];Box(c.Receive.transform.parent,20+i*198,64,192,132,f);Box(c.Receive.transform,0,0,192,132,f);
                    Box(c.Selection.transform,0,0,192,132,f);Box(c.Icon.transform,60,8,72,72,f);Box(c.Title.transform,4,90,184,36,f);
                }
                Box(v.RewardDescription.transform,24,208,580,98,f);Box(v.RewardConfirm.transform,214,322,200,48,f);
            }
            if(v.ResultPanel)
            {
                Box(v.ResultTitle.transform,w*.5f-360,h*.5f-182,720,48,f);Box(v.ResultScope.transform,w*.5f-350,h*.5f-126,700,36,f);
                Box(v.ResultGold.transform,w*.5f-260,h*.5f-64,520,36,f);Box(v.ResultFragments.transform,w*.5f-260,h*.5f-18,520,36,f);
                Box(v.ResultRetryHint.transform,w*.5f-300,h*.5f+40,600,36,f);
                Box(v.ResultRetry.transform,w*.5f-240,h*.5f+112,228,60,f);Box(v.ResultLobby.transform,w*.5f+12,h*.5f+112,228,60,f);
                if(v.Reference)Box(v.Reference.SettlementContinue.transform,w*.5f-140,h*.5f+112,280,60,f);
            }
            // Long copy wraps or scrolls; labels never silently shrink to another size.
            foreach(var t in v.GetComponentsInChildren<Text>(true)){t.fontSize=Mathf.RoundToInt(24*f);t.resizeTextForBestFit=false;t.fontStyle=FontStyle.Normal;}
        }
        public static void Box(Transform transform,float x,float y,float width,float height,float factor=1)
        {
            var r=(RectTransform)transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y)*factor;r.sizeDelta=new Vector2(width,height)*factor;
        }
        static void Center(RectTransform r,float width,float height,float f)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(width,height)*f;}
    }
}
