// Copia del Sprite-Lit-Default de URP 2D (le pegan las luces igual) con dos agregados:
//
// 1) "Blob de color" (las plantas): traduccion del blurblob del Figma (vector gris #545454 en modo Hue con
//    blur 130). El modo Hue con un gris le saca la saturacion a lo que esta abajo y le deja la luminosidad,
//    asi que la planta se ve palida. Aca la planta arranca asi (_Gris = 1) y un blob de borde suave y forma
//    irregular (no es un circulo: el radio ondula con el angulo) crece desde donde paso la chica y le
//    devuelve el color. Por renderer: _Blob = (x, y, radio, borde) en el mundo, _BlobSemilla cambia la forma.
//
// 2) Sectores del templo: textura global _MascaraSectores (0 = roto, 1 = restaurado) que cubre el rect
//    _RectSectores (xMin, yMin, 1/ancho, 1/alto) del mundo. _ModoSector: 1 = se ve donde esta restaurado
//    (el mapa arreglado), -1 = se ve donde todavia no (el mapa roto), 0 = no le afecta. En el borde que
//    avanza se prende una estela de luz (_ColorEstela).
//
// 3) _Tinte / _TinteFuerza: tine el sprite conservando su luminosidad (el mapa roto va en violeta desaturado,
//    para combinar con las particulas violetas; el arreglado no lo usa).
Shader "Pomelo/Sprite Lit Restaurable"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _ModoSector("Modo sector (1 arreglado, -1 roto, 0 nada)", Float) = 0
        _ColorEstela("Color de la estela", Color) = (1, 0.93, 0.6, 1)
        _SatGris("Saturacion cuando esta palida", Range(0, 1)) = 0.3
        _AclararGris("Cuanto se aclara cuando esta palida", Range(0, 1)) = 0.12
        _Tinte("Tinte (mapa roto: violeta desaturado)", Color) = (0.55, 0.5, 0.65, 1)
        _TinteFuerza("Fuerza del tinte (0 = sin tinte)", Range(0, 1)) = 0
        [HideInInspector] _Gris("Gris", Range(0, 1)) = 0
        [HideInInspector] _Blob("Blob", Vector) = (0, 0, 0, 1)
        [HideInInspector] _BlobSemilla("Semilla", Float) = 0
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }



    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "RestaurableComun.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
                float2 mundo       : TEXCOORD6;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ColorEstela;
                float _ModoSector;
                float _SatGris;
                float _AclararGris;
                float _Gris;
                float4 _Blob;
                float _BlobSemilla;
                half4 _Tinte;
                float _TinteFuerza;
            CBUFFER_END

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.mundo = TransformObjectToWorld(input.positionOS).xy;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                main.rgb = Tenir(main.rgb, _Tinte, _TinteFuerza);
                main = Restaurar(main, input.mundo, _Gris, _Blob, _BlobSemilla, _ModoSector, _ColorEstela, _SatGris, _AclararGris);
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "RestaurableComun.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float2 mundo : TEXCOORD1;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ColorEstela;
                float _ModoSector;
                float _SatGris;
                float _AclararGris;
                float _Gris;
                float4 _Blob;
                float _BlobSemilla;
                half4 _Tinte;
                float _TinteFuerza;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.mundo = TransformObjectToWorld(input.positionOS).xy;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                half4 c = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                c.rgb = Tenir(c.rgb, _Tinte, _TinteFuerza);
                return Restaurar(c, input.mundo, _Gris, _Blob, _BlobSemilla, _ModoSector, _ColorEstela, _SatGris, _AclararGris);
            }
            ENDHLSL
        }
    }
}
