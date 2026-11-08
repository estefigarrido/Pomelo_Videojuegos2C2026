// Brillo alrededor de un sprite (su silueta desenfocada) pintado con un degradado de izquierda a derecha.
// Lo usa Umbrae en el ataque de rayos: degradado del Figma 8B5CC7 42 % -> negro 0 % -> B48CF0 42 %.
// Con un radio chico sirve tambien de silueta de color (la estela de los brazos).
// No recibe luces y se dibuja detras del sprite.
Shader "Pomelo/Sprite Glow Gradiente"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _ColorIzq ("Color izquierda (0 %)", Color) = (0.545, 0.361, 0.780, 0.42)
        _ColorCentro ("Color centro (50 %)", Color) = (0, 0, 0, 0)
        _ColorDer ("Color derecha (100 %)", Color) = (0.706, 0.549, 0.941, 0.42)
        _Radio ("Radio del desenfoque (fraccion de la imagen)", Range(0, 0.1)) = 0.025
        _Fuerza ("Fuerza (cuanto se llena la silueta)", Range(0.5, 4)) = 2
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
            half4 _ColorIzq, _ColorCentro, _ColorDer;
            float _Radio, _Fuerza;

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // silueta desenfocada: el centro y 3 anillos de 12 muestras
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                float peso = 1;
                [unroll] for (int anillo = 1; anillo <= 3; anillo++)
                {
                    float r = _Radio * anillo / 3.0;
                    float w = 1.0 - (anillo - 1) / 3.0;
                    [unroll] for (int k = 0; k < 12; k++)
                    {
                        float ang = (k + anillo * 0.5) * 0.5235988;
                        a += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(cos(ang), sin(ang)) * r).a * w;
                        peso += w;
                    }
                }
                a = saturate(a / peso * _Fuerza);

                // degradado de izquierda a derecha con la parada del medio al 50 %
                float u = saturate(i.uv.x);
                half4 c = u < 0.5 ? lerp(_ColorIzq, _ColorCentro, u * 2) : lerp(_ColorCentro, _ColorDer, (u - 0.5) * 2);
                return half4(c.rgb, c.a * a * i.color.a);
            }
            ENDHLSL
        }
    }
}
