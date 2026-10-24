using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class PrologueView : MonoBehaviour
    {
        public RectTransform ReadingWindow;
        public RectTransform Scroll, Lantern;
        public StoryDecorGraphic LampGlow;
        public Text Title, PauseLabel, ReadingLabel, NextLabel;
        public Text[] Paragraphs;
        public Button Pause, Skip, ReadingMode, Next;
        public void Sample(GameSession session)
        {
            var content=session.Catalog.onboarding;
            if(LampGlow)LampGlow.SetLight(session.ProloguePaused?0:Time.unscaledTime);
            Title.text=content.prologueTitle;
            bool manual=session.PrologueManual;int page=Mathf.Min(content.prologueParagraphs.Length-1,(int)(session.PrologueProgress*content.prologueParagraphs.Length));
            float height=ReadingWindow.rect.height,spacing=Paragraphs[0].fontSize*4.3f,total=content.prologueParagraphs.Length*spacing;
            for(int i=0;i<Paragraphs.Length;i++)
            {
                var text=Paragraphs[i];bool visible=i<content.prologueParagraphs.Length&&(!manual||i==page);
                text.gameObject.SetActive(visible);if(!visible)continue;
                text.text=content.prologueParagraphs[i];
                float y=manual?height*.5f:-(i*spacing)+session.PrologueProgress*(total+height)-spacing*.45f;
                text.rectTransform.anchoredPosition=new Vector2(0,y);
                var color=text.color;color.a=manual?1:Mathf.Clamp01(Mathf.Min(y,height-y)/40);text.color=color;
            }
            Pause.gameObject.SetActive(false);Next.gameObject.SetActive(false);ReadingMode.gameObject.SetActive(false);
            PauseLabel.text=session.ProloguePaused?"계속":"일시정지";
            ReadingLabel.text=manual?"자동 스크롤":"문단으로 읽기";
            NextLabel.text=session.PrologueParagraph==content.prologueParagraphs.Length-1?"시작하기":"다음 문단";
        }
    }
}
