Shader "MotorCity/DeloreanNightEmission"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0
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
            Name "DeloreanNightEmission"
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
                float _Intensity;
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

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv);

                half luminance =
                    max(
                        source.r,
                        max(
                            source.g,
                            source.b));

                half minimumChannel =
                    min(
                        source.r,
                        min(
                            source.g,
                            source.b));

                half blueOrCyan =
                    step(
                        source.r + 0.08,
                        source.b) *
                    step(
                        0.18,
                        source.b);

                half neutralWhite =
                    step(
                        0.60,
                        luminance) *
                    step(
                        luminance - 0.14,
                        minimumChannel);

                half allowed =
                    max(
                        blueOrCyan,
                        neutralWhite);

                clip(
                    allowed *
                    source.a -
                    0.01);

                return half4(
                    source.rgb *
                    _Intensity *
                    allowed *
                    source.a,
                    0.0);
            }
            ENDHLSL
        }
    }
}
