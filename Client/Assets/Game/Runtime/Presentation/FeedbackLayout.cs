using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Sole owner of this revision's responsive rectangles. No hierarchy is built here.
    public sealed class FeedbackLayout : MonoBehaviour
    {
        public MvpView View;
        Vector2 lastSize;bool lastCompact;
        void LateUpdate()
        {
            if(!View)return;var size=View.SafeRoot.rect.size;
            bool compact=Screen.safeArea.height<=440||Screen.safeArea.width<=760;
            if(size==lastSize&&compact==lastCompact)return;
            Apply(size.x,size.y,compact);
        }
        public void Apply(float width,float height,bool compact)
        {
            lastSize=new Vector2(width,height);lastCompact=compact;
            float f=height/(compact?360:540),w=width/f;
            if(f<=0)return;
            var v=View;float extra=w-(compact?640:960);
            // Bottom-left direction cluster; only the transparent square receives input.
            float[,] buttons=compact?new float[,]{{68,176,56,56},{8,236,56,56},{68,296,56,56},{128,236,56,56}}:
                new float[,]{{88,312,64,64},{16,384,64,64},{88,456,64,64},{160,384,64,64}};
            for(int i=0;i<4;i++){Box(v.Dodge[i].transform,buttons[i,0],buttons[i,1],buttons[i,2],buttons[i,3],f);Center(v.DodgeSymbols[i].rectTransform,compact?32:38,compact?32:38,f);}
            Box(v.Attack.transform,(compact?548:836)+extra,compact?260:410,compact?80:104,compact?80:104,f);
            Center(v.AttackSymbol.rectTransform,compact?38:48,compact?38:48,f);
            Box(v.Pause.transform,(compact?580:896)+extra,compact?8:12,compact?48:52,compact?48:52,f);
            Box(v.Stage.transform,compact?12:20,compact?9:16,130,32,f);v.Stage.fontSize=Mathf.RoundToInt((compact?18:24)*f);
            // Full viewport parent, independent of safe-area margins and actor position.
            var wave=v.DodgeResult.rectTransform;wave.anchorMin=Vector2.zero;wave.anchorMax=Vector2.one;wave.pivot=new Vector2(.5f,.5f);wave.offsetMin=wave.offsetMax=Vector2.zero;
            var label=v.DodgeResult.Label.rectTransform;label.anchorMin=label.anchorMax=label.pivot=new Vector2(.5f,.5f);label.anchoredPosition=Vector2.zero;label.sizeDelta=new Vector2(220,44);
            v.DodgeResult.Label.fontSize=26;
            Box(v.Tutorial.transform,w*.5f-150,compact?114:180,300,38,f);v.Tutorial.fontSize=Mathf.RoundToInt((compact?18:24)*f);
            Box(v.Stagger.transform.parent,w*.5f-(compact?135:235),compact?59:83,compact?270:470,compact?12:18,f);
            if(v.StaggerIcon)Box(v.StaggerIcon.transform,w*.5f-(compact?162:275),compact?51:76,28,28,f);
            var roster=v.Characters[0].transform.parent;
            Box(roster,compact?8:16,12,compact?176:196,compact?336:502,f);
            var title=roster.Find("RosterTitle");if(title)Box(title,12,8,150,30,f);
            for(int i=0;i<v.Characters.Length;i++){Box(v.Characters[i].transform,12,compact?42+i*54:56+i*86,compact?152:172,compact?48:72,f);Font(v.CharacterLabels[i],compact?18:24,f);}
            var portrait=v.LobbyPortrait.transform.parent;
            Box(portrait,compact?16:228,compact?224:76,compact?160:168,compact?124:328,f);
            Box(v.LobbyPortrait.transform,compact?0:10,compact?5:40,compact?80:148,compact?108:210,f);
            Box(v.CharacterName.transform,compact?78:0,compact?32:255,compact?80:168,32,f);Font(v.CharacterName,compact?20:26,f);
            Box(v.Cycle.transform,compact?78:0,compact?66:0,compact?80:168,28,f);Font(v.Cycle,compact?14:18,f);
            Box(v.Wallet.transform,compact?196:420,compact?18:20,w-(compact?204:436),32,f);Font(v.Wallet,compact?16:21,f);
            var stats=v.Stats[0].Upgrade.transform.parent.parent;
            Box(stats,compact?196:420,compact?68:76,w-(compact?204:436),compact?220:318,f);
            float rw=w-(compact?204:436),rh=compact?52:72;
            for(int i=0;i<4;i++)
            {
                var row=v.Stats[i];Box(row.Upgrade.transform.parent,0,i*(compact?56:82),rw,rh,f);
                Box(row.Title.transform,8,3,56,24,f);Font(row.Title,compact?16:20,f);
                Box(row.Level.transform,compact?124:176,3,compact?100:128,22,f);Font(row.Level,compact?12:16,f);
                Box(row.Value.transform,8,compact?23:28,rw-210,compact?17:22,f);Font(row.Value,compact?12:16,f);
                Box(row.Price.transform,rw-200,compact?20:28,126,24,f);Font(row.Price,compact?14:18,f);
                Box(row.Upgrade.transform,rw-68,(rh-(compact?48:56))/2,64,compact?48:56,f);
                foreach(var t in row.Upgrade.GetComponentsInChildren<Text>())Font(t,26,f);
                for(int k=0;k<5;k++)Box(row.Stars[k].transform,compact?66+k*11:70+k*17,compact?8:7,compact?10:14,compact?10:14,f);
                for(int k=0;k<10;k++)Box(row.Bells[k].transform,8+k*(compact?14:20),compact?42:53,compact?7:12,compact?7:12,f);
            }
            Box(v.OpenRelics.transform,compact?196:420,compact?304:446,(compact?188:216)+extra*.45f,compact?48:68,f);
            Box(v.StartRun.transform,(compact?400:656)+extra*.45f,compact?304:446,(compact?232:288)+extra*.55f,compact?48:68,f);
            Font(v.StartLabel,compact?22:28,f);Font(v.OpenRelics.GetComponentInChildren<Text>(),compact?20:26,f);
            if(v.ResultPanel)
            {
                float x=w*.5f-220,y=compact?26:88;
                Box(v.ResultTitle.transform,x,y,440,48,f);Font(v.ResultTitle,compact?25:32,f);
                Box(v.ResultScope.transform,x,y+55,440,28,f);Font(v.ResultScope,compact?17:20,f);
                Box(v.ResultGold.transform,x+12,y+99,416,34,f);Font(v.ResultGold,compact?23:28,f);
                Box(v.ResultFragments.transform,x+12,y+145,416,34,f);Font(v.ResultFragments,compact?23:28,f);
                Box(v.ResultRetryHint.transform,x,y+193,440,28,f);Font(v.ResultRetryHint,compact?16:20,f);
                Box(v.ResultRetry.transform,x,y+235,212,compact?58:68,f);
                Box(v.ResultLobby.transform,x+228,y+235,212,compact?58:68,f);
                Font(v.ResultRetry.GetComponentInChildren<Text>(),compact?23:28,f);Font(v.ResultLobby.GetComponentInChildren<Text>(),compact?23:28,f);
            }
        }
        static void Font(Text text,int size,float f){text.fontSize=Mathf.RoundToInt(size*f);text.resizeTextForBestFit=false;}
        public static void Box(Transform transform,float x,float y,float width,float height,float factor=1)
        {
            var r=(RectTransform)transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y)*factor;r.sizeDelta=new Vector2(width,height)*factor;
        }
        static void Center(RectTransform r,float width,float height,float f)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(width,height)*f;}
    }
}
