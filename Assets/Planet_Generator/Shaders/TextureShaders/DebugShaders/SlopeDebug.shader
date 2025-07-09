Shader "Custom/SlopeColorShader"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float slope    : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float slope  : TEXCOORD1;
            };

            fixed4 _ColdColor;
            fixed4 _HotColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.slope = v.slope;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return float4(i.slope/90,0,0,1);
            }
            ENDCG
        }
    }
}
