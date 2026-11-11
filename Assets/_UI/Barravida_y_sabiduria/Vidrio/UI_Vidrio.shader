// Vidrio esmerilado para la interfaz (HUD): muestra lo que hay detras en el juego, pero desenfocado,
// con un tono encima y (opcional) un brillo suave arriba, como un vidrio.
// La forma la da el sprite de la Image (por ejemplo hud_fondo_circulo): donde el sprite tiene algo, hay vidrio.
// Lo que hay detras sale de _CameraSortingLayerTexture: la copia de la escena que hace el Renderer 2D
// (la activa el menu  Pomelo -> HUD -> Poner vidrio en la vida).
Shader "Pomelo/UI Vidrio"
{
    Properties
    {
        [PerRendererData] _MainTex ("Forma (sprite)", 2D) = "white" {}
        _Color ("Tinte de la Image", Color) = (1,1,1,1)

        _Desenfoque ("Desenfoque (px de pantalla)", Range(0, 40)) = 12
        _ColorVidrio ("Color del vidrio (alfa = cuanto tapa)", Color) = (0.12, 0.17, 0.15, 0.35)
        _Brillo ("Brillo de arriba", Range(0, 1)) = 0.15
        _Grano ("Grano esmerilado", Range(0, 1)) = 0.35
        _UmbralForma ("Alfa del sprite que cuenta como lleno", Range(0.01, 1)) = 0.2
        [Toggle] _InvertirY ("Dar vuelta (si se ve al reves)", Float) = 0

        // lo que usan todas las Image de UI (mascaras y recortes)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Vidrio"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 mundo    : TEXCOORD1;
                float4 pantalla : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _CameraSortingLayerTexture;
            fixed4 _Color;
            float4 _ClipRect;
            float _Desenfoque, _Brillo, _Grano, _UmbralForma, _InvertirY;
            fixed4 _ColorVidrio;

            v2f vert (appdata v)
            {
                v2f o;
                o.mundo = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.pantalla = ComputeScreenPos(o.pos);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float Azar(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            fixed4 frag (v2f i) : SV_Target
            {
                // forma: donde el sprite tiene algo (el fondo del HUD es negro al 20 %, eso cuenta como lleno)
                float forma = saturate(tex2D(_MainTex, i.uv).a / _UmbralForma);

                // lugar en la pantalla
                float2 uv = i.pantalla.xy / i.pantalla.w;
                if (_InvertirY > 0.5) uv.y = 1.0 - uv.y;

                // grano: corre un poquito cada muestra al azar, como un vidrio esmerilado
                float2 px = 1.0 / _ScreenParams.xy;
                float2 grano = (float2(Azar(i.pos.xy), Azar(i.pos.yx + 7.1)) - 0.5) * px * _Desenfoque * _Grano;

                // desenfoque: 3 anillos de muestras alrededor
                float3 suma = tex2D(_CameraSortingLayerTexture, uv + grano).rgb;
                float peso = 1.0;
                [unroll] for (int anillo = 1; anillo <= 3; anillo++)
                {
                    float radio = _Desenfoque * anillo / 3.0;
                    float w = 1.0 - (anillo - 1) * 0.25;
                    [unroll] for (int k = 0; k < 8; k++)
                    {
                        float ang = (k + anillo * 0.5) * 0.785398;   // 45 grados, cada anillo girado
                        float2 d = float2(cos(ang), sin(ang)) * radio * px;
                        suma += tex2D(_CameraSortingLayerTexture, uv + d + grano).rgb * w;
                        peso += w;
                    }
                }
                float3 fondo = suma / peso;

                // vidrio: tono encima + brillo suave arriba (mas fuerte en el borde de arriba de la forma)
                float3 color = lerp(fondo, _ColorVidrio.rgb, _ColorVidrio.a);
                color += _Brillo * smoothstep(0.35, 1.0, i.uv.y);

                fixed4 salida = fixed4(color, forma * i.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                salida.a *= UnityGet2DClipping(i.mundo.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(salida.a - 0.001);
                #endif
                return salida;
            }
            ENDCG
        }
    }
}
