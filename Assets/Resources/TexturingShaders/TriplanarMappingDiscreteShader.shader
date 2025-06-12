Shader "Custom/TriBiomeBlend"
{
    Properties
    {
        _Scale("Texture Scale", Float) = 10.0
        _BlendSharpness("Blend Sharpness", Range(1, 10)) = 2
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
            float _BlendSharpness;
            UNITY_DECLARE_TEX2DARRAY(_BiomeTexArray);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 biomeIndices : TEXCOORD2; // Each vertex stores its own biome index in one of these (others = 0)
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                nointerpolation float3 biomeIndices : TEXCOORD2; // Interpolated biome index weights
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.biomeIndices = v.biomeIndices; // Will be interpolated
                return o;
            }

            float3 SampleBiome(float2 uv, int biome)
            {
                return UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uv, biome)).rgb;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Triplanar UVs
                float2 uvX = frac(i.worldPos.yz * _Scale);
                float2 uvY = frac(i.worldPos.xz * _Scale);
                float2 uvZ = frac(i.worldPos.xy * _Scale);

                float3 normal = abs(normalize(i.worldNormal));
                float3 triplanarWeights = pow(normal, _BlendSharpness);
                triplanarWeights /= max(dot(triplanarWeights, 1.0), 1e-5);

                // Determine up to 3 biomes and their weights
                int biome0 = i.biomeIndices.x;
                int biome1 = i.biomeIndices.y;
                int biome2 = i.biomeIndices.z;

                float3 baryWeights = float3(i.biomeIndices.x - biome0, i.biomeIndices.y - biome1, i.biomeIndices.z - biome2);
                baryWeights = saturate(i.biomeIndices);
                baryWeights /= max(dot(baryWeights, 1.0), 1e-5); // Normalize

                // Triplanar sample for each biome
                float3 col0 = SampleBiome(uvX, biome0) * triplanarWeights.x +
                              SampleBiome(uvY, biome0) * triplanarWeights.y +
                              SampleBiome(uvZ, biome0) * triplanarWeights.z;

                float3 col1 = SampleBiome(uvX, biome1) * triplanarWeights.x +
                              SampleBiome(uvY, biome1) * triplanarWeights.y +
                              SampleBiome(uvZ, biome1) * triplanarWeights.z;

                float3 col2 = SampleBiome(uvX, biome2) * triplanarWeights.x +
                              SampleBiome(uvY, biome2) * triplanarWeights.y +
                              SampleBiome(uvZ, biome2) * triplanarWeights.z;

                // Blend all biome colors based on barycentric-style weights
                float3 finalColor = col0 * baryWeights.x + col1 * baryWeights.y + col2 * baryWeights.z;

                // Lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normalize(i.worldNormal), lightDir));
                float3 litColor = finalColor * _LightColor0.rgb * NdotL;

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }
}
