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
            #pragma target 3.0

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

            float CloudHash(float2 point)
            {
                float3 seed = frac(float3(point.xyx) * 0.1031);
                seed += dot(seed, seed.yzx + 33.33);
                return frac((seed.x + seed.y) * seed.z);
            }

            float CloudNoise(float2 point)
            {
                float2 cell = floor(point);
                float2 blend = frac(point);
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(
                    lerp(CloudHash(cell), CloudHash(cell + float2(1, 0)), blend.x),
                    lerp(CloudHash(cell + float2(0, 1)), CloudHash(cell + float2(1, 1)), blend.x),
                    blend.y);
            }

            float CloudDensity(float2 point)
            {
                float density = CloudNoise(point) * 0.5333;
                point = point * 2.03 + float2(13.7, 9.2);
                density += CloudNoise(point) * 0.2667;
                point = point * 2.03 + float2(13.7, 9.2);
                density += CloudNoise(point) * 0.1333;
                point = point * 2.03 + float2(13.7, 9.2);
                return density + CloudNoise(point) * 0.0667;
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

                // A world-oriented cloud layer: turning the camera does not move
                // the clouds. Wind drifts continuously, independently of day length.
                float2 cloudPoint = viewRay.xz * 2.8 / max(viewRay.y + 0.12, 0.12);
                cloudPoint += _Time.y * float2(0.018, 0.007);
                float density = CloudDensity(cloudPoint);
                float coverage = CloudNoise(cloudPoint * 0.32 + float2(31.2, 7.8));
                float edge = 0.48 + (coverage - 0.5) * 0.16;
                // Derivative filtering keeps distant cloud edges soft on resize.
                float softness = max(0.085, fwidth(density) * 1.5);
                half cloudAlpha = smoothstep(edge - softness, edge + softness, density);
                cloudAlpha *= smoothstep(0.015, 0.16, viewRay.y) * 0.94;
                half thickness = smoothstep(edge, edge + 0.24, density);
                half3 cloudColor = lerp(half3(.025, .035, .06), half3(.91, .94, .98), daylight);
                cloudColor *= lerp(1.0, 0.66, thickness);
                half sunsetLight = dusk * (0.25 + 0.75 * pow(sunFacing, 4));
                cloudColor = lerp(cloudColor, half3(.92, .46, .25) * lerp(1.0, .65, thickness), sunsetLight * .72);
                cloudColor += half3(1.0, .85, .65) * daylight
                    * pow(saturate(sunDot), 16) * (1.0 - thickness) * .22;
                // Composite over the discs so thicker clouds obscure sun and moon.
                sky = lerp(sky, cloudColor, cloudAlpha);
                return half4(sky, 1.0h);
            }
            ENDCG
        }
    }

    Fallback Off
}
