Shader "Hidden/MotorCity/AtmosphericHaze"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "AtmosphericHaze"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _HazeColor;
            float _HazeStart;
            float _HazeEnd;
            float _HazeStrength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv =
                    input.texcoord.xy;

                half4 source =
                    SAMPLE_TEXTURE2D_X_LOD(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv,
                        _BlitMipLevel);

                float rawDepth =
                    SampleSceneDepth(
                        uv);

                float eyeDepth =
                    LinearEyeDepth(
                        rawDepth,
                        _ZBufferParams);

                float distance01 =
                    saturate(
                        (eyeDepth - _HazeStart) /
                        max(
                            1.0,
                            _HazeEnd - _HazeStart));

                distance01 =
                    distance01 *
                    distance01 *
                    (3.0 - 2.0 * distance01);

                float horizon =
                    1.0 -
                    saturate(
                        abs(
                            uv.y - 0.48) *
                        1.45);

                float haze =
                    distance01 *
                    lerp(
                        0.72,
                        1.0,
                        horizon) *
                    _HazeStrength;

                source.rgb =
                    lerp(
                        source.rgb,
                        _HazeColor.rgb,
                        saturate(
                            haze));

                return source;
            }
            ENDHLSL
        }
    }
}
