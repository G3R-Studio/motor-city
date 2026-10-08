Shader "MotorCity/VehicleGlass"
{
    Properties
    {
        [MainColor] _BaseColor("Face-on charcoal", Color) = (0.024,0.028,0.035,1)
        _EdgeColor("Grazing grey reflection", Color) = (0.22,0.235,0.25,1)
        _FresnelPower("Fresnel falloff", Range(1,6)) = 2.8
        _ReflectionStrength("Probe reflection strength", Range(0,1)) = 0.42
        _Smoothness("Reflection smoothness", Range(0.5,1)) = 0.91
        _SpecularStrength("Light glint", Range(0,1)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Cull Back
        ZWrite On
        Blend One Zero

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                half _FresnelPower;
                half _ReflectionStrength;
                half _Smoothness;
                half _SpecularStrength;
            CBUFFER_END

            // Global already driven by Motor City's day/night cycle.
            float _MotorCityNightEmission;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 viewDirWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.viewDirWS =
                    GetWorldSpaceNormalizeViewDir(positions.positionWS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 N = normalize(input.normalWS);
                half3 V = normalize(input.viewDirWS);
                half ndotv = saturate(abs(dot(N, V)));

                // This is solid, dark-tinted automotive glazing, not alpha
                // transparency. The grey edge appears as the camera turns,
                // even without reflection probes or an HDR skybox.
                half fresnel =
                    pow(1.0h - ndotv, max(_FresnelPower, 1.0h));
                half3 charcoal =
                    lerp(_BaseColor.rgb, _EdgeColor.rgb, fresnel);

                half3 reflectionDirection = reflect(-V, N);
                half perceptualRoughness =
                    saturate(1.0h - _Smoothness);
                float2 screenUV =
                    GetNormalizedScreenSpaceUV(input.positionHCS);
                half3 environment =
                    GlossyEnvironmentReflection(
                        reflectionDirection,
                        input.positionWS,
                        perceptualRoughness,
                        1.0h,
                        screenUV);

                // Desaturate the probe: a blue sky should read as a grey
                // reflection, not turn the entire windshield bright blue.
                half grey = dot(
                    max(environment, half3(0,0,0)),
                    half3(0.2126h, 0.7152h, 0.0722h));
                half3 neutralProbe =
                    lerp(environment, half3(grey,grey,grey), 0.82h);

                half skyBand = smoothstep(
                    -0.25h, 0.75h, reflectionDirection.y);
                half night = saturate(_MotorCityNightEmission);
                half skyLift =
                    (0.012h + 0.045h * fresnel) *
                    skyBand * (1.0h - 0.55h * night);
                half probeAmount =
                    (0.025h + 0.22h * fresnel) *
                    _ReflectionStrength *
                    (1.0h - 0.45h * night);

                Light mainLight =
                    GetMainLight(
                        TransformWorldToShadowCoord(input.positionWS));
                half3 H = normalize(mainLight.direction + V);
                half highlightPower =
                    lerp(32.0h, 220.0h, _Smoothness);
                half glint =
                    pow(saturate(dot(N, H)), highlightPower) *
                    _SpecularStrength *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                half3 color =
                    charcoal +
                    max(neutralProbe, half3(0,0,0)) * probeAmount +
                    half3(skyLift, skyLift, skyLift) +
                    mainLight.color * glint * (0.15h + 0.85h * fresnel);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack Off
}
