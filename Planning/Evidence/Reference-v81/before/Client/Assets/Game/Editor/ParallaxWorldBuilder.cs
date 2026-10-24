using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastBell.Editor
{
    public static class ParallaxWorldBuilder
    {
        public const string PrefabPath="Assets/Game/Prefabs/PresentationWorld.prefab";
        const string Folder="Assets/Game/Art/Presentation/Background";
        const int PixelStep=2,PlanHeight=720;
        static readonly string[] Names={"sky","far","mid","near","floor"};
        static readonly int[] Widths={1280,1440,1520,1320,1344};
        static readonly float[] Factors={.02f,.08f,.22f,.48f,1};

        // New assets only. Existing textures/prefab remain designer-owned.
        public static GameObject EnsurePrefab()
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);if(existing)return existing;
            Directory.CreateDirectory(Folder);Directory.CreateDirectory("Assets/Game/Prefabs");AssetDatabase.Refresh();
            var root=new GameObject("PresentationWorld");var world=root.AddComponent<ParallaxWorld>();world.Layers=new ParallaxLayer[5];
            try
            {
                for(int i=0;i<Names.Length;i++)
                {
                    Sprite sprite=EnsureSprite(i);float width=Widths[i]*ParallaxWorld.UnitsPerPixel;
                    var layer=new GameObject(Names[i]);layer.transform.SetParent(root.transform,false);
                    var binding=new ParallaxLayer{Factor=Factors[i],Width=width,Tiles=new Transform[5]};world.Layers[i]=binding;
                    for(int tile=0;tile<5;tile++)
                    {
                        var go=new GameObject("Tile"+tile);go.transform.SetParent(layer.transform,false);go.transform.localPosition=new Vector3((tile-2)*width,0,0);
                        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=-50+i*10;binding.Tiles[tile]=go.transform;
                    }
                }
                return PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{Object.DestroyImmediate(root);}
        }

        static Sprite EnsureSprite(int layer)
        {
            string path=Folder+"/gate-"+Names[layer]+".png";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing)return existing;
            if(!File.Exists(path))
            {
                var pixels=new Color32[Widths[layer]/PixelStep*(PlanHeight/PixelStep)];
                Paint(layer,pixels,Widths[layer]);
                var texture=new Texture2D(Widths[layer]/PixelStep,PlanHeight/PixelStep,TextureFormat.RGBA32,false);
                texture.SetPixels32(pixels);texture.Apply(false,false);File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.spritePixelsPerUnit=1/(ParallaxWorld.UnitsPerPixel*PixelStep);importer.maxTextureSize=1024;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;
            // The 1280-wide planning viewport's x640/y360 is world origin, even for wider repeat tiles.
            settings.spritePivot=new Vector2(640f/Widths[layer],.5f);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Paint(int layer,Color32[] pixels,int width)
        {
            Color32 sky=Hex(0x233d60),far=Hex(0x1d3452),mid=Hex(0x14293f),near=Hex(0x0b1b2c),floor=Hex(0x091523);
            if(layer==0)
            {
                Fill(pixels,width,0,0,width,720,sky);
                Fill(pixels,width,1040,90,36,56,Hex(0xd8ac55));Fill(pixels,width,1054,82,36,56,sky);
            }
            else if(layer==1)
            {
                for(int i=0;i<8;i++)
                {
                    int x=i*180,top=270+(i*53%110);Fill(pixels,width,x,top,90,600-top,far);
                    Fill(pixels,width,x+25,top-45,40,45,far);Fill(pixels,width,x+38,top-75,14,35,far);
                    if(i%3==1){Fill(pixels,width,x+34,top+52,20,28,Hex(0x101f32));Fill(pixels,width,x+42,top+52,4,28,far);}
                }
            }
            else if(layer==2)
            {
                for(int i=0;i<4;i++)
                {
                    int x=i*380;Arch(pixels,width,x,180,280,420,mid);
                    if(i==1)
                    {
                        // A shut inner gate and a small horizontal latch foreshadow the opening story.
                        Fill(pixels,width,x+56,356,168,244,Hex(0x102238));
                        for(int bar=0;bar<5;bar++)Fill(pixels,width,x+64+bar*32,360,8,240,mid);
                        Fill(pixels,width,x+82,466,118,12,Hex(0x273746));Fill(pixels,width,x+184,458,12,28,Hex(0x33414b));
                    }
                }
            }
            else if(layer==3)
            {
                for(int i=0;i<2;i++)
                {
                    int x=i*660+60;Fill(pixels,width,x,150,44,450,near);Fill(pixels,width,x-12,132,68,18,near);
                    Fill(pixels,width,x+6,40,32,92,near);
                    if(i==1)
                    {
                        int lx=x+52;Fill(pixels,width,lx,260,4,42,Hex(0x614e32));Fill(pixels,width,lx-10,300,24,8,Hex(0x846536));
                        Fill(pixels,width,lx-8,308,20,28,Hex(0x352d24));Fill(pixels,width,lx-4,312,12,20,Hex(0xb38335));
                        Fill(pixels,width,lx-8,336,20,4,Hex(0x846536));
                    }
                }
            }
            else
            {
                Fill(pixels,width,0,600,width,120,floor);
                for(int i=0;i<24;i++){int x=i*56;Fill(pixels,width,x,590,50,12,Hex(0x3f5775));Fill(pixels,width,x+4,604,44,20,Hex(0x122238));}
            }
        }
        static void Arch(Color32[] p,int width,int x,int y,int w,int h,Color32 c)
        {
            Fill(p,width,x,y+40,20,h-40,c);Fill(p,width,x+w-20,y+40,20,h-40,c);Fill(p,width,x+20,y+20,w-40,20,c);
            Fill(p,width,x+40,y,w-80,20,c);Fill(p,width,x,y+35,40,20,c);Fill(p,width,x+w-40,y+35,40,20,c);
        }
        static Color32 Hex(int c){return new Color32((byte)(c>>16),(byte)(c>>8),(byte)c,255);}
        static void Fill(Color32[] p,int width,int x,int y,int w,int h,Color32 c)
        {
            int tw=width/PixelStep,th=PlanHeight/PixelStep;
            int left=Mathf.Clamp(Mathf.RoundToInt(x/(float)PixelStep),0,tw),right=Mathf.Clamp(Mathf.RoundToInt((x+w)/(float)PixelStep),0,tw);
            int top=Mathf.Clamp(Mathf.RoundToInt(y/(float)PixelStep),0,th),bottom=Mathf.Clamp(Mathf.RoundToInt((y+h)/(float)PixelStep),0,th);
            for(int row=top;row<bottom;row++)for(int col=left;col<right;col++)p[(th-1-row)*tw+col]=c;
        }
    }
}
