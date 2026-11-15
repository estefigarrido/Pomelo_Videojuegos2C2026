// Umbrae con las sombras internas del Figma ("umbrae_quieto 1", pagina mAPA V2), con las luces 2D:
//  1) violeta muy oscuro (32,0,64) 100 %, blur 102.9, offset y 6
//  2) blanco 25 %, blur 31.5, offset y 6
//  3) rosa (255,142,253) 45 %, blur 6, offset y 6
// Una sombra interna es la forma invertida, corrida y desenfocada, dibujada solo adentro del dibujo.
// Ademas sube la exposicion (_Exposicion = 1.3) para que Umbrae contraste con el fondo del templo.
// El desenfoque se saca de los mipmaps del alfa (los PNG de Umbrae tienen mipmaps), en texeles del PNG:
// el Figma esta a 1.5 px por texel, por eso _EscalaFigma = 1.5.
Shader "Pomelo/Sprite Lit Sombras Internas"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _EscalaFigma("Px del Figma por texel", Float) = 1.5
        _Sombra1("Sombra 1 (color)", Color) = (0.125, 0, 0.251, 1)
        _Sombra1Blur("Sombra 1 blur (px Figma)", Float) = 102.9
        _Sombra2("Sombra 2 (color)", Color) = (1, 1, 1, 0.25)
        _Sombra2Blur("Sombra 2 blur", Float) = 31.5
        _Sombra3("Sombra 3 (color)", Color) = (1, 0.557, 0.992, 0.45)
        _Sombra3Blur("Sombra 3 blur", Float) = 6
        _OffsetY("Offset y (px Figma, para abajo)", Float) = 6
        _Exposicion("Exposicion (1.3 = 30 % mas, para que contraste con el fondo)", Float) = 1.3
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
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_TexelSize;
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Sombra1, _Sombra2, _Sombra3;
                float _Sombra1Blur, _Sombra2Blur, _Sombra3Blur, _OffsetY, _EscalaFigma, _Exposicion;
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
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            // alfa desenfocado (radio en px del Figma), corrido 'corrida' texeles; 4 muestras para suavizar
            float AlfaBlur(float2 uv, float radio, float2 corrida)
            {
                float r = max(1.0, radio / _EscalaFigma);
                float nivel = log2(r);
                float2 t = _MainTex_TexelSize.xy;
                float2 d = t * r * 0.35;
                float2 c = uv + corrida * t;
                float a = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, c + float2( d.x,  d.y), nivel).a
                        + SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, c + float2(-d.x,  d.y), nivel).a
                        + SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, c + float2( d.x, -d.y), nivel).a
                        + SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, c + float2(-d.x, -d.y), nivel).a;
                return a * 0.25;
            }

            half3 Interna(half3 c, float2 uv, half4 col, float blur)
            {
                // offset para abajo: la sombra se ve en el borde de arriba (se mira el alfa de arriba)
                float2 corrida = float2(0, _OffsetY / _EscalaFigma);
                float k = (1.0 - AlfaBlur(uv, blur, corrida)) * col.a;
                return lerp(c, col.rgb, saturate(k));
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                main.rgb = Interna(main.rgb, input.uv, _Sombra1, _Sombra1Blur);
                main.rgb = Interna(main.rgb, input.uv, _Sombra2, _Sombra2Blur);
                main.rgb = Interna(main.rgb, input.uv, _Sombra3, _Sombra3Blur);
                main.rgb *= _Exposicion;
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
    }
}
