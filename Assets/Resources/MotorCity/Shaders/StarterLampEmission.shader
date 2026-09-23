Shader "MotorCity/StarterLampEmission"
{
    Properties
    {
        _BaseMap ("Lamp Texture", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 0
        _AxisOS ("Vehicle Forward Axis OS", Vector) = (0,0,1,0)
        _Cutoff ("Axle Cutoff", Float) = 0
        _Softness ("Axle Softness", Float) = 0.08
        _Mode ("Mode: 0 rear red, 1 front white", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "StarterLampEmission"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _EmissionColor;
                float4 _AxisOS;
                float _Intensity;
                float _Cutoff;
                float _Softness;
                float _Mode;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float axisCoord : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.axisCoord = dot(input.positionOS.xyz, _AxisOS.xyz);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half maxRgb = max(tex.r, max(tex.g, tex.b));
                half minRgb = min(tex.r, min(tex.g, tex.b));
                half chroma = maxRgb - minRgb;

                // Rear brake lamps: strongly red only. Amber indicators are
                // rejected by the low-green/low-blue tests.
                half rearColor =
                    smoothstep(0.48h, 0.72h, tex.r) *
                    (1.0h - smoothstep(0.20h, 0.36h, tex.g)) *
                    (1.0h - smoothstep(0.16h, 0.30h, tex.b)) *
                    smoothstep(0.22h, 0.42h, tex.r - max(tex.g, tex.b));

                // Front headlamps: bright near-neutral texels only. This rejects
                // amber turn indicators and red rear lamp texels.
                half frontColor =
                    smoothstep(0.58h, 0.82h, minRgb) *
                    (1.0h - smoothstep(0.10h, 0.30h, chroma));

                half frontSide =
                    smoothstep(
                        _Cutoff - _Softness,
                        _Cutoff + _Softness,
                        input.axisCoord);

                half rearSide = 1.0h - frontSide;
                half colorMask = lerp(rearColor, frontColor, saturate(_Mode));
                half sideMask = lerp(rearSide, frontSide, saturate(_Mode));
                half mask = colorMask * sideMask * tex.a;

                clip(mask - 0.015h);

                return half4(_EmissionColor.rgb * (_Intensity * mask), 0);
            }
            ENDHLSL
        }
    }
}
