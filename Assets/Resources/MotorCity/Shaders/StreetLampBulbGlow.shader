Shader "MotorCity/StreetLampBulbGlow"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (1.0, 0.62, 0.28, 1.0)
        _Intensity ("Intensity", Range(0, 8)) = 3.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "LampBulbGlow"
            Tags { "LightMode"="UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _Intensity;
            CBUFFER_END

            float _MotorCityNightEmission;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS =
                    positionInputs.positionCS;

                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                output.viewDirWS =
                    GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);

                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half night =
                    saturate(
                        (_MotorCityNightEmission - 0.26h) / 0.74h);

                clip(
                    night - 0.01h);

                half facing =
                    saturate(
                        dot(
                            normalize(input.normalWS),
                            normalize(input.viewDirWS)));

                half rim =
                    pow(
                        1.0h - facing,
                        2.0h);

                half core =
                    lerp(
                        1.0h,
                        0.38h,
                        rim);

                half3 color =
                    _GlowColor.rgb *
                    (_Intensity * night * core);

                color =
                    MixFog(
                        color,
                        input.fogFactor);

                return half4(
                    color,
                    0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
