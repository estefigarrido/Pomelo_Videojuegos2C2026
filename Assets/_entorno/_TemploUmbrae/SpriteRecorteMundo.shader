// Sprite que se esconde donde lo tapa algo fijo de la escena (columnas y pared del templo), usando una mascara
// en coordenadas del mundo: el sprite puede moverse o cambiar de tamano y el recorte queda quieto.
// _Recorte: textura con alfa = lo que tapa; _RectRecorte: (x, y, ancho, alto) del mundo que cubre.
Shader "Pomelo/Sprite Recorte Mundo"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _Recorte ("Mascara (alfa = lo que tapa)", 2D) = "black" {}
        _RectRecorte ("Rect del mundo (x, y, ancho, alto)", Vector) = (0, 0, 1, 1)
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
            TEXTURE2D(_Recorte); SAMPLER(sampler_Recorte);
            float4 _RectRecorte;
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 mundo : TEXCOORD1; };
            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(w);
                o.color = v.color; o.uv = v.uv; o.mundo = w.xy;
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float2 m = (i.mundo - _RectRecorte.xy) / _RectRecorte.zw;
                half tapa = (m.x >= 0 && m.x <= 1 && m.y >= 0 && m.y <= 1) ? SAMPLE_TEXTURE2D(_Recorte, sampler_Recorte, m).a : 0;
                c.a *= 1 - tapa;
                return c;
            }
            ENDHLSL
        }
    }
}
