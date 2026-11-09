// Sprite con saturacion regulable (_Saturacion: 0 = gris, 1 = colores originales).
// Lo usa el templo de Umbrae: desaturado y oscuro antes de la pelea (_Exposicion) y con sus colores cuando empieza.
Shader "Pomelo/Sprite Saturacion"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _Saturacion ("Saturacion", Range(0, 1)) = 1
        _Exposicion ("Exposicion (1 = normal, menos = mas oscuro)", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _Saturacion;
            float _Exposicion;
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.color = v.color * unity_SpriteColor; o.uv = v.uv; return o; }
            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                half gris = dot(c.rgb, half3(0.299, 0.587, 0.114));
                c.rgb = lerp(gris.xxx, c.rgb, _Saturacion) * _Exposicion;
                return c;
            }
            ENDHLSL
        }
    }
}
