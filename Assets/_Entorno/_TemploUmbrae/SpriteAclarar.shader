// Modo de capa "Aclarar" (Lighten del Figma): de cada canal queda el mas claro entre el sprite y lo de atras.
// Lo usan los rayos de luz del fondo del salon de Umbrae (Group 209 del componente espacio_salon_roseton).
Shader "Pomelo/Sprite Aclarar"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        BlendOp Max
        Blend One One
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
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.color = v.color * unity_SpriteColor; o.uv = v.uv; return o; }
            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                // donde es transparente no aclara nada (max con 0)
                return half4(c.rgb * c.a, 0);
            }
            ENDHLSL
        }
    }
}
