Shader "MotorCity/NightEmissive"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _EmissionMap("Emission Map", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0,2)) = 0.75
        [HDR] _EmissionColor("Emission Color", Color) = (1.0,0.62,0.28,1)
        _EmissionStrength("Emission Strength", Range(0,8)) = 2.6
        _DayGlassTint("Day Glass Tint", Color) = (0.16,0.22,0.28,1)
        _DayGlassLift("Day Glass Lift", Range(0,1)) = 0.34
        _FresnelColor("Fresnel Color", Color) = (0.52,0.66,0.78,1)
        _FresnelStrength("Fresnel Strength", Range(0,1)) = 0.24
        _SpecularStrength("Day Specular", Range(0,1)) = 0.22
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

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _EmissionMap_ST;
                float4 _BumpMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionStrength;
                half _BumpScale;
                half4 _DayGlassTint;
                half _DayGlassLift;
                half4 _FresnelColor;
                half _FresnelStrength;
                half _SpecularStrength;
            CBUFFER_END

            float _MotorCityNightEmission;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 baseUv : TEXCOORD3;
                float2 emissionUv : TEXCOORD4;
                float2 normalUv : TEXCOORD5;
                half3 viewDirWS : TEXCOORD6;
                half fogFactor : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                half tangentSign =
                    input.tangentOS.w *
                    GetOddNegativeScale();

                output.tangentWS =
                    half4(
                        normalInputs.tangentWS,
                        tangentSign);

                output.baseUv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.emissionUv = TRANSFORM_TEX(input.uv, _EmissionMap);
                output.normalUv = TRANSFORM_TEX(input.uv, _BumpMap);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.baseUv) *
                    _BaseColor;

                half3 tangentWS =
                    normalize(
                        input.tangentWS.xyz);

                half3 bitangentWS =
                    input.tangentWS.w *
                    cross(
                        input.normalWS,
                        tangentWS);

                half3 normalTS =
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _BumpMap,
                            sampler_BumpMap,
                            input.normalUv),
                        _BumpScale);

                half3 normalWS =
                    normalize(
                        TransformTangentToWorld(
                            normalTS,
                            half3x3(
                                tangentWS,
                                bitangentWS,
                                input.normalWS)));

                float4 shadowCoord =
                    TransformWorldToShadowCoord(input.positionWS);

                Light mainLight =
                    GetMainLight(shadowCoord);

                half diffuse =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction));

                half3 ambient =
                    SampleSH(normalWS);

                half3 lighting =
                    ambient +
                    mainLight.color *
                    diffuse *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                half3 litBase =
                    baseSample.rgb *
                    max(
                        lighting,
                        half3(0.11h, 0.11h, 0.11h));

                half3 dayGlass =
                    lerp(
                        litBase,
                        _DayGlassTint.rgb,
                        _DayGlassLift);

                half3 viewDirWS =
                    normalize(input.viewDirWS);

                half fresnel =
                    pow(
                        1.0h -
                        saturate(
                            dot(
                                normalWS,
                                viewDirWS)),
                        4.0h);

                half3 halfDirection =
                    normalize(
                        mainLight.direction +
                        viewDirWS);

                half specular =
                    pow(
                        saturate(
                            dot(
                                normalWS,
                                halfDirection)),
                        48.0h) *
                    _SpecularStrength *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                half3 color =
                    dayGlass +
                    _FresnelColor.rgb *
                    fresnel *
                    _FresnelStrength +
                    mainLight.color *
                    specular;

                half4 emissionSample =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        input.emissionUv);

                half nightFactor =
                    saturate(
                        (_MotorCityNightEmission - 0.30h) /
                        0.70h);

                half emissionMask =
                    emissionSample.a *
                    max(
                        emissionSample.r,
                        max(
                            emissionSample.g,
                            emissionSample.b));

                color +=
                    emissionSample.rgb *
                    _EmissionColor.rgb *
                    _EmissionStrength *
                    emissionMask *
                    nightFactor;

                color =
                    MixFog(
                        color,
                        input.fogFactor);

                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack Off
}
