Shader "MotorCity/UI/ModalWorldBlur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float2 d = _CameraOpaqueTexture_TexelSize.xy * 1.5;
                half3 c = SampleSceneColor(i.uv) * .4;
                c += (SampleSceneColor(i.uv + float2(d.x,0)) + SampleSceneColor(i.uv - float2(d.x,0))
                    + SampleSceneColor(i.uv + float2(0,d.y)) + SampleSceneColor(i.uv - float2(0,d.y))) * .15;
                return half4(c,.42);
            }
            ENDHLSL
        }
    }
}
