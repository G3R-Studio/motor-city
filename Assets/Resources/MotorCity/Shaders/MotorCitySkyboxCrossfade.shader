Shader "MotorCity/SkyboxCrossfade"
{
    Properties
    {
        _TexA("Sky A", 2D) = "black" {}
        _TexB("Sky B", 2D) = "black" {}
        _Blend("Blend", Range(0,1)) = 0
        _Exposure("Exposure", Range(0,8)) = 1
        _Rotation("Rotation", Range(0,360)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_TexA);
            SAMPLER(sampler_TexA);
            TEXTURE2D(_TexB);
            SAMPLER(sampler_TexB);

            CBUFFER_START(UnityPerMaterial)
                float4 _TexA_ST;
                float4 _TexB_ST;
                half _Blend;
                half _Exposure;
                half _Rotation;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 directionWS =
                    TransformObjectToWorldDir(input.positionOS.xyz);

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.directionWS =
                    directionWS;

                return output;
            }

            float2 DirectionToLatLong(float3 direction)
            {
                direction =
                    normalize(direction);

                float rotationRadians =
                    radians(_Rotation);

                float sine =
                    sin(rotationRadians);

                float cosine =
                    cos(rotationRadians);

                float2 rotatedXZ =
                    float2(
                        direction.x * cosine -
                        direction.z * sine,
                        direction.x * sine +
                        direction.z * cosine);

                direction.x =
                    rotatedXZ.x;

                direction.z =
                    rotatedXZ.y;

                float longitude =
                    atan2(direction.x, direction.z);

                float latitude =
                    acos(
                        clamp(
                            direction.y,
                            -1.0,
                            1.0));

                return float2(
                    longitude / (2.0 * PI) + 0.5,
                    latitude / PI);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv =
                    DirectionToLatLong(input.directionWS);

                half3 skyA =
                    SAMPLE_TEXTURE2D(
                        _TexA,
                        sampler_TexA,
                        uv).rgb;

                half3 skyB =
                    SAMPLE_TEXTURE2D(
                        _TexB,
                        sampler_TexB,
                        uv).rgb;

                half blend =
                    smoothstep(
                        0.0h,
                        1.0h,
                        saturate(_Blend));

                half3 color =
                    lerp(
                        skyA,
                        skyB,
                        blend) *
                    _Exposure;

                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
