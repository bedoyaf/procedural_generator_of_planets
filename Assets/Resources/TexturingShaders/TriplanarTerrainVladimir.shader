Shader "Custom/TriBiomeBlendFragment"
{
    Properties { }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 bary   : TEXCOORD0;
                float4 color0 : TEXCOORD1;
                float4 color1 : TEXCOORD2;
                float4 color2 : TEXCOORD3;
            };

            struct v2f
            {
                float4 pos     : SV_POSITION;
                float3 bary    : TEXCOORD0;
                float4 color0  : TEXCOORD1;
                float4 color1  : TEXCOORD2;
                float4 color2  : TEXCOORD3;
                float3 worldNormal : TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.bary   = v.bary;
                o.color0 = v.color0;
                o.color1 = v.color1;
                o.color2 = v.color2;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 c0 = i.color0.rgb;
                float3 c1 = i.color1.rgb;
                float3 c2 = i.color2.rgb;

                float3 blended = i.bary.x * c0
                               + i.bary.y * c1
                               + i.bary.z * c2;
                
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float NdotL = saturate(dot(normalize(i.worldNormal), lightDir));
                float3 litColor = blended * lightColor * NdotL;


                return float4(litColor, 1.0);


            //    return float4(blended, 1);
            }
            ENDCG
        }
    }
}
