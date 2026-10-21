using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastBell.Editor
{
    // Explicit import command: no automatic rebuild on reload or Play.
    public static class MvpArtImporter
    {
        public const string CatalogPath = "Assets/Game/Art/MvpArtCatalog.asset";
        private const string Source = "Assets/Game/Art/Source/";

        [MenuItem("Last Bell/Art/Create Missing Catalog")]
        public static void CreateMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath) != null) return;
            ImportPrototypeSheets();
        }

        [MenuItem("Last Bell/Art/Import Prototype Sheets")]
        public static void ImportPrototypeSheets()
        {
            var player = ImportSheet(Source + "on-poses-v1.png", 4, 2, "on");
            var enemies = ImportSheet(Source + "gate-enemies-v1.png", 5, 3, "gate");
            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath);
            bool create = catalog == null;
            if (create) catalog = ScriptableObject.CreateInstance<ArtCatalog>();
            var ids = new[] { "gate_sentinel", "chain_thrall", "ash_wisp", "gate_warden", "bell_knight" };
            var entries = new ActorSprites[6];
            entries[0] = new ActorSprites {
                id="hero_on", idle=new[]{player[0],player[1]}, attack=new[]{player[2],player[3]},
                stun=new[]{player[5]}, dodgeUp=new[]{player[4]}, dodgeDown=new[]{player[5]},
                dodgeBack=new[]{player[6]}, dodgeForward=new[]{player[7]}, facesRight=true, height=1.1f
            };
            for (int i=0;i<5;i++) entries[i+1] = new ActorSprites {
                id=ids[i], idle=new[]{enemies[i]}, attack=new[]{enemies[5+i]},
                stun=new[]{enemies[10+i]}, facesRight=false,
                height=i==4 ? 2.1f : i==3 ? 1.8f : i==2 ? 1f : 1.5f
            };
            catalog.actors=entries;
            catalog.gateBackground=ImportBackground(Source+"gate-background-v1.png");
            catalog.uiFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NanumGothic-Regular.ttf");
            if (create) AssetDatabase.CreateAsset(catalog,CatalogPath);
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Debug.Log("LAST_BELL_ART_IMPORTED: 8 player poses, 5 enemies x 3 poses; editable sprite metadata and catalog.");
        }

        private static Sprite[] ImportSheet(string path,int columns,int rows,string prefix)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Prototype art is not present yet",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.filterMode=FilterMode.Point;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=4096; importer.isReadable=true;
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var pixels=texture.GetPixels32();
            bool Solid(int x,int y) => pixels[y*texture.width+x].a>128;
            // Generation trims transparent outer padding. Detect empty gutters instead of assuming equal canvas cells.
            var rowOccupied=new bool[texture.height];
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(Solid(x,y)){rowOccupied[y]=true;break;}
            var rowCuts=SplitGutters(rowOccupied,rows,path);
            var metadata=new SpriteMetaData[columns*rows];
            for (int row=0;row<rows;row++)
            {
                int y0=rowCuts[rows-row-1],height=rowCuts[rows-row]-y0;
                var columnOccupied=new bool[texture.width];
                for(int x=0;x<texture.width;x++)for(int y=y0;y<y0+height;y++)if(Solid(x,y)){columnOccupied[x]=true;break;}
                var columnCuts=SplitGutters(columnOccupied,columns,path+" row "+row);
                for (int col=0;col<columns;col++) {
                int n=row*columns+col;
                // Slice, never redraw or overwrite the generated PNG. Tight bounds remove atlas padding.
                int x0=columnCuts[col],width=columnCuts[col+1]-x0;
                int minX=width,minY=height,maxX=-1,maxY=-1;
                for (int y=0;y<height;y++) for (int x=0;x<width;x++)
                    if (Solid(x0+x,y0+y)) {
                        minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
                    }
                if (maxX<minX) throw new InvalidDataException($"Empty cell {n}: {path}");
                int footLeft=maxX,footRight=minX;
                for(int y=minY;y<=Math.Min(maxY,minY+6);y++)for(int x=minX;x<=maxX;x++)
                    if(Solid(x0+x,y0+y)){footLeft=Math.Min(footLeft,x);footRight=Math.Max(footRight,x);}
                metadata[n]=new SpriteMetaData {
                    name=$"{prefix}_{n:00}",rect=new Rect(x0+minX,y0+minY,maxX-minX+1,maxY-minY+1),
                    alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(((footLeft+footRight)*.5f-minX)/(maxX-minX+1),0)
                };
                }
            }
            // Keep a constant pixel/world ratio across poses, so a crouch does not expand to idle height.
            importer.spritePixelsPerUnit=100;
#pragma warning disable CS0618
            importer.spritesheet=metadata;
#pragma warning restore CS0618
            importer.isReadable=false;importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(x=>x.name).ToArray();
            if(sprites.Length!=columns*rows)throw new InvalidDataException("Wrong sprite count: "+path);
            return sprites;
        }

        private static int[] SplitGutters(bool[] occupied,int pieces,string label)
        {
            int first=Array.FindIndex(occupied,x=>x),last=Array.FindLastIndex(occupied,x=>x);
            if(first<0)throw new InvalidDataException("Transparent sheet: "+label);
            var gaps=new System.Collections.Generic.List<Vector2Int>();
            for(int i=first;i<=last;i++)if(!occupied[i]){
                int start=i;while(i<=last&&!occupied[i])i++;
                gaps.Add(new Vector2Int(start,i-start));
            }
            var chosen=gaps.OrderByDescending(x=>x.y).Take(pieces-1).Select(x=>x.x+x.y/2).OrderBy(x=>x).ToArray();
            if(chosen.Length!=pieces-1)throw new InvalidDataException("Missing atlas gutters: "+label);
            return new[]{first}.Concat(chosen).Concat(new[]{last+1}).ToArray();
        }

        private static Sprite ImportBackground(string path)
        {
            if(!File.Exists(path))throw new FileNotFoundException("Background missing",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
