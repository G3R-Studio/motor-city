Shader "MotorCity/PanoramicHazeSkybox"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Panoramic Texture", 2D) = "grey" {}
        _Tint("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _Exposure("Exposure", Range(0,8)) = 1
        _Rotation("Rotation", Range(0,360)) = 0
        _HazeColor("Haze Color", Color) = (0.6,0.65,0.7,1)
        _HazeStrength("Haze Strength", Range(0,1)) = 0.25
        _HazeHeight("Haze Height", Range(0.05,1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "PreviewType"="Skybox"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _HazeColor;
                half _Exposure;
                half _Rotation;
                half _HazeStrength;
                half _HazeHeight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            float3 RotateY(float3 direction, float degrees)
            {
                float angleRad = degrees * 0.01745329252;
                float s = sin(angleRad);
                float c = cos(angleRad);

                return float3(
                    c * direction.x - s * direction.z,
                    direction.y,
                    s * direction.x + c * direction.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 direction =
                    normalize(input.positionOS.xyz);

                direction =
                    RotateY(
                        direction,
                        _Rotation);

                output.direction =
                    direction;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz);

                output.positionHCS =
                    positionInputs.positionCS;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction =
                    normalize(input.direction);

                const float InvTwoPi =
                    0.15915494309;

                const float InvPi =
                    0.31830988618;

                float2 uv;

                uv.x =
                    atan2(
                        direction.z,
                        direction.x) *
                    InvTwoPi +
                    0.5;

                uv.y =
                    asin(
                        clamp(
                            direction.y,
                            -1.0,
                            1.0)) *
                    InvPi +
                    0.5;

                half3 sky =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv).rgb;

                sky *=
                    _Tint.rgb *
                    unity_ColorSpaceDouble.rgb *
                    _Exposure;

                half horizon =
                    saturate(
                        1.0h -
                        abs(direction.y) /
                        max(
                            _HazeHeight,
                            0.05h));

                horizon =
                    horizon *
                    horizon *
                    (3.0h - 2.0h * horizon);

                half lowerAtmosphere =
                    saturate(
                        1.0h -
                        direction.y * 2.0h);

                half haze =
                    saturate(
                        horizon *
                        lowerAtmosphere *
                        _HazeStrength);

                sky =
                    lerp(
                        sky,
                        _HazeColor.rgb,
                        haze);

                return
                    half4(
                        sky,
                        1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
