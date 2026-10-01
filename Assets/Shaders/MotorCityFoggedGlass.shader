Shader "MotorCity/FoggedGlass"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.17,0.19,0.25,0.45)
        _Smoothness("Smoothness", Range(0,1)) = 0.55
        _Metallic("Metallic", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 color = _BaseColor.rgb;
                half fog = saturate(input.fogFactor * 1.25h);
                fog = 1.0h - (1.0h - fog) * (1.0h - fog);
                color = MixFog(color, fog);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
