using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class StageBannerGraphic : MaskableGraphic
    {
        public const float Duration=.5f;
        int stage=1,columns;
        float age=-1;
        bool reduced;
        readonly List<Vector2Int> dots=new List<Vector2Int>();
        static readonly Dictionary<char,string[]> Glyphs=new Dictionary<char,string[]>
        {
            {'S',new[]{"01111","10000","10000","01110","00001","00001","11110"}},
            {'t',new[]{"0100","0100","1111","0100","0100","0100","0011"}},
            {'a',new[]{"00000","00000","01110","00001","01111","10001","01111"}},
            {'g',new[]{"00000","01111","10001","10001","01111","00001","01110"}},
            {'e',new[]{"00000","00000","01110","10001","11111","10000","01111"}},
            {' ',new[]{"000","000","000","000","000","000","000"}},
            {'0',new[]{"01110","10001","10011","10101","11001","10001","01110"}},
            {'1',new[]{"00100","01100","00100","00100","00100","00100","01110"}},
            {'2',new[]{"01110","10001","00001","00010","00100","01000","11111"}},
            {'3',new[]{"11110","00001","00001","01110","00001","00001","11110"}},
            {'4',new[]{"00010","00110","01010","10010","11111","00010","00010"}},
            {'5',new[]{"11111","10000","10000","11110","00001","00001","11110"}},
            {'6',new[]{"01110","10000","10000","11110","10001","10001","01110"}},
            {'7',new[]{"11111","00001","00010","00100","01000","01000","01000"}},
            {'8',new[]{"01110","10001","10001","01110","10001","10001","01110"}},
            {'9',new[]{"01110","10001","10001","01111","00001","00001","01110"}}
        };
        public void Show(int stageNumber,float ageSeconds,bool reducedMotion)
        {
            if(stage!=stageNumber||dots.Count==0){stage=Mathf.Clamp(stageNumber,1,999);BuildDots();}
            age=ageSeconds;reduced=reducedMotion;SetVerticesDirty();
        }
        public void Hide(){age=-1;SetVerticesDirty();}
        void BuildDots()
        {
            dots.Clear();columns=0;
            foreach(char c in "Stage "+stage){var rows=Glyphs[c];for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[y].Length;x++)if(rows[y][x]=='1')dots.Add(new Vector2Int(columns+x,y));columns+=rows[0].Length+1;}
            columns--;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(age<0||age>=Duration)return;if(dots.Count==0)BuildDots();
            var r=GetPixelAdjustedRect();const float unit=9;float left=r.center.x-columns*unit*.5f,top=r.yMax-101.25f;
            float fade=age<.4f?1:1-Mathf.Clamp01((age-.4f)/.1f);
            for(int i=0;i<dots.Count;i++)
            {
                float delay=(i*47%101)/1000f;if(!reduced&&age<delay)continue;
                float q=reduced?1:Mathf.Clamp01((age-delay)/.18f),e=1-Mathf.Pow(1-q,3);
                float dx=(i*59%17-8)*13.5f,dy=(i*31%11-5)*10.5f;
                var p=new Vector2(left+dots[i].x*unit+dx*(1-e),top-dots[i].y*unit+dy*(1-e));
                PixelVfxMesh.Rect(vh,new Rect(p.x-1.5f,p.y-unit-1.5f,unit+2.25f,unit+2.25f),PixelVfxMesh.Alpha(PixelVfxMesh.Dark,fade));
                PixelVfxMesh.Rect(vh,new Rect(p.x,p.y-unit,unit-.75f,unit-.75f),PixelVfxMesh.Alpha(PixelVfxMesh.Cream,fade));
            }
        }
    }
}
