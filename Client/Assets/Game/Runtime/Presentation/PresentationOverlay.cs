using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class PresentationOverlay : MaskableGraphic
    {
        public Text StorySpeaker,StoryText;
        public Button ButtonNext,ButtonSkip;
        int mode;
        float age,opacity=1;
        public void ShowDeath(float ageSeconds){mode=1;age=ageSeconds;SetStoryVisible(false);SetVerticesDirty();}
        public void SetFade(float alpha){mode=2;opacity=Mathf.Clamp01(alpha);SetStoryVisible(false);SetVerticesDirty();}
        public void ShowStory(string text,float alpha=1){ShowStory("",text,alpha);}
        public void ShowStory(string speaker,string text,float alpha=1)
        {
            mode=3;opacity=Mathf.Clamp01(alpha);SetStoryVisible(true);
            if(StorySpeaker)StorySpeaker.text=speaker??"";if(StoryText)StoryText.text=text??"";SetVerticesDirty();
        }
        public void Hide(){mode=0;SetStoryVisible(false);SetVerticesDirty();}
        void SetStoryVisible(bool show)
        {
            if(StorySpeaker)StorySpeaker.gameObject.SetActive(show);if(StoryText)StoryText.gameObject.SetActive(show);
            if(ButtonNext)ButtonNext.gameObject.SetActive(show);if(ButtonSkip)ButtonSkip.gameObject.SetActive(show);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();Color black=new Color(3/255f,7/255f,13/255f,1);
            if(mode==0)return;
            if(mode==2){PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Alpha(black,opacity));return;}
            if(mode==3)
            {
                PixelVfxMesh.Rect(vh,new Rect(r.xMin,r.yMin,r.width,145),PixelVfxMesh.Alpha(black,.94f*opacity));
                PixelVfxMesh.Rect(vh,new Rect(r.xMin+24,r.yMin+142,r.width-48,2),PixelVfxMesh.Alpha(PixelVfxMesh.Old,opacity));return;
            }
            if(age<.85f)
            {
                PixelVfxMesh.Rect(vh,r,PixelVfxMesh.Alpha(black,Mathf.Clamp01((age-.25f)/.45f)));return;
            }
            float p=Mathf.Clamp01((age-.85f)/.75f),e=p*p*(3-2*p);
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
