Shader "MotorCity/UI/GarageNeonBorder"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BorderColor ("Border Color", Color) = (0.35,0.75,1,0.9)
        _GlowColor ("Glow Color", Color) = (0.65,0.25,1,0.45)
        _RectSize ("Rect Size", Vector) = (256,128,0,0)
        _FramePadding ("Frame Padding", Float) = 8
        _BorderPx ("Border Width", Float) = 1.5
        _GlowPx ("Glow Width", Float) = 7

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
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            Name "GarageNeonBorder"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _BorderColor;
            fixed4 _GlowColor;
            float4 _RectSize;
            float _FramePadding;
            float _BorderPx;
            float _GlowPx;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 sizePx = max(_RectSize.xy, float2(1.0, 1.0));

                float leftPx = i.texcoord.x * sizePx.x;
                float rightPx = (1.0 - i.texcoord.x) * sizePx.x;
                float bottomPx = i.texcoord.y * sizePx.y;
                float topPx = (1.0 - i.texcoord.y) * sizePx.y;

                float edgePx =
                    min(
                        min(leftPx, rightPx),
                        min(bottomPx, topPx));

                float frameDistancePx =
                    abs(edgePx - _FramePadding);

                float aa = max(fwidth(frameDistancePx), 0.75);

                float border =
                    1.0 - smoothstep(
                        _BorderPx,
                        _BorderPx + aa,
                        frameDistancePx);

                float glow =
                    1.0 - smoothstep(
                        _GlowPx,
                        _GlowPx + aa,
                        frameDistancePx);

                float glowOnly =
                    saturate(glow - border * 0.75);

                fixed4 color;
                color.rgb =
                    _GlowColor.rgb * glowOnly +
                    _BorderColor.rgb * border;

                color.a =
                    saturate(
                        _GlowColor.a * glowOnly +
                        _BorderColor.a * border);

                color *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(
                    i.worldPosition.xy,
                    _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
