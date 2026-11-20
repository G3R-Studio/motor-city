Shader "MotorCity/UI/ModalWorldBlur"
{
    Properties { [PerRendererData] _MainTex ("Background", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        fixed4 Gaussian(float2 uv, float2 axis)
        {
            // Contiguous Gaussian kernel, slightly reduced radius.
            float2 d = abs(_MainTex_TexelSize.xy) * axis;
            float3 c = tex2D(_MainTex, uv).rgb * 0.115697629;
            c += (tex2D(_MainTex, uv+d*1.4694259).rgb + tex2D(_MainTex, uv-d*1.4694259).rgb)*0.209339965;
            c += (tex2D(_MainTex, uv+d*3.4290534).rgb + tex2D(_MainTex, uv-d*3.4290534).rgb)*0.140343474;
            c += (tex2D(_MainTex, uv+d*5.3896034).rgb + tex2D(_MainTex, uv-d*5.3896034).rgb)*0.068321080;
            c += (tex2D(_MainTex, uv+d*7.3515473).rgb + tex2D(_MainTex, uv-d*7.3515473).rgb)*0.024146666;
            return fixed4(c,1);
        }
        fixed4 horizontal(v2f_img i):SV_Target { return Gaussian(i.uv,float2(1,0)); }
        fixed4 vertical(v2f_img i):SV_Target { return Gaussian(i.uv,float2(0,1)); }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment horizontal
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment vertical
            ENDCG
        }
    }
}
