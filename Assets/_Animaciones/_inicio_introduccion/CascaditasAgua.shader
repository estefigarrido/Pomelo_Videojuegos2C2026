// Las tiras de agua de las plataformas de la derecha (parte 2 de la pantalla de inicio) corren para abajo:
// sobre lo claro del dibujo (las tiras) pasan bandas de luz y sombra que bajan, como agua cayendo.
// Lo oscuro (las plataformas) no cambia. Para sprites, en el 2D de URP.
// Usa el reloj _TiempoAgua (lo avanza PantallaInicio con el tiempo del juego): en pausa se queda quieta.
Shader "Pomelo/CascaditasAgua"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _AltoPx ("Alto de la imagen (px)", Float) = 283
        _Velocidad ("Velocidad de caida (px por segundo)", Float) = 120
        _LargoBanda ("Largo de cada chorrito (px)", Float) = 26
        _Contraste ("Cuanto se notan", Range(0, 1)) = 0.35
        _Umbral ("Desde que tan claro es agua", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
               "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _AltoPx;
                float _Velocidad;
                float _LargoBanda;
                float _Contraste;
                float _Umbral;
            CBUFFER_END

            float _TiempoAgua;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float claro = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float agua = smoothstep(_Umbral - 0.06, _Umbral + 0.06, claro);   // 1 en las tiras, 0 en lo demas

                // uv.y crece para arriba: sumarle el tiempo hace que las bandas bajen
                float yPx = i.uv.y * _AltoPx;
                float x = i.uv.x * 897.0;
                float u = (yPx + _TiempoAgua * _Velocidad) / _LargoBanda + sin(x * 0.35) * 0.6;   // cada tira desfasada
                float banda = sin(u * 6.2831853) * 0.5 + 0.5;
                banda = banda * banda;                                                            // chorritos finos
                c.rgb *= 1.0 + agua * _Contraste * (banda - 0.45);
                return c;
            }
            ENDHLSL
        }
    }
}
