Shader "MotorCity/CityBackdrop"
{
    Properties
    {
        [MainTexture] _BaseMap("Backdrop", 2D) = "white" {}
        [MainColor] _DayTint("Day Tint", Color) = (0.78,0.82,0.86,1)
        _NightTint("Night Tint", Color) = (0.055,0.075,0.11,1)
        _NightBrightness("Night Brightness", Range(0,1)) = 0.42
        _Alpha("Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-50"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Backdrop"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _DayTint;
                half4 _NightTint;
                half _NightBrightness;
                half _Alpha;
            CBUFFER_END

            float _MotorCityNightEmission;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
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

                output.positionHCS =
                    positionInputs.positionCS;

                output.uv =
                    TRANSFORM_TEX(input.uv, _BaseMap);

                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);

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

                half night =
                    saturate(
                        (_MotorCityNightEmission - 0.18h) /
                        0.82h);

                half3 dayColor =
                    tex.rgb *
                    _DayTint.rgb;

                half3 nightColor =
                    tex.rgb *
                    _NightTint.rgb *
                    _NightBrightness;

                half3 color =
                    lerp(
                        dayColor,
                        nightColor,
                        night);

                color =
                    MixFog(
                        color,
                        input.fogFactor);

                return half4(
                    color,
                    tex.a * _Alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
