// Relleno de la barra de vida de Umbrae (la "pista" exportada del Figma, en blanco, va como sprite).
// Se llena y se vacia de izquierda a derecha (se vacia de derecha a izquierda), segun la vida:
//  - hasta _Asentado: lleno (en el material: lavanda #9C90AC de "barra de vida umbrae")
//  - de _Asentado a _Relleno: liquido oscuro y viscoso que avanza ondulando (al principio y al curarse)
//  - de _Relleno a _Fantasma: el pedazo que se acaba de perder, lila claro con un destello blanco (_Destello)
//  - el resto: vacio (en el material: violeta oscuro #2A2238)
// _Rango: donde empieza y termina la barra dentro del PNG (u), porque el PNG tiene margen para el glow.
Shader "Pomelo/Sprite Barra Liquida"
{
    Properties
    {
        [MainTexture] _MainTex ("Pista (blanca)", 2D) = "white" {}
        _ColorLleno ("Lleno", Color) = (0.376, 0.067, 0.596, 1)
        _ColorVacio ("Vacio", Color) = (0.612, 0.569, 0.675, 1)
        _ColorLiquido ("Liquido viscoso", Color) = (0.10, 0.02, 0.16, 1)
        _ColorFantasma ("Lo que se perdio", Color) = (0.86, 0.78, 0.94, 1)
        _Rango ("Rango u de la barra (min, max)", Vector) = (0.1, 0.9, 0, 0)
        _Relleno ("Relleno (frente del liquido)", Range(0, 1)) = 1
        _Asentado ("Asentado", Range(0, 1)) = 1
        _Fantasma ("Fantasma", Range(0, 1)) = 1
        _Destello ("Destello", Range(0, 1)) = 0
        _Onda ("Onda del frente", Range(0, 0.05)) = 0.012
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorLleno, _ColorVacio, _ColorLiquido, _ColorFantasma;
                float4 _Rango;
                float _Relleno, _Asentado, _Fantasma, _Destello, _Onda;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.color = v.color; o.uv = v.uv; return o; }

            half4 frag (Varyings i) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * i.color.a;
                float x = saturate((i.uv.x - _Rango.x) / max(0.0001, _Rango.y - _Rango.x));
                float t = _Time.y;
                // el frente del liquido ondula (viscoso) solo mientras esta llenando
                float llenando = step(_Asentado + 0.002, _Relleno);
                float frente = _Relleno + llenando * (_Onda * sin(i.uv.y * 180.0 + t * 3.0) + _Onda * 0.6 * sin(i.uv.y * 110.0 - t * 2.1));
                half3 c;
                if (x < min(_Asentado, frente))
                {
                    c = _ColorLleno.rgb;
                }
                else if (x < frente)
                {
                    // liquido oscuro: mas espeso en el frente, con un brillo arriba
                    float k = saturate((x - _Asentado) / max(0.001, frente - _Asentado));
                    c = lerp(lerp(_ColorLleno.rgb, _ColorLiquido.rgb, 0.65), _ColorLiquido.rgb, k);
                    // (la barra ocupa v 0.41..0.59 del PNG: el brillo va en su parte de arriba)
                    c += 0.12 * smoothstep(0.54, 0.555, i.uv.y) * (1.0 - smoothstep(0.565, 0.58, i.uv.y));
                }
                else if (x < _Fantasma)
                {
                    c = lerp(_ColorFantasma.rgb, 1.0.xxx, _Destello);
                }
                else c = _ColorVacio.rgb;
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
