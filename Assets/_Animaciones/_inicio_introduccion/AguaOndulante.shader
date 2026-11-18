// Agua de la pantalla de inicio, que corre como un rio: rayitas de luz que viajan hacia la derecha (cada fila a
// su velocidad, mas rapido arriba que abajo), el dibujo del agua que se mece apenas de costado y unas bandas de
// brillo muy suaves. Para sprites, en el 2D de URP.
// _AnchoPx / _AltoPx = tamano en px de la imagen del agua (lo pone el menu que arma la escena).
Shader "Pomelo/AguaOndulante"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _AnchoPx ("Ancho de la imagen (px)", Float) = 3612
        _AltoPx ("Alto de la imagen (px)", Float) = 164
        [Header(Corriente)]
        _VelocidadRio ("Velocidad del rio (px por segundo)", Float) = 140
        _AltoFila ("Alto de cada fila de rayitas (px)", Float) = 9
        _LargoRaya ("Largo de las rayitas (px)", Float) = 110
        _CuantasRayas ("Cuantas rayitas (0 a 1)", Range(0, 1)) = 0.35
        _Rayas ("Brillo de las rayitas", Range(0, 1)) = 0.28
        _ColorRayas ("Color de las rayitas", Color) = (0.89, 0.925, 0.875, 1)
        [Header(Vaiven)]
        _Amplitud ("Cuanto se mece (px)", Float) = 4
        _Ondas ("Ondas por px de alto", Float) = 0.09
        _Velocidad ("Velocidad del vaiven", Float) = 1.1
        _Brillo ("Bandas de brillo", Range(0, 0.2)) = 0.04
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
                float _AnchoPx;
                float _AltoPx;
                float _VelocidadRio;
                float _AltoFila;
                float _LargoRaya;
                float _CuantasRayas;
                float _Rayas;
                half4 _ColorRayas;
                float _Amplitud;
                float _Ondas;
                float _Velocidad;
                float _Brillo;
            CBUFFER_END

            // reloj del agua: lo avanza PantallaInicio con el tiempo del juego, asi en pausa el agua se queda quieta
            // (el reloj propio de los shaders, _Time, sigue corriendo aunque el juego este en pausa)
            float _TiempoAgua;

            float Azar(float n) { return frac(sin(n * 127.1) * 43758.5453); }

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
                float t = _TiempoAgua;
                float xPx = i.uv.x * _AnchoPx;
                float yPx = i.uv.y * _AltoPx;

                // se mece de costado: dos ondas que no coinciden, asi no se ve repetido
                float tv = t * _Velocidad;
                float dx = sin(yPx * _Ondas + tv + xPx * 0.003) * _Amplitud
                         + sin(yPx * _Ondas * 2.3 - tv * 1.7 + xPx * 0.007) * _Amplitud * 0.5;
                float2 uv = i.uv;
                uv.x = clamp(uv.x + dx / _AnchoPx, 0.5 / _AnchoPx, 1.0 - 0.5 / _AnchoPx);
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * i.color;

                // corriente: filas de rayitas de luz que viajan hacia la derecha
                float fila = floor(yPx / _AltoFila);
                float azarFila = Azar(fila + 3.7);
                float arriba = saturate(i.uv.y);                               // 1 = la superficie, 0 = el fondo
                float vel = _VelocidadRio * (0.55 + 0.7 * azarFila) * (0.6 + 0.4 * arriba);
                float largo = _LargoRaya * (0.6 + 0.8 * Azar(fila + 11.3));
                float u = (xPx - t * vel) / largo + azarFila * 50.0;
                float celda = floor(u);
                float f = frac(u);
                float hay = step(Azar(celda * 0.731 + fila * 17.13), _CuantasRayas);
                float raya = smoothstep(0.0, 0.25, f) * smoothstep(0.75, 0.5, f);   // se afina en las puntas
                float dentroFila = frac(yPx / _AltoFila);
                float fina = 1.0 - smoothstep(0.08, 0.22, abs(dentroFila - 0.5));     // una linea finita por fila
                float brillo = hay * raya * fina * _Rayas;
                c.rgb = lerp(c.rgb, _ColorRayas.rgb, brillo);

                // bandas de luz que pasan despacio
                c.rgb *= 1.0 + _Brillo * sin(xPx * 0.012 - tv * 0.6 + yPx * 0.06);
                return c;
            }
            ENDHLSL
        }
    }
}
