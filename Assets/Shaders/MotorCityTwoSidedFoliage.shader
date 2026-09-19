Shader "MotorCity/TwoSidedFoliage"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Cull Off
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
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
                float2 uv : TEXCOORD2;
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

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.normalWS =
                    normalInputs.normalWS;

                output.uv =
                    TRANSFORM_TEX(input.uv, _BaseMap);

                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 Frag(
                Varyings input,
                FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv) *
                    _BaseColor;

                clip(
                    baseSample.a -
                    _Cutoff);

                half faceSign =
                    IS_FRONT_VFACE(
                        frontFace,
                        1.0h,
                        -1.0h);

                half3 normalWS =
                    normalize(
                        input.normalWS) *
                    faceSign;

                float4 shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS);

                Light mainLight =
                    GetMainLight(
                        shadowCoord);

                half diffuse =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction));

                half3 ambient =
                    SampleSH(
                        normalWS);

                half3 lighting =
                    ambient +
                    mainLight.color *
                    diffuse *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                half3 color =
                    baseSample.rgb *
                    max(
                        lighting,
                        half3(
                            0.08h,
                            0.08h,
                            0.08h));

                color =
                    MixFog(
                        color,
                        input.fogFactor);

                return
                    half4(
                        color,
                        1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
