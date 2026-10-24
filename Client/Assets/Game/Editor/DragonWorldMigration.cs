using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastBell.Editor
{
    // Reuse the approved meadow image and its floor strip without repainting the source.
    public static class DragonWorldMigration
    {
        public const string Source="Assets/Game/Art/Dragon-v1/Background/meadow-pixel-v3.png";
        const float PlanWidth=1280f,SourceWidth=1672f,SourceHeight=941f;
        // Source pixels, top-left: grass fringe starts at 602; contact seam is 634.
        const float GrassTop=602f,SourceContactY=634f;
        static readonly string[] LayerNames={"DragonMeadowFar","DragonMeadowFloor"};

        public static void Import()
        {
            if(!File.Exists(Source))throw new FileNotFoundException("Approved meadow source is missing",Source);
            AssetDatabase.ImportAsset(Source,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Source);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit=SourceWidth/(PlanWidth*ParallaxWorld.UnitsPerPixel);
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
#pragma warning disable 0618
            importer.spritesheet=new[]{
                new SpriteMetaData{name="MeadowFar",rect=new Rect(0,0,SourceWidth,SourceHeight),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)},
                new SpriteMetaData{name="MeadowFloor",rect=new Rect(0,0,SourceWidth,SourceHeight-GrassTop),alignment=(int)SpriteAlignment.TopCenter,pivot=new Vector2(.5f,1)}
            };
#pragma warning restore 0618
            importer.SaveAndReimport();
        }
        static Sprite FindSprite(string name)
        {
            foreach(var a in AssetDatabase.LoadAllAssetsAtPath(Source))if(a is Sprite s&&s.name==name)return s;
            throw new InvalidDataException("Missing meadow slice: "+name);
        }
        public static void Configure(ParallaxWorld world)
        {
            if(!world)throw new InvalidDataException("Saved parallax world is missing");
            foreach(var layer in world.Layers)foreach(var tile in layer.Tiles)if(tile)tile.parent.gameObject.SetActive(false);
            var far=FindSprite("MeadowFar");var floor=FindSprite("MeadowFloor");
            float scale=PlanWidth/SourceWidth;
            float pictureTop=ParallaxWorld.GroundPlanY-SourceContactY*scale;
            var layers=new ParallaxLayer[2];
            for(int i=0;i<2;i++)
            {
                var parent=world.transform.Find(LayerNames[i]);
                if(!parent){var go=new GameObject(LayerNames[i]);parent=go.transform;parent.SetParent(world.transform,false);}
                parent.gameObject.SetActive(true);
                parent.localPosition=ParallaxWorld.PlanPoint(640,i==0?pictureTop+SourceHeight*scale*.5f:pictureTop+GrassTop*scale);
                parent.localScale=Vector3.one;
                var binding=new ParallaxLayer{Factor=i==0?.08f:1f,Width=PlanWidth*ParallaxWorld.UnitsPerPixel,MirrorAlternating=true,Tiles=new Transform[7]};
                for(int t=0;t<7;t++)
                {
                    var tile=parent.Find("Tile"+t);
                    if(!tile){var go=new GameObject("Tile"+t);tile=go.transform;tile.SetParent(parent,false);}
                    tile.localPosition=new Vector3((t-3)*binding.Width,0,0);tile.localScale=Vector3.one;
                    var sr=tile.GetComponent<SpriteRenderer>();
                    if(!sr)sr=tile.gameObject.AddComponent<SpriteRenderer>();
                    sr.sprite=i==0?far:floor;sr.sortingOrder=i==0?-50:-10;sr.color=Color.white;
                    // Mirror the two alternating tiles to match the generated image's edge pixels.
                    sr.flipX=t%2==0;binding.Tiles[t]=tile;
                }
                layers[i]=binding;
            }
            world.Layers=layers;world.DragonTheme=true;
            world.GroundTiles=Array.ConvertAll(layers[1].Tiles,t=>t.GetComponent<SpriteRenderer>());
            world.RefreshGround(1);
            if(world.Gate)world.Gate.gameObject.SetActive(false);
            world.ShowTravel(0,false);EditorUtility.SetDirty(world);
        }
        public static void ApplyPrefab()
        {
            Import();var root=PrefabUtility.LoadPrefabContents(ParallaxWorldBuilder.PrefabPath);
            try{Configure(root.GetComponent<ParallaxWorld>());PrefabUtility.SaveAsPrefabAsset(root,ParallaxWorldBuilder.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
