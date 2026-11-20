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
            // Contiguous Gaussian kernel, with a slightly softer radius.
            float2 d = abs(_MainTex_TexelSize.xy) * axis;
            float3 c = tex2D(_MainTex, uv).rgb * 0.100589796;
            c += (tex2D(_MainTex, uv+d*1.4765797).rgb + tex2D(_MainTex, uv-d*1.4765797).rgb)*0.186265156;
            c += (tex2D(_MainTex, uv+d*3.4455295).rgb + tex2D(_MainTex, uv-d*3.4455295).rgb)*0.136939957;
            c += (tex2D(_MainTex, uv+d*5.4148988).rgb + tex2D(_MainTex, uv-d*5.4148988).rgb)*0.078710090;
            c += (tex2D(_MainTex, uv+d*7.3849121).rgb + tex2D(_MainTex, uv-d*7.3849121).rgb)*0.035367417;
            c += (tex2D(_MainTex, uv+d*9.3557749).rgb + tex2D(_MainTex, uv-d*9.3557749).rgb)*0.012422482;
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
