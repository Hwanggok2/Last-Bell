using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public static class RouteV4Migration
    {
        const string Folder="Assets/Game/Art/Presentation/Background";
        static readonly Color Cream=new Color(.95f,.87f,.7f),Navy=new Color(.04f,.085f,.13f);
        [MenuItem("Last Bell/Story/Apply route v4")]
        public static void Apply()
        {
            var loaded=SceneManager.GetSceneByPath(MvpSceneBuilder.ScenePath);
            if(EditorApplication.isPlayingOrWillChangePlaymode||loaded.IsValid()&&loaded.isLoaded&&loaded.isDirty)
                throw new InvalidOperationException("Save scene edits and stop Play mode before migration.");
            PresentationArtImporter.MigrateExisting();
            var root=PrefabUtility.LoadPrefabContents(MvpSceneBuilder.ViewPath);
            try{ConfigureView(root.GetComponent<MvpView>());PrefabUtility.SaveAsPrefabAsset(root,MvpSceneBuilder.ViewPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var worldRoot=PrefabUtility.LoadPrefabContents(ParallaxWorldBuilder.PrefabPath);
            try{ConfigureWorld(worldRoot.GetComponent<ParallaxWorld>());PrefabUtility.SaveAsPrefabAsset(worldRoot,ParallaxWorldBuilder.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(worldRoot);}
            bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene(MvpSceneBuilder.ScenePath,OpenSceneMode.Additive):loaded;
            try
            {
                foreach(var r in scene.GetRootGameObjects())foreach(var p in r.GetComponentsInChildren<MvpPresenter>(true))
                {
                    ConfigureView(p.View);ConfigureWorld(p.Presentation.World);
                    p.EnemyActor.DefaultActorId="gate_sentry";p.EnemyActor.ApplyArt(p.Art.Find("gate_sentry"),false);
                    Dirty(p.EnemyActor);Dirty(p.EnemyActor.Body);Dirty(p.EnemyActor.Body.transform);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_ROUTE_V4_APPLIED");
        }
        static void Dirty(UnityEngine.Object value)
        {EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
        static void ConfigureView(MvpView view)
        {
            if(!view.ReplayPrologue)
            {
                view.ReplayPrologue=Button("ReplayPrologue",view.LobbyPanel.transform,view.ModalBody.font,"프롤로그",Vector2.zero,126);
                var r=(RectTransform)view.ReplayPrologue.transform;r.anchorMin=r.anchorMax=new Vector2(1,1);r.anchoredPosition=new Vector2(-85,-82);Dirty(view);
            }
            if(!view.ComboHintBells)
            {
                var row=Rect("ComboHintBells",view.ModalTitle.transform.parent,new Vector2(0,-30),new Vector2(100,46));view.ComboHintBells=row.gameObject;
                for(int i=0;i<2;i++){var r=Rect("Bell"+i,row,new Vector2(i==0?-27:27,0),new Vector2(34,42));var bell=r.gameObject.AddComponent<SymbolGraphic>();bell.Symbol=UiSymbol.Bell;bell.color=Cream;bell.raycastTarget=false;}
                row.gameObject.SetActive(false);Dirty(view);
            }
            if(view.Prologue){ConfigureLantern(view.Prologue.transform);return;}
            var panel=Rect("Prologue",view.SafeRoot,Vector2.zero,Vector2.zero);Stretch(panel);
            var bg=panel.gameObject.AddComponent<Image>();bg.color=Navy;bg.raycastTarget=true;
            var p=panel.gameObject.AddComponent<PrologueView>();view.Prologue=p;
            var font=view.ModalBody.font;
            p.Title=Label("Title",panel,font,20,new Vector2(0,218),new Vector2(600,40));p.Title.color=new Color(.68f,.56f,.35f);
            p.ReadingWindow=Rect("ReadingWindow",panel,new Vector2(0,10),new Vector2(600,340));p.ReadingWindow.gameObject.AddComponent<RectMask2D>();
            p.Paragraphs=new Text[8];
            for(int i=0;i<8;i++)
            {
                var text=Label("Paragraph"+i,p.ReadingWindow,font,26,Vector2.zero,new Vector2(600,88));text.lineSpacing=1.4f;
                text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,0);p.Paragraphs[i]=text;
            }
            p.Pause=Button("Pause",panel,font,"일시정지",new Vector2(-290,-215),170);p.PauseLabel=p.Pause.GetComponentInChildren<Text>();
            p.Next=Button("Next",panel,font,"다음 문단",new Vector2(-290,-215),170);p.NextLabel=p.Next.GetComponentInChildren<Text>();
            p.ReadingMode=Button("ReadingMode",panel,font,"문단으로 읽기",new Vector2(0,-215),220);p.ReadingLabel=p.ReadingMode.GetComponentInChildren<Text>();
            p.Skip=Button("Skip",panel,font,"건너뛰기",new Vector2(290,-215),170);
            ConfigureLantern(panel);
            panel.gameObject.SetActive(false);Dirty(view);
        }
        static void ConfigureLantern(Transform panel)
        {
            var existing=panel.Find("Lantern");var lantern=existing?(RectTransform)existing:Rect("Lantern",panel,new Vector2(-400,-135),new Vector2(20,32));
            var frame=lantern.GetComponent<Image>()??lantern.gameObject.AddComponent<Image>();frame.color=new Color(.35f,.30f,.20f);frame.raycastTarget=false;Dirty(frame);
            if(lantern.Find("Glass"))return;
            string[] names={"Glass","Cap","Foot","Hook","Post"};
            var positions=new[]{Vector2.zero,new Vector2(0,19),new Vector2(0,-19),new Vector2(0,31),new Vector2(-19,20)};
            var sizes=new[]{new Vector2(12,22),new Vector2(28,5),new Vector2(28,5),new Vector2(3,22),new Vector2(4,124)};
            for(int i=0;i<names.Length;i++){var r=Rect(names[i],lantern,positions[i],sizes[i]);var image=r.gameObject.AddComponent<Image>();image.color=i==0?new Color(.85f,.58f,.22f):new Color(.35f,.30f,.20f);image.raycastTarget=false;}
        }
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static Text Label(string name,Transform parent,Font font,int size,Vector2 pos,Vector2 dimensions)
        {var r=Rect(name,parent,pos,dimensions);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Cream;t.raycastTarget=false;return t;}
        static Button Button(string name,Transform parent,Font font,string caption,Vector2 pos,float width)
        {var r=Rect(name,parent,pos,new Vector2(width,54));var im=r.gameObject.AddComponent<Image>();im.color=new Color(.12f,.21f,.29f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var text=Label("Label",r,font,21,Vector2.zero,r.sizeDelta);text.text=caption;return b;}
        static void ConfigureWorld(ParallaxWorld world)
        {
            var mid=Texture("outskirts-mid-v4",760,360,false);
            foreach(var tile in world.Layers[2].Tiles){var sr=tile.GetComponent<SpriteRenderer>();sr.sprite=mid;Dirty(sr);}
            if(!world.Gate)
            {
                var go=new GameObject("Stage10Gate");go.transform.SetParent(world.transform,false);world.Gate=go.AddComponent<SpriteRenderer>();
            }
            world.GateClosed=Texture("stage10-gate-closed-v4",320,440,false);
            world.GateOpen=Texture("stage10-gate-open-v4",320,440,true);
            world.Gate.sortingOrder=-15;world.ShowStage(1,null,false,false);Dirty(world);Dirty(world.Gate);
        }
        static Sprite Texture(string name,int width,int height,bool open)
        {
            string path=Folder+"/"+name+".png";bool mid=name.StartsWith("outskirts");
            if(!File.Exists(path))
            {
                var pixels=new Color32[width*height];
                Action<int,int,int,int,int> box=(x,y,w,h,c)=>{var color=new Color32((byte)(c>>16),(byte)(c>>8),(byte)c,255);for(int yy=Math.Max(0,y);yy<Math.Min(height,y+h);yy++)for(int xx=Math.Max(0,x);xx<Math.Min(width,x+w);xx++)pixels[(height-1-yy)*width+xx]=color;};
                if(mid)
                {
                    for(int i=0;i<5;i++){int x=i*152;box(x,175,138,125,0x14293f);box(x,170,138,7,0x243e52);box(x+14,135,20,165,0x14293f);box(x+8,128,32,10,0x243e52);}
                }
                else
                {
                    box(0,435,320,5,0x101f2b);box(10,24,62,416,0x31526c);box(248,24,62,416,0x31526c);
                    box(48,22,224,66,0x31526c);box(28,0,34,40,0x4e6d82);box(258,0,34,40,0x4e6d82);
                    box(72,80,176,358,0x102232);
                    for(int y=45;y<420;y+=42){box(12,y,56,4,0x20394e);box(252,y,56,4,0x20394e);}
                    if(!open){for(int x=82;x<243;x+=29)box(x,88,7,352,0x60727a);box(74,255,172,16,0x826e4d);}
                    else {box(72,88,16,352,0x718485);box(232,88,16,352,0x718485);box(46,255,42,16,0xc0a166);}
                    box(151,38,18,25,open?0xf1d28b:0x826e4d);box(146,62,28,5,open?0xf1d28b:0x826e4d);box(157,68,6,5,0xd8ac55);
                }
                var t=new Texture2D(width,height,TextureFormat.RGBA32,false);t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.spritePixelsPerUnit=1/(ParallaxWorld.UnitsPerPixel*(mid?2:1));
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=mid?new Vector2(320f/width,.5f):new Vector2(.5f,0);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static void Validate()
        {
            var view=AssetDatabase.LoadAssetAtPath<GameObject>(MvpSceneBuilder.ViewPath).GetComponent<MvpView>();
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(ParallaxWorldBuilder.PrefabPath).GetComponent<ParallaxWorld>();
            if(!view.Prologue||view.Prologue.Paragraphs.Length!=8||!view.Prologue.Skip||!world.Gate||!world.GateClosed||!world.GateOpen)
                throw new InvalidOperationException("Missing saved route/onboarding bindings.");
        }
        public static void ApplyAndBuild(){Apply();MvpBuild.Windows();}
    }
}
