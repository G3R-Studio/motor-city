Shader "MotorCity/UI/ModalWorldBlur"
{
    Properties { [PerRendererData] _MainTex ("Background", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = abs(_MainTex_TexelSize.xy) * 1.7;
                fixed3 c = tex2D(_MainTex,i.uv).rgb * .4;
                c += (tex2D(_MainTex,i.uv+float2(d.x,0)).rgb + tex2D(_MainTex,i.uv-float2(d.x,0)).rgb
                    + tex2D(_MainTex,i.uv+float2(0,d.y)).rgb + tex2D(_MainTex,i.uv-float2(0,d.y)).rgb) * .15;
                return fixed4(c,1);
            }
            ENDCG
        }
    }
}
