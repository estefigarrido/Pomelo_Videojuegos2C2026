// Sprite en modo "Multiplicar" (como el blend mode Multiply de Figma): oscurece lo que tiene abajo.
// Lo transparente del sprite no cambia nada (multiplica por blanco).
Shader "Pomelo/Sprite Multiplicar"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _BordeX ("Borde difuminado a los costados (fraccion del ancho)", Range(0, 0.5)) = 0.2
        _BordeY ("Borde difuminado arriba y abajo (fraccion del alto)", Range(0, 0.5)) = 0.12
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Blend DstColor Zero
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
            CBUFFER_START(UnityPerMaterial) float _BordeX; float _BordeY; CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color * unity_SpriteColor;   // el color/opacidad del SpriteRenderer llega por unity_SpriteColor
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                // bordes difuminados: el gradiente se apaga hacia los cuatro lados y blendea con el fondo (sin linea recta)
                float2 uv = i.uv;
                c.a *= smoothstep(0.0, max(_BordeX, 0.0001), uv.x) * smoothstep(0.0, max(_BordeX, 0.0001), 1.0 - uv.x);
                c.a *= smoothstep(0.0, max(_BordeY, 0.0001), uv.y) * smoothstep(0.0, max(_BordeY, 0.0001), 1.0 - uv.y);
                // con la opacidad (del sprite y del color) se mezcla hacia blanco: 0 % = no oscurece nada
                return half4(lerp(half3(1, 1, 1), c.rgb, c.a), 1);
            }
            ENDHLSL
        }
    }
}
