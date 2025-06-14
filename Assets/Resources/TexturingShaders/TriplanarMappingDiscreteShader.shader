Shader "Custom/SingleBiomeTriplanar"
{
    Properties
    {
        _Scale("Texture Scale", Float) = 10.0
        _BiomeTexArray("Biome Texture Array", 2DArray) = "" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float _Scale;
            UNITY_DECLARE_TEX2DARRAY(_BiomeTexArray);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float biomeIndex : TEXCOORD2; // Store biome index as float (will be cast to int)
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                nointerpolation float biomeIndex : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.biomeIndex = v.biomeIndex;
                return o;
            }

            float3 SampleBiome(float2 uv, int biome)
            {
                return UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uv, biome)).rgb;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int biome = (int)i.biomeIndex;

                // Triplanar UVs
                float2 uvX = frac(i.worldPos.yz * _Scale);
                float2 uvY = frac(i.worldPos.xz * _Scale);
                float2 uvZ = frac(i.worldPos.xy * _Scale);

                float3 normal = abs(normalize(i.worldNormal));
                float3 weights = normal / max(dot(normal, 1.0), 1e-5);

                float3 col = SampleBiome(uvX, biome) * weights.x +
                             SampleBiome(uvY, biome) * weights.y +
                             SampleBiome(uvZ, biome) * weights.z;

                // Lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normalize(i.worldNormal), lightDir));
                float3 litColor = col * _LightColor0.rgb * NdotL;

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }
}
