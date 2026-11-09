// Las dos drop shadows del Figma de Umbrae ("umbrae_quieto 1"), dibujadas detras de el:
//  A) lila (219,193,255) 100 %, blur 187.5, offset y -1.5
//  B) violeta (80,30,146) 100 %, blur 37.5, offset y 6
// Va en una copia de Umbrae (UmbraeSombras) que sigue su dibujo. El quad se agranda '_Margen' texeles
// para que el resplandor no se corte en el borde del PNG (los sprites tienen que ser FullRect).
// El desenfoque sale de los mipmaps del alfa.
Shader "Pomelo/Sprite Sombra Exterior"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _PPU ("Pixeles por unidad del sprite", Float) = 100
        _Margen ("Margen (texeles)", Float) = 180
        _EscalaFigma ("Px del Figma por texel", Float) = 1.5
        _SombraA ("Sombra A", Color) = (0.859, 0.757, 1, 1)
        _SombraABlur ("Sombra A blur", Float) = 187.5
        _SombraAOffsetY ("Sombra A offset y", Float) = -1.5
        _SombraB ("Sombra B", Color) = (0.314, 0.118, 0.573, 1)
        _SombraBBlur ("Sombra B blur", Float) = 37.5
        _SombraBOffsetY ("Sombra B offset y", Float) = 6
        _Alfa ("Alfa (lo pone el script)", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                float _PPU, _Margen, _EscalaFigma, _SombraABlur, _SombraAOffsetY, _SombraBBlur, _SombraBOffsetY, _Alfa;
                half4 _SombraA, _SombraB;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                // cada esquina se corre hacia afuera 'margen' texeles (y su uv tambien)
                float2 dir = sign(v.uv - 0.5);
                float3 p = v.positionOS.xyz;
                p.xy += dir * _Margen / _PPU;
                o.positionCS = TransformObjectToHClip(p);
                o.uv = v.uv + dir * _Margen * _MainTex_TexelSize.xy;
                return o;
            }

            float AlfaBlur(float2 uv, float radio, float corridaY)
            {
                float r = max(1.0, radio / _EscalaFigma);
                float nivel = log2(r);
                float2 t = _MainTex_TexelSize.xy;
                float2 c = uv + float2(0, corridaY / _EscalaFigma) * t;
                float2 d = t * r * 0.35;
                float a = 0;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 o2 = float2((i & 1) ? d.x : -d.x, (i & 2) ? d.y : -d.y);
                    float2 q = c + o2;
                    // fuera del PNG el desenfoque se esfuma de a poco (si se corta en seco se ve el recuadro)
                    float2 fuera = max(0, max(-q, q - 1)) / t;
                    float dentro = saturate(1 - length(fuera) / max(1.0, r));
                    a += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, saturate(q), nivel).a * dentro;
                }
                return a * 0.25;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // offset para abajo en el Figma = la sombra baja: se mira el alfa de arriba
                float aA = AlfaBlur(i.uv, _SombraABlur, _SombraAOffsetY) * _SombraA.a;
                float aB = AlfaBlur(i.uv, _SombraBBlur, _SombraBOffsetY) * _SombraB.a;
                // B encima de A (premultiplicado)
                half3 c = _SombraB.rgb * aB + _SombraA.rgb * aA * (1 - aB);
                half a = aB + aA * (1 - aB);
                return half4(c, a) * _Alfa;
            }
            ENDHLSL
        }
    }
}
