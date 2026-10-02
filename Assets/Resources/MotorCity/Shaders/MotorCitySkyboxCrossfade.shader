Shader "MotorCity/SkyboxCrossfade"
{
    Properties
    {
        [NoScaleOffset] _TexA ("Sky A", 2D) = "grey" {}
        [NoScaleOffset] _TexB ("Sky B", 2D) = "grey" {}

        _TintA ("Tint A", Color) = (.5,.5,.5,.5)
        _TintB ("Tint B", Color) = (.5,.5,.5,.5)

        [Gamma] _ExposureA ("Exposure A", Range(0,8)) = 1
        [Gamma] _ExposureB ("Exposure B", Range(0,8)) = 1

        _RotationA ("Rotation A", Range(0,360)) = 0
        _RotationB ("Rotation B", Range(0,360)) = 0

        _Blend ("Blend", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "PreviewType"="Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _TexA;
            sampler2D _TexB;

            half4 _TintA;
            half4 _TintB;

            half _ExposureA;
            half _ExposureB;

            float _RotationA;
            float _RotationB;

            half _Blend;

            float3 RotateAroundYInDegrees(
                float3 direction,
                float degrees)
            {
                float alpha =
                    degrees *
                    UNITY_PI /
                    180.0;

                float sine;
                float cosine;

                sincos(
                    alpha,
                    sine,
                    cosine);

                float2x2 rotation =
                    float2x2(
                        cosine,
                        -sine,
                        sine,
                        cosine);

                return float3(
                    mul(
                        rotation,
                        direction.xz),
                    direction.y).xzy;
            }

            float2 ToRadialCoords(
                float3 coords)
            {
                float3 normalizedCoords =
                    normalize(
                        coords);

                float latitude =
                    acos(
                        normalizedCoords.y);

                float longitude =
                    atan2(
                        normalizedCoords.z,
                        normalizedCoords.x);

                float2 sphereCoords =
                    float2(
                        longitude,
                        latitude) *
                    float2(
                        0.5 / UNITY_PI,
                        1.0 / UNITY_PI);

                return
                    float2(
                        0.5,
                        1.0) -
                    sphereCoords;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(
                appdata input)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(
                    input);

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(
                    output);

                output.vertex =
                    UnityObjectToClipPos(
                        input.vertex);

                output.direction =
                    input.vertex.xyz;

                return output;
            }

            fixed4 frag(
                v2f input) : SV_Target
            {
                float3 directionA =
                    RotateAroundYInDegrees(
                        input.direction,
                        -_RotationA);

                float3 directionB =
                    RotateAroundYInDegrees(
                        input.direction,
                        -_RotationB);

                float2 uvA =
                    ToRadialCoords(
                        directionA);

                float2 uvB =
                    ToRadialCoords(
                        directionB);

                half3 colorA =
                    tex2D(
                        _TexA,
                        uvA).rgb;

                half3 colorB =
                    tex2D(
                        _TexB,
                        uvB).rgb;

                colorA =
                    colorA *
                    _TintA.rgb *
                    unity_ColorSpaceDouble.rgb *
                    _ExposureA;

                colorB =
                    colorB *
                    _TintB.rgb *
                    unity_ColorSpaceDouble.rgb *
                    _ExposureB;

                half progress =
                    saturate(
                        _Blend);

                progress =
                    progress *
                    progress *
                    (3.0h -
                     2.0h *
                     progress);

                return half4(
                    lerp(
                        colorA,
                        colorB,
                        progress),
                    1.0h);
            }
            ENDCG
        }
    }

    Fallback Off
}
