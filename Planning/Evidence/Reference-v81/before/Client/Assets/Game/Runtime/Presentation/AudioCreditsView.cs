using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // An authored lobby overlay; audio provenance lives in the referenced text asset.
    public sealed class AudioCreditsView : MonoBehaviour
    {
        public Button OpenButton,CloseButton,SourceButton,LicenseButton;
        public GameObject Panel;
        public RectTransform Dialog;
        public ScrollRect Scroll;
        public Text Heading,Body;
        public TextAsset Credits;
        public string SourceUrl,LicenseUrl;
        public bool IsOpen => Panel&&Panel.activeSelf;
        void Awake()
        {
            OpenButton.onClick.AddListener(Open);CloseButton.onClick.AddListener(Close);
            SourceButton.onClick.AddListener(()=>Application.OpenURL(SourceUrl));
            LicenseButton.onClick.AddListener(()=>Application.OpenURL(LicenseUrl));
            Body.text=Credits?Credits.text:"";Close();
        }
        public void Open()
        {
            if(!OpenButton.isActiveAndEnabled)return;
            Panel.SetActive(true);Panel.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();Scroll.verticalNormalizedPosition=1;
        }
        public void Close(){if(Panel)Panel.SetActive(false);}
        public void Layout(float width,float height,bool compact)
        {
            float f=height/(compact?360:540),w=width/f,h=height/f;
            FeedbackLayout.Box(OpenButton.transform,compact?100:120,compact?4:8,76,48,f);
            OpenButton.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(12*f);
            float dw=Mathf.Min(w-32,760),dh=Mathf.Min(h-32,480);
            FeedbackLayout.Box(Dialog,(w-dw)/2,(h-dh)/2,dw,dh,f);
            FeedbackLayout.Box(Heading.transform,20,12,dw-40,30,f);Heading.fontSize=Mathf.RoundToInt((compact?21:25)*f);
            FeedbackLayout.Box(Scroll.transform,20,54,dw-40,dh-132,f);Body.fontSize=Mathf.RoundToInt((compact?15:18)*f);
            float bw=(dw-64)/3;var buttons=new[]{SourceButton,LicenseButton,CloseButton};
            for(int i=0;i<buttons.Length;i++)
            {
                FeedbackLayout.Box(buttons[i].transform,20+i*(bw+12),dh-64,bw,48,f);
                buttons[i].GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((compact?15:18)*f);
            }
        }
    }
}
