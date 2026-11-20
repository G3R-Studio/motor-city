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
            // Every adjacent texel contributes; do not dilate the sampling kernel.
            float2 d = abs(_MainTex_TexelSize.xy) * axis;
            float3 c = tex2D(_MainTex, uv).rgb * 0.080779934;
            c += (tex2D(_MainTex, uv+d*1.4850045).rgb + tex2D(_MainTex, uv-d*1.4850045).rgb)*0.153749662;
            c += (tex2D(_MainTex, uv+d*3.4650571).rgb + tex2D(_MainTex, uv-d*3.4650571).rgb)*0.126131344;
            c += (tex2D(_MainTex, uv+d*5.4452208).rgb + tex2D(_MainTex, uv-d*5.4452208).rgb)*0.088315322;
            c += (tex2D(_MainTex, uv+d*7.4255575).rgb + tex2D(_MainTex, uv-d*7.4255575).rgb)*0.052777441;
            c += (tex2D(_MainTex, uv+d*9.4061269).rgb + tex2D(_MainTex, uv-d*9.4061269).rgb)*0.026918619;
            c += (tex2D(_MainTex, uv+d*11.3869858).rgb + tex2D(_MainTex, uv-d*11.3869858).rgb)*0.011717645;
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
