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

                bool isSky =
#if UNITY_REVERSED_Z
                    rawDepth <= 0.0001;
#else
                    rawDepth >= 0.9999;
#endif

                float eyeDepth =
                    LinearEyeDepth(
                        rawDepth,
                        _ZBufferParams);

                float distanceFactor =
                    saturate(
                        (eyeDepth - _HazeStart) /
                        max(
                            1.0,
                            _HazeEnd - _HazeStart));

                // Strict near-field exclusion: anything before _HazeStart
                // is copied byte-for-byte with no haze contribution.
                distanceFactor =
                    eyeDepth <= _HazeStart
                        ? 0.0
                        : distanceFactor;

                distanceFactor =
                    distanceFactor *
                    distanceFactor *
                    (3.0 - 2.0 * distanceFactor);

                // Keep sky haze subtle. This supplies the missing layer of
                // air between far geometry and the sky without washing out
                // nearby cutout foliage or building windows.
                float skyFactor =
                    isSky
                        ? 0.36
                        : 0.0;

                float horizon =
                    1.0 -
                    saturate(
                        abs(
                            uv.y - 0.48) *
                        1.7);

                float haze =
                    max(
                        distanceFactor,
                        skyFactor * horizon) *
                    _HazeStrength;

                source.rgb =
                    lerp(
                        source.rgb,
                        _HazeColor.rgb,
                        saturate(haze));

                return source;
            }
            ENDHLSL
        }
    }
}
