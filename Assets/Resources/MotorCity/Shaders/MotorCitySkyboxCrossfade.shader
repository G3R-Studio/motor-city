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
            float4 _MotorCitySunDirection;
            float4 _MotorCityMoonDirection;
            half4 _MotorCitySunDiscColor;
            half4 _MotorCityMoonDiscColor;

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

            half4 frag(
                v2f input) : SV_Target
            {
                // Procedural sky has no baked sun/moon from the old panoramas.
                float3 viewRay = normalize(input.direction);
                float solarHeight = _MotorCitySunDirection.y;
                half daylight = smoothstep(-0.08, 0.22, solarHeight);
                half dusk = 1.0 - smoothstep(0.02, 0.30, abs(solarHeight));
                half elevation = pow(saturate(viewRay.y), 0.45);
                half3 zenith = lerp(half3(.008,.015,.035), half3(.10,.30,.62), daylight);
                half3 horizonColor = lerp(half3(.035,.045,.075), half3(.60,.73,.86), daylight);
                horizonColor = lerp(horizonColor, half3(.82,.30,.12), dusk * .72);
                half3 sky = lerp(horizonColor, zenith, elevation);
                float sunFacing = saturate(dot(normalize(float3(viewRay.x,0,viewRay.z)+.0001),
                    normalize(float3(_MotorCitySunDirection.x,0,_MotorCitySunDirection.z)+.0001)));
                sky += half3(.32,.09,.015)*dusk*pow(sunFacing,8)*(1-elevation);
                float sunDot = dot(viewRay, (_MotorCitySunDirection.xyz / max(length(_MotorCitySunDirection.xyz), 0.0001)));
                float moonDot = dot(viewRay, (_MotorCityMoonDirection.xyz / max(length(_MotorCityMoonDirection.xyz), 0.0001)));
                half horizon = smoothstep(-0.015, 0.025, viewRay.y);
                half sunDisc = smoothstep(0.99980, 0.99991, sunDot);
                half sunHalo = pow(saturate(sunDot), 256.0) * 0.14;
                half moonDisc = smoothstep(0.99982, 0.99993, moonDot);
                // Subtle procedural lunar surface, stable in world sky space.
                half lunarDetail = 0.80 + 0.20 * sin(viewRay.x*1700.0) * sin(viewRay.z*1300.0);
                sky += _MotorCitySunDiscColor.rgb * (sunDisc + sunHalo) * horizon;
                sky += _MotorCityMoonDiscColor.rgb * moonDisc * lunarDetail * horizon;
                return half4(sky, 1.0h);
            }
            ENDCG
        }
    }

    Fallback Off
}
