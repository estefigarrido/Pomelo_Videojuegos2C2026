// Particulas de ambiente que siguen la restauracion del templo.
// Leen la misma mascara de sectores que los fondos (_MascaraSectores / _RectSectores, de RestauracionTemplo):
//  - donde el templo esta roto: violeta (_ColorRoto), con opacidad _AlfaRoto
//  - donde ya se arreglo: su verde original (el de la textura), con opacidad _AlfaArreglado
// La mascara tiene borde suave, asi que al pasar de una zona a otra el color y la opacidad cambian de a poco.
// Sin RestauracionTemplo en la escena (o en el editor) todo se ve roto: violeta.
Shader "Pomelo/Particulas Restaurables"
{
    Properties
    {
        [MainTexture] _MainTex ("Textura", 2D) = "white" {}
        _ColorRoto ("Color en lo roto (violeta)", Color) = (0.69, 0.47, 1.0, 1)
        _AlfaRoto ("Opacidad en lo roto", Range(0, 1.5)) = 0.75
        _AlfaArreglado ("Opacidad en lo arreglado", Range(0, 1.5)) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
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
            #include "RestaurableComun.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _ColorRoto;
                half _AlfaRoto, _AlfaArreglado;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 mundo : TEXCOORD1; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(w);
                o.mundo = w.xy;
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float m = Sector(i.mundo).x;                       // 0 roto, 1 arreglado
                // violeta: el mismo brillo del punto verde, en el tono violeta
                half brillo = max(t.r, max(t.g, t.b));
                half3 violeta = _ColorRoto.rgb * saturate(brillo / 0.8);
                half3 rgb = lerp(violeta, t.rgb, m);
                half a = t.a * lerp(_AlfaRoto, _AlfaArreglado, m);
                return half4(rgb, a) * i.color;
            }
            ENDHLSL
        }
    }
}
