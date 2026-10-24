using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    [Serializable]
    public sealed class DialoguePortraitEntry
    {
        public string Id;
        public Sprite Sprite;
    }

    public sealed class PresentationOverlay : MaskableGraphic
    {
        public Text StorySpeaker,StoryText;
        public Image StoryPortrait;
        public DialoguePortraitEntry[] Portraits=Array.Empty<DialoguePortraitEntry>();
        public Button ButtonNext,ButtonSkip;
        public Button ButtonJoin,ButtonLater;
        public bool DragonTheme;
        public bool StoryV5;
        public bool PlayerSpeaking {get;private set;}
        bool companionChoice;
        int mode;
        float age,opacity=1;
        public void ShowDeath(float ageSeconds){mode=1;age=ageSeconds;SetStoryVisible(false);SetVerticesDirty();}
        public void SetFade(float alpha){mode=2;opacity=Mathf.Clamp01(alpha);SetStoryVisible(false);SetVerticesDirty();}
        public void ShowStory(string text,float alpha=1){ShowStory("",text,"",alpha);}
        public void ShowStory(string speaker,string text,float alpha=1){ShowStory(speaker,text,"",alpha);}
        public void ShowStory(string speaker,string text,string portraitId,float alpha=1)
        {
            mode=3;opacity=Mathf.Clamp01(alpha);SetStoryVisible(true);
            PlayerSpeaking=portraitId=="on"||portraitId=="dam"||portraitId=="rin";
            if(StorySpeaker)StorySpeaker.text=speaker??"";if(StoryText)StoryText.text=text??"";
            if(StoryPortrait)
            {
                var entry=Array.Find(Portraits??Array.Empty<DialoguePortraitEntry>(),x=>x!=null&&x.Id==portraitId);
                StoryPortrait.sprite=entry?.Sprite;
                StoryPortrait.gameObject.SetActive(StoryPortrait.sprite);
                StoryPortrait.color=new Color(1,1,1,opacity);
            }
            if(StoryV5)LayoutV5();
            SetVerticesDirty();
        }
        // One owner for all v5 dialogue layout, sampled in the safe-area canvas.
        public void LayoutV5()
        {
            if(!StoryV5||!StorySpeaker||!StoryText)return;
            var r=rectTransform.rect;float s=Mathf.Min(r.width/1280f,r.height/720f);
            if(s<=0)return;
            float x=(r.width-1280*s)*.5f,y=r.height-720*s;
            Place(StoryPortrait.rectTransform,PlayerSpeaking?48:1056,384,144,144,s,x,y);
            float nameX=PlayerSpeaking?216:48;
            StorySpeaker.fontSize=Mathf.RoundToInt(36*s);StorySpeaker.fontStyle=FontStyle.Normal;
            StorySpeaker.alignment=TextAnchor.MiddleLeft;StorySpeaker.resizeTextForBestFit=false;
            Place(StorySpeaker.rectTransform,nameX+24,448,320,40,s,x,y);
            Place(StoryText.rectTransform,56,532,1008,132,s,x,y);
            StoryText.fontSize=Mathf.RoundToInt(32*s);StoryText.fontStyle=FontStyle.Normal;
            StoryText.alignment=TextAnchor.UpperLeft;StoryText.lineSpacing=1.375f;
            StoryText.horizontalOverflow=HorizontalWrapMode.Wrap;StoryText.verticalOverflow=VerticalWrapMode.Truncate;
            StoryText.resizeTextForBestFit=false;
            LayoutButton(ButtonNext,1104,568,128,96,s,x,y);
            LayoutButton(ButtonSkip,1104,24,152,96,s,x,y);
            LayoutButton(ButtonJoin,848,568,192,96,s,x,y);
            LayoutButton(ButtonLater,1056,568,176,96,s,x,y);
            if(companionChoice)Place(StoryText.rectTransform,56,532,744,132,s,x,y);
        }
        public void ShowCompanionChoice(bool show)
        {
            companionChoice=show;
            if(ButtonJoin)ButtonJoin.gameObject.SetActive(show);
            if(ButtonLater)ButtonLater.gameObject.SetActive(show);
            if(show&&ButtonNext)ButtonNext.gameObject.SetActive(false);
            if(show&&ButtonSkip)ButtonSkip.gameObject.SetActive(false);
            if(StoryV5)LayoutV5();
        }
        static void Place(RectTransform t,float x,float y,float w,float h,float s,float ox,float oy)
        {
            if(!t)return;t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);
            t.anchoredPosition=new Vector2(ox+x*s,-oy-y*s);t.sizeDelta=new Vector2(w*s,h*s);t.localScale=Vector3.one;
        }
        static void LayoutButton(Button button,float x,float y,float w,float h,float s,float ox,float oy)
        {
            if(!button)return;Place((RectTransform)button.transform,x,y,w,h,s,ox,oy);
            var text=button.GetComponentInChildren<Text>(true);if(text){text.fontSize=Mathf.RoundToInt(28*s);text.resizeTextForBestFit=false;text.alignment=TextAnchor.MiddleCenter;}
        }
        Rect PlanRect(Rect r,float x,float y,float w,float h)
        {
            float s=Mathf.Min(r.width/1280f,r.height/720f),ox=(r.width-1280*s)*.5f;
            return new Rect(r.xMin+ox+x*s,r.yMin+(720-y-h)*s,w*s,h*s);
        }
        public void Hide(){mode=0;SetStoryVisible(false);SetVerticesDirty();}
        void SetStoryVisible(bool show)
        {
            if(StorySpeaker)StorySpeaker.gameObject.SetActive(show);if(StoryText)StoryText.gameObject.SetActive(show);
            if(StoryPortrait)StoryPortrait.gameObject.SetActive(show&&StoryPortrait.sprite);
            if(ButtonNext)ButtonNext.gameObject.SetActive(show);if(ButtonSkip)ButtonSkip.gameObject.SetActive(show);
            if(!show){companionChoice=false;if(ButtonJoin)ButtonJoin.gameObject.SetActive(false);if(ButtonLater)ButtonLater.gameObject.SetActive(false);}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();Color black=new Color(3/255f,7/255f,13/255f,1);
            if(mode==0)return;
            if(mode==2){PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Alpha(black,opacity));return;}
            if(mode==3)
            {
                if(DragonTheme)
                {
                    if(StoryV5)
                    {
                        var panel=PlanRect(r,24,488,1232,208);float cell=r.height/720*4;
                        DragonUiPalette.Stepped(vh,panel,cell,PixelVfxMesh.Alpha(DragonUiPalette.Edge,opacity));
                        PixelVfxMesh.Rect(vh,new Rect(panel.x+cell,panel.y+cell,panel.width-2*cell,panel.height-2*cell),PixelVfxMesh.Alpha(DragonUiPalette.Paper,opacity));
                        float nameWidth=Mathf.Max(176,StorySpeaker.preferredWidth/Mathf.Max(.01f,r.height/720)+48);
                        var badge=PlanRect(r,PlayerSpeaking?216:48,440,nameWidth,56);
                        DragonUiPalette.Stepped(vh,badge,cell,PixelVfxMesh.Alpha(DragonUiPalette.Edge,opacity));
                        PixelVfxMesh.Rect(vh,new Rect(badge.x+cell,badge.y+cell,badge.width-2*cell,badge.height-2*cell),PixelVfxMesh.Alpha(DragonUiPalette.Paper,opacity));
                        return;
                    }
                    PixelVfxMesh.Rect(vh,new Rect(r.xMin,r.yMin,r.width,184),PixelVfxMesh.Alpha(DragonUiPalette.Paper,opacity));
                    PixelVfxMesh.Rect(vh,new Rect(r.xMin,r.yMin+180,r.width,4),PixelVfxMesh.Alpha(DragonUiPalette.Edge,opacity));
                    if(StoryPortrait&&StoryPortrait.gameObject.activeSelf)
                    {
                        DragonUiPalette.Stepped(vh,new Rect(r.xMin+24,r.yMin+14,152,152),4,PixelVfxMesh.Alpha(DragonUiPalette.Edge,opacity));
                        PixelVfxMesh.Rect(vh,new Rect(r.xMin+28,r.yMin+18,144,144),PixelVfxMesh.Alpha(DragonUiPalette.Light,opacity));
                    }
                    return;
                }
                PixelVfxMesh.Rect(vh,new Rect(r.xMin,r.yMin,r.width,145),PixelVfxMesh.Alpha(black,.94f*opacity));
                PixelVfxMesh.Rect(vh,new Rect(r.xMin+24,r.yMin+142,r.width-48,2),PixelVfxMesh.Alpha(PixelVfxMesh.Old,opacity));return;
            }
            if(age<.85f)
            {
                PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Alpha(black,Mathf.Clamp01((age-.25f)/.45f)));return;
            }
            float p=Mathf.Clamp01((age-.85f)/.75f),e=p*p*(3-2*p);
            if(DragonTheme){PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Alpha(DragonUiPalette.Ink,1-e));return;}
            Vector2 lamp=new Vector2(r.xMin+r.width*520/1280,r.yMax-r.height*130/720);
            float radius=18.75f+e*Mathf.Max(r.width,r.height)*.9f;
            // Coarse cells make a stepped light falloff instead of a smooth modern spotlight.
            if(p<1)
            {
                const float cell=12;
                for(float y=r.yMin;y<r.yMax;y+=cell)for(float x=r.xMin;x<r.xMax;x+=cell)
                {
                    float distance=Vector2.Distance(new Vector2(x+cell*.5f,y+cell*.5f),lamp);
                    float shade=Mathf.Clamp01((distance-radius)/100+.35f)*(1-p);
                    shade=Mathf.Round(shade*6)/6;
                    if(shade>0)PixelVfxMesh.Rect(vh,new Rect(x,y,Mathf.Min(cell,r.xMax-x),Mathf.Min(cell,r.yMax-y)),PixelVfxMesh.Alpha(black,shade));
                }
            }
            PixelVfxMesh.Rect(vh,new Rect(lamp.x-1.5f,lamp.y+20,3,r.yMax-lamp.y-20),PixelVfxMesh.Old);
            PixelVfxMesh.Rect(vh,new Rect(lamp.x-14.25f,lamp.y+13.5f,28.5f,6),PixelVfxMesh.Old);
            PixelVfxMesh.Rect(vh,new Rect(lamp.x-12.75f,lamp.y-19.5f,25.5f,33),PixelVfxMesh.Dark);
            PixelVfxMesh.Rect(vh,new Rect(lamp.x-9,lamp.y-15,18,25.5f),Color.Lerp(PixelVfxMesh.Old,PixelVfxMesh.Gold,p));
            PixelVfxMesh.Rect(vh,new Rect(lamp.x-3.75f,lamp.y-11.25f,7.5f,16.5f),Color.Lerp(PixelVfxMesh.Gold,PixelVfxMesh.Cream,p));
            PixelVfxMesh.Rect(vh,new Rect(lamp.x-15,lamp.y-24.75f,30,6),PixelVfxMesh.Old);
        }
    }
}
