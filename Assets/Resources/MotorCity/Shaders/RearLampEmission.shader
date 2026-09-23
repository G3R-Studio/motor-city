Shader "MotorCity/RearLampEmission"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 0.03, 0.015, 1)
        _Intensity ("Intensity", Float) = 1
        _RearAxisOS ("Rear Axis Object Space", Vector) = (0, 0, -1, 0)
        _RearCutoff ("Rear Cutoff", Float) = 0
        _RearSoftness ("Rear Softness", Float) = 0.1
        _ChromaLow ("Chroma Low", Float) = 0.22
        _ChromaHigh ("Chroma High", Float) = 0.46
        _RedLow ("Red Low", Float) = 0.62
        _RedHigh ("Red High", Float) = 0.90
        _GreenLow ("Green Low", Float) = 0.20
        _GreenHigh ("Green High", Float) = 0.42
        _BlueLow ("Blue Low", Float) = 0.18
        _BlueHigh ("Blue High", Float) = 0.38
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "RearLampEmission"
            Tags { "LightMode" = "UniversalForward" }

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
                float4 _RearAxisOS;
                float _Intensity;
                float _RearCutoff;
                float _RearSoftness;
                float _ChromaLow;
                float _ChromaHigh;
                float _RedLow;
                float _RedHigh;
                float _GreenLow;
                float _GreenHigh;
                float _BlueLow;
                float _BlueHigh;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float rearCoord : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz);

                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap);

                output.rearCoord =
                    dot(
                        input.positionOS.xyz,
                        _RearAxisOS.xyz);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv);

                // PolyPack uses a shared atlas: on red/orange cars the body
                // itself is red, so a simple "red pixel" test lights the whole
                // rear quarter. Real tail-lamp texels are substantially
                // brighter and more saturated than the painted body.
                half chroma =
                    source.r -
                    max(source.g, source.b);

                half redDominance =
                    smoothstep(
                        _ChromaLow,
                        _ChromaHigh,
                        chroma);

                half redBrightness =
                    smoothstep(
                        _RedLow,
                        _RedHigh,
                        source.r);

                half lowGreen =
                    1.0 -
                    smoothstep(
                        _GreenLow,
                        _GreenHigh,
                        source.g);

                half lowBlue =
                    1.0 -
                    smoothstep(
                        _BlueLow,
                        _BlueHigh,
                        source.b);

                half rearMask =
                    smoothstep(
                        _RearCutoff -
                        _RearSoftness,
                        _RearCutoff +
                        _RearSoftness,
                        input.rearCoord);

                half mask =
                    redDominance *
                    redBrightness *
                    lowGreen *
                    lowBlue *
                    rearMask *
                    source.a;

                clip(
                    mask -
                    0.015);

                return half4(
                    _EmissionColor.rgb *
                    (_Intensity * mask),
                    0.0);
            }
            ENDHLSL
        }
    }
}
