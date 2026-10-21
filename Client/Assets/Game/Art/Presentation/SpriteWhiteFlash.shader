Shader "LastBell/Presentation Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 1
        [PerRendererData] _Flash ("White hit flash", Range(0,1)) = 0
        [PerRendererData] _Dissolve ("Pixel dissolve", Range(0,1)) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment PresentationFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _Flash;
            float _Dissolve;
            float4 _MainTex_TexelSize;
            fixed4 PresentationFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                // Stable 4-source-pixel blocks: do not redraw the character or alter its palette.
                float2 block = floor(input.texcoord * _MainTex_TexelSize.zw / 4.0);
                float threshold = frac(sin(dot(block, float2(12.9898, 78.233))) * 43758.5453);
                if (_Dissolve > 0) clip(threshold - _Dissolve);
                color.rgb = lerp(color.rgb, fixed3(1,1,1), saturate(_Flash));
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
