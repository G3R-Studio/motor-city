Shader "MotorCity/CityGlass"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (0.28,0.38,0.46,1)
        _Opacity("Opacity", Range(0.2,1.0)) = 0.72
        _Smoothness("Smoothness", Range(0,1)) = 0.9
        _FresnelStrength("Fresnel Strength", Range(0,1)) = 0.38
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardGlass"
            Tags { "LightMode" = "UniversalForwardOnly" }

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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Opacity;
                half _Smoothness;
                half _FresnelStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
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
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv);

                half3 normalWS =
                    normalize(input.normalWS);

                half3 viewDirWS =
                    normalize(input.viewDirWS);

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

                half fresnel =
                    pow(
                        1.0h -
                        saturate(
                            dot(
                                normalWS,
                                viewDirWS)),
                        4.0h);

                half3 baseColor =
                    lerp(
                        tex.rgb,
                        tex.rgb * _BaseColor.rgb,
                        0.72h);

                half3 lighting =
                    ambient * 0.62h +
                    mainLight.color *
                    diffuse *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation *
                    0.38h;

                half3 reflectionTint =
                    lerp(
                        half3(0.16h, 0.22h, 0.28h),
                        half3(0.62h, 0.72h, 0.82h),
                        _Smoothness);

                half3 color =
                    baseColor *
                    max(
                        lighting,
                        half3(0.18h, 0.18h, 0.18h));

                color +=
                    reflectionTint *
                    fresnel *
                    _FresnelStrength;

                color =
                    MixFog(
                        color,
                        input.fogFactor);

                // Deliberately ignore the old FCG texture alpha here.
                // Several legacy glass atlases use tiny alpha values that
                // become almost invisible after conversion to URP.
                return half4(
                    color,
                    saturate(_Opacity));
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack Off
}
