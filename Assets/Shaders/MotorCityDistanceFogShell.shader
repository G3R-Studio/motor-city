Shader "MotorCity/DistanceFogShell"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.56,0.61,0.66,0.5)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+25" "RenderType"="Transparent" }
        Pass
        {
            Name "DistanceFogShell"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _FogColor;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half horizon : TEXCOORD0;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 direction = normalize(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Keep overhead sky and the ground from being tinted.
                output.horizon = saturate((0.56h - abs(direction.y)) * 3.0h);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = _FogColor;
                color.a *= input.horizon;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
