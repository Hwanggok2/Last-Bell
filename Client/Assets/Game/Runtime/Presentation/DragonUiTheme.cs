using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // Saved decoration bindings. FeedbackLayout owns layout; this adapter follows the
    // fixed combat home anchors, never the attacking/dodging actor transform.
    public sealed class DragonUiTheme : MonoBehaviour
    {
        public MvpView View;
        public ReferencePanelGraphic BattleShelf,StagePanel;
        public SymbolGraphic StageCoin,WalletGem,PauseSymbol,RelicBag;
        public Image LobbyBackdrop,PrologueBackdrop;
        public float HealthGap=64,ShelfGap=32;
        Vector2 previousPlayer,previousEnemy,previousSize;
        float previousPixelScale;
        bool previousBoss,positioned;

        public void Layout(float width,float height)
        {
            float scale=height/720;
            Box(StagePanel.transform,16,16,244,104,width,height);
            Box(View.Stage.transform,32,28,212,40,width,height);
            Box(StageCoin.transform,30,76,32,36,width,height);
            Box(View.BattleGold.transform,76,76,168,36,width,height);
            Box(View.Pause.transform,1160,16,96,96,width,height);
            Center(PauseSymbol.rectTransform,32*scale,44*scale);
            View.Stage.fontSize=View.BattleGold.fontSize=View.EnemyName.fontSize=Mathf.RoundToInt(32*scale);
            View.EnemyName.alignment=TextAnchor.MiddleCenter;
            var pauseLabel=View.Pause.GetComponentInChildren<Text>(true);if(pauseLabel)pauseLabel.enabled=false;
            Box(View.Attack.transform,1120,564,144,144,width,height);
            Center(View.AttackSymbol.rectTransform,104*scale,108*scale);
            foreach(var symbol in View.DodgeSymbols)if(symbol.NormalSprite)
                Center(symbol.rectTransform,symbol.NormalSprite.rect.width*scale,symbol.NormalSprite.rect.height*scale);
            Box(WalletGem.transform,1088,23,36,36,width,height);
            FeedbackLayout.Box(RelicBag.transform,12*scale,12*scale,36*scale,44*scale);
            var relicLabel=View.OpenRelics.GetComponentInChildren<Text>(true);
            if(relicLabel)
            {
                relicLabel.text="유물";var rect=relicLabel.rectTransform;
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);
                rect.offsetMin=new Vector2(44*scale,0);rect.offsetMax=new Vector2(-8*scale,0);
            }
            foreach(var panel in View.GetComponentsInChildren<ReferencePanelGraphic>(true))if(panel.DragonTheme)panel.Cell=4*scale;
            positioned=false;
        }
        public void Sample(MvpPresenter presenter,bool boss)
        {
            bool story=presenter.Session.Phase==SessionPhase.StoryDialogue;
            BattleShelf.gameObject.SetActive(!story);StagePanel.gameObject.SetActive(!story);StageCoin.gameObject.SetActive(!story);
            var size=View.SafeRoot.rect.size;float scale=size.y/720;
            var origin=presenter.WorldRoot;
            var offset=presenter.Effects.WorldOffset;
            var player=presenter.ScreenPoint(origin.TransformPoint(ParallaxWorld.PlanPoint(500,590))-offset);
            var enemy=presenter.ScreenPoint(origin.TransformPoint(ParallaxWorld.PlanPoint(900,590))-offset);
            var canvas=View.GetComponentInParent<Canvas>();float pixelScale=canvas?canvas.scaleFactor:1;
            if(!positioned||player!=previousPlayer||enemy!=previousEnemy||size!=previousSize||boss!=previousBoss||pixelScale!=previousPixelScale)
            {
                previousPlayer=player;previousEnemy=enemy;previousSize=size;previousBoss=boss;previousPixelScale=pixelScale;positioned=true;
                At(View.PlayerHpRoot,player+Vector2.down*HealthGap*scale,new Vector2(208,44)*scale);
                At(View.EnemyHpRoot,boss?new Vector2(0,size.y*.5f-38*scale):enemy+Vector2.down*HealthGap*scale,new Vector2(boss?560:224,44)*scale);
                SnapHealthToScreenPixels(View.PlayerHpRoot,canvas);SnapHealthToScreenPixels(View.EnemyHpRoot,canvas);
                // The centered direction cue occupies x640/y92; keep boss names in its right-hand margin.
                At(View.EnemyName.rectTransform,boss?new Vector2(260*scale,size.y*.5f-84*scale):enemy+Vector2.down*(HealthGap+52)*scale,new Vector2(boss?440:300,40)*scale);
                float top=size.y*.5f-Mathf.Min(player.y,enemy.y)+ShelfGap*scale;
                FeedbackLayout.Box(BattleShelf.transform,0,Mathf.Clamp(top,0,size.y),size.x,Mathf.Max(0,size.y-top));
            }
        }
        public static void Select(Button button,bool selected)
        {
            if(!button)return;foreach(var panel in button.GetComponentsInChildren<ReferencePanelGraphic>(true))if(panel.DragonTheme)panel.SetSelected(selected);
        }
        static void At(RectTransform rect,Vector2 position,Vector2 size)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;}
        static void SnapHealthToScreenPixels(RectTransform rect,Canvas canvas)
        {
            if(!canvas)return;
            Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            // Snap one edge in physical pixels, not Canvas units (640p scales by 2/3).
            // Translation is at most half a screen pixel per axis; size/cell count stay intact.
            var edge=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.min));
            var snapped=new Vector2(Mathf.Round(edge.x),Mathf.Round(edge.y));
            var parent=(RectTransform)rect.parent;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,edge,camera,out var before)&&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,snapped,camera,out var after))
                rect.anchoredPosition+=after-before;
        }
        static void Center(RectTransform rect,float width,float height)=>At(rect,Vector2.zero,new Vector2(width,height));
        static void Box(Transform rect,float x,float y,float width,float height,float screenWidth,float screenHeight)
        {FeedbackLayout.Box(rect,x*screenWidth/1280,y*screenHeight/720,width*screenWidth/1280,height*screenHeight/720);}
    }
}
