using UnityEngine;
using UnityEngine.UI;

namespace LastBell
{
    // A saved graphic; decorative spikes/arrows never create damage events.
    public sealed class DangerGraphic : MaskableGraphic
    {
        public int Region;
        public float Progress;
        public bool NextPreview;
        public float CasterHeight=174;
        Vector2 playerFoot=new Vector2(0,-53),enemyFoot=new Vector2(300,-53);
        float remaining=.65f,impactAge=-1;
        const float S=.75f;
        public void SetLayout(Vector2 playerFootLocal,Vector2 enemyFootLocal){playerFoot=playerFootLocal;enemyFoot=enemyFootLocal;SetVerticesDirty();}
        public void Show(int region,float progress){Show(region,progress,(1-Mathf.Clamp01(progress))*.65f);}
        public void Show(int region,float progress,float secondsToHit){Region=region;Progress=Mathf.Clamp01(progress);remaining=Mathf.Max(0,secondsToHit);impactAge=-1;SetVerticesDirty();}
        public void ShowImpact(int region,float ageSeconds){Region=region;Progress=1;impactAge=ageSeconds;SetVerticesDirty();}
        public void Hide(){impactAge=-2;SetVerticesDirty();}
        Vector2 At(float x,float y){return playerFoot+new Vector2((x-500)*S,(590-y)*S);}
        void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c){PixelVfxMesh.Line(vh,a,b,width*S,PixelVfxMesh.Alpha(c,color.a));}
        void Dot(VertexHelper vh,Vector2 p,float size,Color c){PixelVfxMesh.Square(vh,p,size*S,PixelVfxMesh.Alpha(c,color.a));}
        void Rune(VertexHelper vh,Vector2 p,float progress)
        {
            float radius=(12+24*(1-progress))*S;
            Line(vh,p+new Vector2(-radius,3),p+new Vector2(-radius,-3),3,PixelVfxMesh.Gold);
            Line(vh,p+new Vector2(-radius,-3),p+new Vector2(-4,-3),3,PixelVfxMesh.Gold);
            Line(vh,p+new Vector2(radius,3),p+new Vector2(radius,-3),3,PixelVfxMesh.Gold);
            Line(vh,p+new Vector2(radius,-3),p+new Vector2(4,-3),3,PixelVfxMesh.Gold);
            Dot(vh,p,6,PixelVfxMesh.Cream);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(impactAge==-2||impactAge>=.14f)return;
            bool before=impactAge<0;float life=before?1:1-Mathf.Clamp01(impactAge/.14f);
            if(NextPreview)
            {
                Vector2 p=Region==0?At(500,450):Region==2?At(500,582):Region==1?At(410,580):At(575,525);
                float r=8*S;Line(vh,p+Vector2.left*r,p+Vector2.up*r,2,PixelVfxMesh.Old);Line(vh,p+Vector2.up*r,p+Vector2.right*r,2,PixelVfxMesh.Old);
                Line(vh,p+Vector2.right*r,p+Vector2.down*r,2,PixelVfxMesh.Old);Line(vh,p+Vector2.down*r,p+Vector2.left*r,2,PixelVfxMesh.Old);return;
            }
            if(before){Vector2 source=enemyFoot+new Vector2(-51,Mathf.Max(75,CasterHeight*.54f));Rune(vh,source,Progress);}
            if(Region==2)
            {
                float[] bases={464,506,548,590},heights={94,110,120,104};
                Vector2 last=At(430,588);
                for(int i=0;i<7;i++){Vector2 next=At(430+i*25,584+(i%2)*5);Line(vh,last,next,10,PixelVfxMesh.Dark);Line(vh,last,next,5,PixelVfxMesh.Gold);last=next;}
                for(int i=0;i<bases.Length;i++){if(before)Rune(vh,At(bases[i],584),Progress);else Spike(vh,bases[i],heights[i]*(.45f+.55f*Mathf.Clamp01(impactAge/.04f)),life);}
            }
            else if(Region==1)
            {
                float[] land={350,410,470};
                for(int i=0;i<land.Length;i++)
                {
                    Vector2 landing=At(land[i],590);
                    if(before)
                    {
                        Rune(vh,landing+Vector2.up*4,Progress);Line(vh,At(land[i]+38,522),At(land[i]+16,559),3,PixelVfxMesh.Old);
                        float q=Mathf.Clamp01(1-remaining/.18f);if(q>0){Vector2 tip=landing+new Vector2(400,780)*S*(1-q*q);GreatArrow(vh,tip,1);}
                    }
                    else{GreatArrow(vh,landing,life);PixelVfxMesh.Spark(vh,landing+Vector2.up*4,impactAge/.14f,18,life*color.a);}
                }
            }
            else if(Region==0)
            {
                if(before){Line(vh,At(430,461),At(620,459),10,PixelVfxMesh.Dark);Line(vh,At(430,461),At(620,459),4,PixelVfxMesh.Gold);Rune(vh,At(Mathf.Lerp(620,450,Progress),456),Progress);}
                else
                {
                    Vector2[] points={At(810,452),At(750,442),At(665,442),At(610,450),At(555,460),At(493,465),At(430,458)};
                    for(int i=0;i<points.Length-1;i++){Line(vh,points[i],points[i+1],25,PixelVfxMesh.Alpha(PixelVfxMesh.Dark,life));Line(vh,points[i],points[i+1],17,PixelVfxMesh.Alpha(PixelVfxMesh.Gold,life));Line(vh,points[i],points[i+1],5,PixelVfxMesh.Alpha(PixelVfxMesh.Cream,life));}
                }
            }
            else
            {
                if(before){Line(vh,enemyFoot+new Vector2(-60,49),At(585,525),3,PixelVfxMesh.Old);Rune(vh,At(Mathf.Lerp(790,565,Progress),525),Progress);}
                else
                {
                    Line(vh,At(820,525),At(555,525),20,PixelVfxMesh.Alpha(PixelVfxMesh.Dark,life));Line(vh,At(820,525),At(555,525),10,PixelVfxMesh.Alpha(PixelVfxMesh.Old,life));
                    for(int i=0;i<7;i++)Dot(vh,At(540+i*8,525-i*2),4+i*4,PixelVfxMesh.Alpha(i<2?PixelVfxMesh.Cream:PixelVfxMesh.Gold,life));
                    PixelVfxMesh.Spark(vh,At(545,525),impactAge/.14f,18,life*color.a);
                }
            }
        }
        void Spike(VertexHelper vh,float x,float height,float alpha)
        {
            for(int y=0;y<height;y+=4)
            {
                float f=y/Mathf.Max(1,height),width=Mathf.Max(3,38*(1-f)),cx=x-52*f*height/120;Vector2 p=At(cx,590-y);
                PixelVfxMesh.Rect(vh,new Rect(p.x-(width+6)*S*.5f,p.y-3*S,(width+6)*S,6*S),PixelVfxMesh.Alpha(PixelVfxMesh.Dark,alpha*color.a));
                PixelVfxMesh.Rect(vh,new Rect(p.x-width*S*.5f,p.y-2*S,width*S,4*S),PixelVfxMesh.Alpha(PixelVfxMesh.Gold,alpha*color.a));
                PixelVfxMesh.Rect(vh,new Rect(p.x-width*S*.28f,p.y-2*S,3*S,4*S),PixelVfxMesh.Alpha(PixelVfxMesh.Cream,alpha*color.a));
            }
        }
        void GreatArrow(VertexHelper vh,Vector2 tip,float alpha)
        {
            Vector2 dir=new Vector2(-400,-780).normalized,n=new Vector2(-dir.y,dir.x),tail=tip-dir*150;
            Line(vh,tail,tip-dir*15,16,PixelVfxMesh.Alpha(PixelVfxMesh.Dark,alpha));Line(vh,tail,tip-dir*15,10,PixelVfxMesh.Alpha(PixelVfxMesh.Old,alpha));Line(vh,tail,tip-dir*18,3,PixelVfxMesh.Alpha(PixelVfxMesh.Cream,alpha));
            for(int k=0;k<14;k++){float q=k/13f;Vector2 p=tip-dir*(q*40);float width=q*30;Line(vh,p-n*width*.5f,p+n*width*.5f,5,PixelVfxMesh.Alpha(PixelVfxMesh.Dark,alpha));Line(vh,p-n*Mathf.Max(0,width-5)*.5f,p+n*Mathf.Max(0,width-5)*.5f,3,PixelVfxMesh.Alpha(PixelVfxMesh.Gold,alpha));}
            foreach(int sign in new[]{-1,1})Line(vh,tail+n*sign*16.5f,tail+dir*21,8,PixelVfxMesh.Alpha(PixelVfxMesh.Gold,alpha));
        }
    }
}

