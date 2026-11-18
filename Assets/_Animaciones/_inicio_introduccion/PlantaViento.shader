// Planta de la pantalla de inicio que se mece con una brisa: la base queda quieta y la punta va y viene (mas
// arriba, mas se mueve), con un temblorcito extra en las hojas. Para sprites, en el 2D de URP.
// Usa el reloj _TiempoAgua (lo avanza PantallaInicio con el tiempo del juego): en pausa se queda quieta.
Shader "Pomelo/PlantaViento"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Vaiven ("Cuanto se mece la punta (unidades)", Float) = 0.09
        _Velocidad ("Velocidad", Float) = 1.3
        _Temblor ("Temblor de las hojas", Range(0, 0.02)) = 0.006
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
                float _Vaiven;
                float _Velocidad;
                float _Temblor;
            CBUFFER_END

            float _TiempoAgua;

            Varyings vert(Attributes i)
            {
                Varyings o;
                float t = _TiempoAgua * _Velocidad;
                // brisa: una onda lenta y otra mas corta encima, asi no parece un pendulo
                float brisa = sin(t) * 0.75 + sin(t * 2.3 + 1.1) * 0.25;
                float3 p = i.positionOS;
                p.x += brisa * _Vaiven * i.uv.y * i.uv.y;   // la base (uv.y = 0) no se mueve
                o.positionCS = TransformObjectToHClip(p);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _TiempoAgua * _Velocidad;
                float2 uv = i.uv;
                uv.x += sin(t * 2.7 + uv.y * 9.0) * _Temblor * uv.y;   // las hojas tiemblan un poquito
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * i.color;
            }
            ENDHLSL
        }
    }
}
