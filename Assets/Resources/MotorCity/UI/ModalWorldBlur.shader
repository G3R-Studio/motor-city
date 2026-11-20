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
            // Bilinear paired Gaussian taps, spread over roughly 18 source pixels.
            float2 d = abs(_MainTex_TexelSize.xy) * axis * 2.2;
            half3 c = tex2D(_MainTex, uv).rgb * 0.227027;
            c += (tex2D(_MainTex, uv+d*1.384615).rgb + tex2D(_MainTex, uv-d*1.384615).rgb)*0.316216;
            c += (tex2D(_MainTex, uv+d*3.230769).rgb + tex2D(_MainTex, uv-d*3.230769).rgb)*0.070270;
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
