using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBell.Editor
{
    public static class MvpSceneBuilder
    {
        public const string ScenePath="Assets/Game/Scenes/Mvp.unity";
        public const string ViewPath="Assets/Game/Prefabs/GameView.prefab";
        public const string ActorPath="Assets/Game/Prefabs/Actor.prefab";
        public const string UiPixelPath="Assets/Game/Art/UI/WhitePixel.png";
        static readonly Color Navy=new Color(.055f,.10f,.16f),Blue=new Color(.16f,.26f,.35f),Cream=new Color(.94f,.83f,.62f),Gold=new Color(.91f,.58f,.16f),Line=new Color(.35f,.45f,.50f);
        static Font font;
        static Sprite white;

        [MenuItem("Last Bell/MVP/Create Missing")]
        public static void CreateMissing()
        {
            Directory.CreateDirectory("Assets/Game/Prefabs");Directory.CreateDirectory("Assets/Game/Scenes");AssetDatabase.Refresh();
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NanumGothic-Regular.ttf");
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            white=CreateMissingUiPixel();
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath))
            {
                var actor=MakeActor();PrefabUtility.SaveAsPrefabAsset(actor.gameObject,ActorPath);UnityEngine.Object.DestroyImmediate(actor.gameObject);
            }
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath))
            {
                var view=MakeView();PrefabUtility.SaveAsPrefabAsset(view.gameObject,ViewPath);UnityEngine.Object.DestroyImmediate(view.gameObject);
            }
            if(!File.Exists(ScenePath))
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=new GameObject("LastBellMvp");var presenter=root.AddComponent<MvpPresenter>();
                var cameraObject=new GameObject("WorldCamera");var camera=cameraObject.AddComponent<Camera>();cameraObject.tag="MainCamera";
                camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=3.1f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Navy;
                presenter.WorldCamera=camera;
                var world=new GameObject("World");world.transform.SetParent(root.transform,false);presenter.WorldRoot=world.transform;
                var background=new GameObject("Backdrop");background.transform.SetParent(world.transform,false);var backdrop=background.AddComponent<SpriteRenderer>();backdrop.sprite=white;backdrop.color=Blue;backdrop.sortingOrder=-10;background.transform.localScale=new Vector3(15,7,1);presenter.Background=backdrop;
                var actorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath);
                var hero=(GameObject)PrefabUtility.InstantiatePrefab(actorPrefab,world.transform);hero.name="Player";hero.transform.localPosition=new Vector3(-.8f,-1.984f,0);presenter.PlayerActor=hero.GetComponent<ActorView>();
                var enemy=(GameObject)PrefabUtility.InstantiatePrefab(actorPrefab,world.transform);enemy.name="Enemy";enemy.transform.localPosition=new Vector3(1.45f,-1.984f,0);presenter.EnemyActor=enemy.GetComponent<ActorView>();presenter.EnemyActor.Body.color=Gold;
                var viewObject=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath));presenter.View=viewObject.GetComponent<MvpView>();
                presenter.Effects=viewObject.GetComponentInChildren<BattleEffects>(true);presenter.Effects.WorldRoot=world.transform;
                presenter.Art=AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/Game/Art/MvpArtCatalog.asset");
                if(presenter.Art){presenter.PlayerActor.ApplyArt(presenter.Art.Find("hero_on"),true);presenter.EnemyActor.ApplyArt(presenter.Art.Find("gate_sentinel"),false);}
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(root.transform,false);
                AttachBootstrap(root,presenter);
                EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            }
            AssetDatabase.SaveAssets();Validate();
        }

        // Only links Bootstrap when its separately-owned script is present. Never constructs the UI in play mode.
        public static void AttachBootstrap(GameObject root,MvpPresenter presenter)
        {
            var type=typeof(MvpPresenter).Assembly.GetType("LastBell.GameBootstrap");if(type==null)return;
            var component=root.GetComponent(type)??root.AddComponent(type);var so=new SerializedObject(component);
            foreach(string field in new[]{"presenter","Presenter"}){var p=so.FindProperty(field);if(p!=null)p.objectReferenceValue=presenter;}
            var catalog=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Generated/mvp-catalog.json");
            foreach(string field in new[]{"catalogJson","catalogAsset","CatalogJson","CatalogAsset"}){var p=so.FindProperty(field);if(p!=null)p.objectReferenceValue=catalog;}
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(component);
        }

        [MenuItem("Last Bell/MVP/Migrate Existing Input Targets And Visuals")]
        public static void MigrateExisting()
        {
            // Explicit migration for the first generated prefab. Recognized initial sizes only;
            // never delete/recreate the scene or overwrite a manually customized target size.
            white=CreateMissingUiPixel();
            var initialSprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var root=PrefabUtility.LoadPrefabContents(ViewPath);
            try
            {
                var v=root.GetComponent<MvpView>();
                // Replace only the first draft's stretched rounded sprite. Keep authored art,
                // fill settings, colors, input targets and prefab references intact.
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                    if(image.sprite==initialSprite)image.sprite=white;
                ResizeInitial((RectTransform)v.Pause.transform,new Vector2(56,56),new Vector2(72,72),new Vector2(-48,-48));
                foreach(var row in v.Stats)ResizeInitial((RectTransform)row.Upgrade.transform,new Vector2(64,64),new Vector2(72,72),new Vector2(-42,0));
                ResizeInitial((RectTransform)v.RelicBack.transform,new Vector2(92,52),new Vector2(92,72),new Vector2(-58,-42));
                ResizeInitial((RectTransform)v.ModalConfirm.transform,new Vector2(268,64),new Vector2(268,72));
                ResizeInitial((RectTransform)v.ModalCancel.transform,new Vector2(268,64),new Vector2(268,72));
                ResizeInitial((RectTransform)v.Resume.transform,new Vector2(220,64),new Vector2(220,72));
                ResizeInitial((RectTransform)v.ReturnHome.transform,new Vector2(220,64),new Vector2(220,72));
                MigrateFooter((RectTransform)v.OpenRelics.transform,.025f,.14f,12,84);
                MigrateFooter((RectTransform)v.StartRun.transform,.025f,.14f,12,84);
                MigrateFooter((RectTransform)v.SlotPurchase.transform,.025f,.18f,12,84);
                MigrateFooter((RectTransform)v.RelicEquip.transform,.025f,.18f,12,84);
                foreach(var reward in v.Rewards)MigrateFooter((RectTransform)reward.Receive.transform,.035f,.2f,14,86);
                var boss=(RectTransform)v.BossHpRoot.transform;
                if(boss.sizeDelta==new Vector2(650,14))AnchorWidth(boss,new Vector2(0,1),new Vector2(1,1),new Vector2(160,-43),new Vector2(-104,-27));
                for(int i=0;i<v.RelicItems.Length;i++)
                {
                    var r=(RectTransform)v.RelicItems[i].transform;
                    if(Mathf.Approximately(r.sizeDelta.y,70))AnchorWidth(r,new Vector2(r.anchorMin.x,1),new Vector2(r.anchorMax.x,1),new Vector2(r.offsetMin.x,-72-i*78),new Vector2(r.offsetMax.x,-i*78));
                }
                var content=(RectTransform)v.RelicItems[0].transform.parent;if(Mathf.Approximately(content.sizeDelta.y,760))content.sizeDelta=new Vector2(content.sizeDelta.x,780);
                var toggle=(RectTransform)v.ReducedShake.transform;ResizeInitial(toggle,new Vector2(400,56),new Vector2(400,72));
                if(!toggle.GetComponent<Image>()){var hit=toggle.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;}
                if(v.StunStars==null||v.StunStars.Length!=3)v.StunStars=new SymbolGraphic[3];
                for(int i=0;i<3;i++)if(!v.StunStars[i])
                {
                    var item=v.BattlePanel.transform.Find("StunStar"+i);if(!item)continue;
                    var text=item.GetComponent<Text>();if(text)UnityEngine.Object.DestroyImmediate(text);
                    var star=item.GetComponent<SymbolGraphic>()??item.gameObject.AddComponent<SymbolGraphic>();star.Symbol=UiSymbol.Star;star.color=Cream;star.raycastTarget=false;v.StunStars[i]=star;
                }
                PrefabUtility.SaveAsPrefabAsset(root,ViewPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            MigrateInitialWorld(initialSprite);
            AssetDatabase.SaveAssets();Validate();Debug.Log("LAST_BELL_UI_INPUT_MIGRATION_COMPLETE; LAST_BELL_UI_VISUAL_MIGRATION_COMPLETE");
        }
        static Sprite CreateMissingUiPixel()
        {
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(UiPixelPath);if(existing)return existing;
            if(File.Exists(UiPixelPath))throw new InvalidOperationException("Existing UI pixel is not an imported Sprite: "+UiPixelPath);
            Directory.CreateDirectory(Path.GetDirectoryName(UiPixelPath));
            var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
            try{pixel.SetPixel(0,0,Color.white);pixel.Apply();File.WriteAllBytes(UiPixelPath,pixel.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(pixel);}
            AssetDatabase.ImportAsset(UiPixelPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(UiPixelPath);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=1;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(UiPixelPath);
        }
        static void MigrateInitialWorld(Sprite initialSprite)
        {
            var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                bool changed=false;
                foreach(var root in scene.GetRootGameObjects())foreach(var presenter in root.GetComponentsInChildren<MvpPresenter>(true))
                {
                    if(presenter.EnemyActor)
                    {
                        var enemy=presenter.EnemyActor.transform;var position=enemy.localPosition;
                        if(Mathf.Approximately(position.x,2.25f)){position.x=1.45f;enemy.localPosition=position;PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);changed=true;}
                    }
                    if(!presenter.WorldRoot)continue;
                    var ground=presenter.WorldRoot.Find("Ground");if(!ground)continue;
                    var renderer=ground.GetComponent<SpriteRenderer>();
                    if(renderer&&renderer.sprite==initialSprite&&ground.localPosition==new Vector3(0,-2.65f,0)&&ground.gameObject.activeSelf)
                    {ground.gameObject.SetActive(false);changed=true;}
                }
                if(changed)EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static void ResizeInitial(RectTransform r,Vector2 oldSize,Vector2 newSize,Vector2? position=null)
        {
            if(r.sizeDelta!=oldSize)return;r.sizeDelta=newSize;if(position.HasValue)r.anchoredPosition=position.Value;
        }
        static void MigrateFooter(RectTransform r,float oldMin,float oldMax,float bottom,float top)
        {
            if(!Mathf.Approximately(r.anchorMin.y,oldMin)||!Mathf.Approximately(r.anchorMax.y,oldMax))return;
            AnchorWidth(r,new Vector2(r.anchorMin.x,0),new Vector2(r.anchorMax.x,0),new Vector2(r.offsetMin.x,bottom),new Vector2(r.offsetMax.x,top));
        }

        [MenuItem("Last Bell/MVP/Validate")]
        public static void Validate()
        {
            var view=AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath);var actor=AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath);
            if(!view||!actor||!File.Exists(ScenePath))throw new InvalidOperationException("Missing saved MVP scene/prefabs; use Create Missing.");
            var binding=view.GetComponent<MvpView>();
            if(!binding||!binding.SafeRoot||!binding.BattlePanel||!binding.LobbyPanel||!binding.RewardPanel||!binding.RelicPanel||binding.Stats.Length!=4||binding.Rewards.Length!=3||binding.Dodge.Length!=4)
                throw new InvalidOperationException("GameView serialized references are incomplete.");
            foreach(var row in binding.Stats)if(!row.Title||!row.Value||!row.Level||!row.Upgrade||row.Stars.Length!=5||row.Bells.Length!=10)throw new InvalidOperationException("Incomplete saved stat row.");
            float minDodge=binding.GetComponent<FeedbackLayout>()?64:72;
            foreach(var key in binding.Dodge){var r=(RectTransform)key.transform;if(r.sizeDelta.x<minDodge||r.sizeDelta.y<minDodge)throw new InvalidOperationException("Dodge hit area is smaller than its authored layout. FeedbackLayout uses 64px at960 and 56px at640.");}
            var effects=view.GetComponentInChildren<BattleEffects>(true);if(!effects||effects.Numbers.Length<6||effects.Fragments.Length<16)throw new InvalidOperationException("Effect pools missing.");
            Debug.Log("MVP saved scene, GameView/Actor prefabs, input targets and pooled VFX validated.");
        }

        static ActorView MakeActor()
        {
            var root=new GameObject("Actor");var view=root.AddComponent<ActorView>();var body=new GameObject("Body");body.transform.SetParent(root.transform,false);
            view.Body=body.AddComponent<SpriteRenderer>();view.Body.sprite=white;view.Body.color=Cream;view.Body.sortingOrder=2;body.transform.localScale=new Vector3(.55f,1.1f,1);view.SpriteOffset=Vector3.up*.55f;return view;
        }
        static MvpView MakeView()
        {
            var root=new GameObject("GameView",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.layer=5;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(960,540);scaler.matchWidthOrHeight=1;
            var v=root.AddComponent<MvpView>();var safe=Rect("SafeArea",root.transform);Stretch(safe);safe.gameObject.AddComponent<SafeAreaLayout>();v.SafeRoot=safe;
            v.BattlePanel=Panel("Battle",safe,false);v.LobbyPanel=Panel("Lobby",safe,true);v.RelicPanel=Panel("Relics",safe,true);v.RewardPanel=Panel("RewardChoice",safe,true);
            BuildBattle(v);BuildLobby(v);BuildRelics(v);BuildRewards(v);BuildModal(v);BuildPause(v);
            v.Feedback=Label("Feedback",safe,"",20,TextAnchor.MiddleCenter);Place(v.Feedback.rectTransform,new Vector2(.5f,0),new Vector2(0,28),new Vector2(640,42));v.Feedback.gameObject.SetActive(false);
            v.Only(v.BattlePanel);v.ModalPanel.SetActive(false);v.PausePanel.SetActive(false);return v;
        }
        static void BuildBattle(MvpView v)
        {
            var p=v.BattlePanel.transform;
            v.Stage=Label("Stage",p,"Stage 1",24);Place(v.Stage.rectTransform,new Vector2(0,1),new Vector2(78,-36),new Vector2(140,40));
            v.Pause=Button("Pause",p,"Ⅱ",false);Place((RectTransform)v.Pause.transform,new Vector2(1,1),new Vector2(-48,-48),new Vector2(72,72));
            v.BossHp=Meter("BossHP",p,new Vector2(.5f,1),new Vector2(30,-34),new Vector2(650,14),Cream);v.BossHpRoot=v.BossHp.transform.parent.gameObject;AnchorWidth((RectTransform)v.BossHpRoot.transform,new Vector2(0,1),new Vector2(1,1),new Vector2(160,-43),new Vector2(-104,-27));
            v.Stagger=Meter("BellMeter",p,new Vector2(.5f,1),new Vector2(0,-92),new Vector2(470,18),Gold);
            var bell=Symbol("Bell",p,UiSymbol.Bell,32);Place(bell.rectTransform,new Vector2(.5f,1),new Vector2(-260,-92),new Vector2(32,32));
            v.PlayerHp=Meter("PlayerHP",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(80,8),Cream);v.PlayerHpRoot=(RectTransform)v.PlayerHp.transform.parent;
            v.EnemyHp=Meter("EnemyHP",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(80,8),Gold);v.EnemyHpRoot=(RectTransform)v.EnemyHp.transform.parent;
            v.NextDanger=Graphic<DangerGraphic>("NextDanger",p);v.NextDangerRoot=v.NextDanger.rectTransform;Place(v.NextDangerRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(138,146));v.NextDanger.color=new Color(1,1,1,.4f);
            v.Danger=Graphic<DangerGraphic>("Danger",p);v.DangerRoot=v.Danger.rectTransform;Place(v.DangerRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(120,130));
            v.StunStars=new SymbolGraphic[3];for(int i=0;i<3;i++){v.StunStars[i]=Symbol("StunStar"+i,p,UiSymbol.Star,22);Place(v.StunStars[i].rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,22));v.StunStars[i].gameObject.SetActive(false);}
            Vector2[] positions={new Vector2(150,224),new Vector2(70,144),new Vector2(150,64),new Vector2(230,144)};
            v.Dodge=new PointerCommand[4];v.DodgeSymbols=new SymbolGraphic[4];
            for(int i=0;i<4;i++)
            {
                var key=Image("Dodge"+(DodgeDirection)i,p,Color.clear);Place(key.rectTransform,Vector2.zero,positions[i],new Vector2(72,72));key.raycastTarget=true;
                v.Dodge[i]=key.gameObject.AddComponent<PointerCommand>();v.DodgeSymbols[i]=Symbol("Arrow",key.transform,(UiSymbol)i,40);Place(v.DodgeSymbols[i].rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(40,40));
            }
            var attack=Image("Attack",p,Blue);Place(attack.rectTransform,new Vector2(1,0),new Vector2(-72,72),new Vector2(100,88));v.Attack=attack.gameObject.AddComponent<PointerCommand>();attack.raycastTarget=true;
            v.AttackSymbol=Symbol("Sword",attack.transform,UiSymbol.Sword,42);Place(v.AttackSymbol.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,42));
            v.Tutorial=Label("Tutorial",p,"▲를 눌러 피해!",26,TextAnchor.MiddleCenter);Place(v.Tutorial.rectTransform,new Vector2(.5f,1),new Vector2(0,-150),new Vector2(360,60));
            var effectRoot=Rect("BattleEffects",v.SafeRoot);Stretch(effectRoot);var fx=effectRoot.gameObject.AddComponent<BattleEffects>();fx.Root=effectRoot;
            fx.Numbers=new PixelNumber[8];for(int i=0;i<8;i++){var n=Graphic<PixelNumber>("Damage"+i,effectRoot);Place(n.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(150,35));fx.Numbers[i]=n;n.gameObject.SetActive(false);}
            fx.Fragments=new Image[32];for(int i=0;i<32;i++){var f=Image("Fragment"+i,effectRoot,Gold);f.raycastTarget=false;Place(f.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(i%2==0?5:8,i%2==0?5:8));fx.Fragments[i]=f;f.gameObject.SetActive(false);}
            effectRoot.SetAsFirstSibling();
        }
        static void BuildLobby(MvpView v)
        {
            var p=v.LobbyPanel.transform;var roster=Image("Roster",p,Blue);Area(roster.rectTransform,0,0,.25f,1,12);
            var title=Label("RosterTitle",roster.transform,"종지기",26);Place(title.rectTransform,new Vector2(0,1),new Vector2(82,-30),new Vector2(140,40));
            v.Characters=new Button[3];v.CharacterLabels=new Text[3];for(int i=0;i<3;i++){v.Characters[i]=Button("Character"+i,roster.transform,new[]{"온","담","린"}[i],i==0);AnchorWidth((RectTransform)v.Characters[i].transform,new Vector2(0,1),new Vector2(1,1),new Vector2(12,-154-i*100),new Vector2(-12,-70-i*100));v.CharacterLabels[i]=v.Characters[i].GetComponentInChildren<Text>();}
            v.Wallet=Label("Wallet",p,"골드 0 · 종 파편 0",21,TextAnchor.MiddleRight);Place(v.Wallet.rectTransform,new Vector2(1,1),new Vector2(-235,-30),new Vector2(440,40));
            var preview=Rect("PortraitPanel",p);Area(preview,.25f,.18f,.44f,.88f,8);
            v.LobbyPortrait=Image("IdlePortrait",preview,Color.white);v.LobbyPortrait.preserveAspect=true;v.LobbyPortrait.raycastTarget=false;Place(v.LobbyPortrait.rectTransform,new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(116,150));
            v.CharacterName=Label("CharacterName",preview,"온",26,TextAnchor.MiddleCenter);Place(v.CharacterName.rectTransform,new Vector2(.5f,.5f),new Vector2(0,-100),new Vector2(150,38));
            v.Cycle=Label("Cycle",preview,"NG+ 0",18,TextAnchor.MiddleCenter);Place(v.Cycle.rectTransform,new Vector2(.5f,1),new Vector2(0,-30),new Vector2(150,36));
            var stats=Rect("Stats",p);Area(stats,.44f,.19f,1,.88f,12);v.Stats=new StatRowBinding[4];
            for(int i=0;i<4;i++)
            {
                var row=Image("Stat"+i,stats,Blue);AnchorWidth(row.rectTransform,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-80-i*85),new Vector2(0,-i*85));
                var b=new StatRowBinding();b.Title=Label("Title",row.transform,new[]{"공격","체력","행운","공명"}[i],20);Place(b.Title.rectTransform,new Vector2(0,1),new Vector2(42,-16),new Vector2(68,28));
                b.Stars=new SymbolGraphic[5];for(int k=0;k<5;k++){b.Stars[k]=Symbol("Star"+k,row.transform,UiSymbol.Star,14);Place(b.Stars[k].rectTransform,new Vector2(0,1),new Vector2(86+k*17,-17),new Vector2(14,14));}
                b.Level=Label("Level",row.transform,"Lv.0 · 1단계",16,TextAnchor.MiddleRight);Place(b.Level.rectTransform,new Vector2(1,1),new Vector2(-142,-16),new Vector2(130,28));
                b.Value=Label("Value",row.transform,"",16);AnchorWidth(b.Value.rectTransform,new Vector2(0,1),new Vector2(1,1),new Vector2(10,-50),new Vector2(-82,-28));
                b.Bells=new SymbolGraphic[10];for(int k=0;k<10;k++){b.Bells[k]=Symbol("Bell"+k,row.transform,UiSymbol.Bell,17);Place(b.Bells[k].rectTransform,new Vector2(0,0),new Vector2(20+k*29,16),new Vector2(17,17));b.Bells[k].Filled=false;}
                b.Upgrade=Button("Upgrade",row.transform,"+",false);Place((RectTransform)b.Upgrade.transform,new Vector2(1,.5f),new Vector2(-42,0),new Vector2(72,72));v.Stats[i]=b;
            }
            v.OpenRelics=Button("Relics",p,"유물",false);AnchorWidth((RectTransform)v.OpenRelics.transform,new Vector2(.27f,0),new Vector2(.51f,0),new Vector2(4,12),new Vector2(-4,84));
            v.StartRun=Button("Start",p,"Stage 1",true);AnchorWidth((RectTransform)v.StartRun.transform,new Vector2(.53f,0),new Vector2(.985f,0),new Vector2(4,12),new Vector2(-4,84));v.StartLabel=v.StartRun.GetComponentInChildren<Text>();
        }
        static void BuildRelics(MvpView v)
        {
            var p=v.RelicPanel.transform;var title=Label("Title",p,"유물",28);Place(title.rectTransform,new Vector2(0,1),new Vector2(95,-30),new Vector2(160,44));
            v.RelicBack=Button("Back",p,"뒤로",false);Place((RectTransform)v.RelicBack.transform,new Vector2(1,1),new Vector2(-58,-42),new Vector2(92,72));
            var left=Image("Owned",p,Blue);Area(left.rectTransform,0,.02f,1f/3,.86f,12);
            v.EquippedSlots=Label("Slots",left.transform,"장착 0 / 1",22);AnchorWidth(v.EquippedSlots.rectTransform,new Vector2(0,1),new Vector2(1,1),new Vector2(12,-52),new Vector2(-12,-8));
            v.EquippedSlotIcons=new Image[3];for(int i=0;i<3;i++){var slot=Image("Slot"+i,left.transform,Line);Place(slot.rectTransform,new Vector2(0,1),new Vector2(32+48*i,-76),new Vector2(32,32));v.EquippedSlotIcons[i]=slot;}
            var scrollRoot=Rect("InventoryScroll",left.transform);AnchorWidth(scrollRoot,Vector2.zero,Vector2.one,new Vector2(8,8),new Vector2(-8,-106));
            var viewport=Image("Viewport",scrollRoot,Color.clear);Stretch(viewport.rectTransform);viewport.gameObject.AddComponent<RectMask2D>();viewport.raycastTarget=true;
            var content=Rect("Content",viewport.transform);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,780);
            var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            v.RelicItems=new Button[10];v.RelicItemLabels=new Text[10];for(int i=0;i<10;i++){var b=Button("Relic"+i,content,"유물",false);AnchorWidth((RectTransform)b.transform,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-72-i*78),new Vector2(0,-i*78));var label=b.GetComponentInChildren<Text>();label.fontSize=20;v.RelicItems[i]=b;v.RelicItemLabels[i]=label;}
            var right=Image("SelectedRelic",p,Navy);Area(right.rectTransform,1f/3,.02f,1,.86f,12);
            v.RelicName=Label("Name",right.transform,"유물",30);WideLine(v.RelicName,24,54);
            v.RelicLevel=Label("Level",right.transform,"미보유",24);WideLine(v.RelicLevel,84,44);
            v.RelicEffect=Label("Effect",right.transform,"",24);WideLine(v.RelicEffect,146,54);
            v.RelicNext=Label("Next",right.transform,"",21);WideLine(v.RelicNext,216,48);
            v.SlotPurchase=Button("BuySlot",right.transform,"슬롯",false);AnchorWidth((RectTransform)v.SlotPurchase.transform,new Vector2(.02f,0),new Vector2(.44f,0),new Vector2(4,12),new Vector2(-4,84));v.SlotPurchaseLabel=v.SlotPurchase.GetComponentInChildren<Text>();v.SlotPurchaseLabel.fontSize=20;
            v.RelicEquip=Button("Equip",right.transform,"장착",true);AnchorWidth((RectTransform)v.RelicEquip.transform,new Vector2(.46f,0),new Vector2(.98f,0),new Vector2(4,12),new Vector2(-4,84));v.RelicEquipLabel=v.RelicEquip.GetComponentInChildren<Text>();
        }
        static void BuildRewards(MvpView v)
        {
            var p=v.RewardPanel.transform;var title=Label("Title",p,"유물 하나를 선택하세요",28,TextAnchor.MiddleCenter);Place(title.rectTransform,new Vector2(.5f,1),new Vector2(0,-35),new Vector2(720,48));
            v.Rewards=new RewardCardBinding[3];for(int i=0;i<3;i++)
            {
                var panel=Image("Reward"+i,p,Blue);Area(panel.rectTransform,.02f+i*.325f,.14f,.33f+i*.325f,.86f,4);
                var b=new RewardCardBinding();b.Title=Label("Name",panel.transform,"유물",24);WideLine(b.Title,14,42);
                b.Level=Label("Level",panel.transform,"",20);WideLine(b.Level,65,38);
                b.Before=Label("Before",panel.transform,"",18);WideLine(b.Before,110,42);
                b.After=Label("After",panel.transform,"",18);WideLine(b.After,158,44);
                b.Equipment=Label("Equipment",panel.transform,"",17);WideLine(b.Equipment,210,44);
                b.Receive=Button("Receive",panel.transform,"받기",true);AnchorWidth((RectTransform)b.Receive.transform,new Vector2(.04f,0),new Vector2(.96f,0),new Vector2(4,14),new Vector2(-4,86));v.Rewards[i]=b;
            }
            v.RewardNote=Label("Note",p,"받으면 다음 적과 전투합니다.",22,TextAnchor.MiddleCenter);Place(v.RewardNote.rectTransform,new Vector2(.5f,0),new Vector2(0,35),new Vector2(760,44));
        }
        static void BuildModal(MvpView v)
        {
            v.ModalPanel=Panel("Modal",v.SafeRoot,false);var dim=Image("Scrim",v.ModalPanel.transform,new Color(0,0,0,.85f));Stretch(dim.rectTransform);dim.raycastTarget=true;
            var box=Image("Dialog",dim.transform,Navy);Place(box.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,400));
            v.ModalTitle=Label("Title",box.transform,"골드 교환",28,TextAnchor.MiddleCenter);WideLine(v.ModalTitle,18,45);
            v.ModalBody=Label("Body",box.transform,"",22,TextAnchor.UpperCenter);WideLine(v.ModalBody,88,210);v.ModalBody.lineSpacing=1.15f;
            v.ModalConfirm=Button("Confirm",box.transform,"교환",true);Place((RectTransform)v.ModalConfirm.transform,new Vector2(.25f,0),new Vector2(0,52),new Vector2(268,72));v.ModalConfirmLabel=v.ModalConfirm.GetComponentInChildren<Text>();
            v.ModalCancel=Button("Cancel",box.transform,"취소",false);Place((RectTransform)v.ModalCancel.transform,new Vector2(.75f,0),new Vector2(0,52),new Vector2(268,72));v.ModalCancelLabel=v.ModalCancel.GetComponentInChildren<Text>();
        }
        static void BuildPause(MvpView v)
        {
            v.PausePanel=Panel("PauseMenu",v.SafeRoot,false);var dim=Image("Scrim",v.PausePanel.transform,new Color(0,0,0,.85f));Stretch(dim.rectTransform);dim.raycastTarget=true;
            var box=Image("PauseDialog",dim.transform,Navy);Place(box.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(550,340));
            var title=Label("Title",box.transform,"잠시 멈춤",28,TextAnchor.MiddleCenter);WideLine(title,20,48);
            var toggleRoot=Rect("ReducedShake",box.transform);Place(toggleRoot,new Vector2(.5f,.5f),new Vector2(0,15),new Vector2(400,72));var toggleHit=toggleRoot.gameObject.AddComponent<Image>();toggleHit.color=Color.clear;toggleHit.raycastTarget=true;var toggle=toggleRoot.gameObject.AddComponent<Toggle>();
            var bg=Image("Box",toggleRoot,Blue);Place(bg.rectTransform,new Vector2(0,.5f),new Vector2(24,0),new Vector2(40,40));bg.raycastTarget=true;
            var check=Image("Check",bg.transform,Cream);Place(check.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,24));toggle.targetGraphic=bg;toggle.graphic=check;v.ReducedShake=toggle;
            var label=Label("Label",toggleRoot,"흔들림 줄이기",24);Place(label.rectTransform,new Vector2(.5f,.5f),new Vector2(30,0),new Vector2(310,48));
            v.Resume=Button("Resume",box.transform,"계속",true);Place((RectTransform)v.Resume.transform,new Vector2(.25f,0),new Vector2(0,64),new Vector2(220,72));
            v.ReturnHome=Button("Lobby",box.transform,"귀환",false);Place((RectTransform)v.ReturnHome.transform,new Vector2(.75f,0),new Vector2(0,64),new Vector2(220,72));
        }
        static RectTransform Rect(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));g.layer=5;g.transform.SetParent(parent,false);return (RectTransform)g.transform;}
        static GameObject Panel(string name,Transform parent,bool backdrop){var r=Rect(name,parent);Stretch(r);if(backdrop){var im=r.gameObject.AddComponent<Image>();im.sprite=white;im.color=Navy;im.raycastTarget=true;}return r.gameObject;}
        static Image Image(string name,Transform parent,Color color){var r=Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.sprite=white;image.color=color;image.raycastTarget=false;return image;}
        static T Graphic<T>(string name,Transform parent)where T:Graphic{var r=Rect(name,parent);var g=r.gameObject.AddComponent<T>();g.color=Cream;g.raycastTarget=false;return g;}
        static SymbolGraphic Symbol(string name,Transform parent,UiSymbol kind,float size){var s=Graphic<SymbolGraphic>(name,parent);s.Symbol=kind;s.rectTransform.sizeDelta=Vector2.one*size;return s;}
        static Text Label(string name,Transform parent,string text,int size,TextAnchor alignment=TextAnchor.MiddleLeft){var r=Rect(name,parent);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=Cream;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        static Button Button(string name,Transform parent,string text,bool primary){var image=Image(name,parent,primary?Cream:Blue);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;var label=Label("Label",b.transform,text,24,TextAnchor.MiddleCenter);Stretch(label.rectTransform);label.rectTransform.offsetMin=new Vector2(5,3);label.rectTransform.offsetMax=new Vector2(-5,-3);label.color=primary?Navy:Cream;return b;}
        static Image Meter(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color){var back=Image(name,parent,Blue);Place(back.rectTransform,anchor,pos,size);var fill=Image("Fill",back.transform,color);Stretch(fill.rectTransform);fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;return fill;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,Vector2 anchor,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;}
        static void Area(RectTransform r,float x0,float y0,float x1,float y1,float inset){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=new Vector2(inset,inset);r.offsetMax=new Vector2(-inset,-inset);}
        static void AnchorWidth(RectTransform r,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax){r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;}
        static void WideLine(Text t,float top,float height){AnchorWidth(t.rectTransform,new Vector2(0,1),new Vector2(1,1),new Vector2(16,-top-height),new Vector2(-16,-top));}
    }
}
