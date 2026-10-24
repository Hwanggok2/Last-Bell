Shader "LastBell/Gate Portcullis"
{
    Properties
    {
        [PerRendererData] _MainTex ("Gate sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 1
        [PerRendererData] _GateLift ("Portcullis raised", Range(0,1)) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        // The saved 320x440 gate is not atlased: these UV bounds are its doorway.
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment GateFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #include "UnitySprites.cginc"
            float _GateLift;
            fixed4 GateFrag(v2f input) : SV_Target
            {
                float2 uv=input.texcoord;
                fixed4 color=SampleSpriteTexture(uv);
                if(uv.x>=74.0/320.0&&uv.x<246.0/320.0&&uv.y<352.0/440.0)
                {
                    uv.y-=floor(saturate(_GateLift)*352.0)/440.0;
                    color=uv.y<0?fixed4(16.0/255.0,34.0/255.0,50.0/255.0,1):SampleSpriteTexture(uv);
                }
                color*=input.color;
                color.rgb*=color.a;
                return color;
            }
            ENDCG
        }
    }
}
