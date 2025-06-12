/*
Shader "Custom/TriplanarBiomeBlended"
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

            float _BiomeTexArray_Depth;


            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 biomeData : TEXCOORD2; // x = primary, y = secondary, z = blendFactor
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 biomeData : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.biomeData = v.biomeData;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {


                float3 scaledPos = i.worldPos * _Scale;

                float2 uvX = scaledPos.yz;
                float2 uvY = scaledPos.xz;
                float2 uvZ = scaledPos.xy;


                float3 normal = abs(normalize(i.worldNormal));
                float3 weights = pow(normal, _BlendSharpness);
                weights /= max(dot(weights, float3(1, 1, 1)), 1e-5);

                
                int sliceCount = (int)_BiomeTexArray_Depth;
                int primaryBiome = clamp((int)i.biomeData.x, 0, sliceCount - 1);
                int rawSecondary = (int)i.biomeData.y;
                int secondaryBiome = (rawSecondary < 0 || rawSecondary >= sliceCount) ? primaryBiome : rawSecondary;
                

                 int primaryBiome = clamp((int)i.biomeData.x, 0, 15);
                int secondaryBiome = primaryBiome;
                float blendFactor = 0.0;




          //      float blendFactor = saturate(i.biomeData.z);

                float3 colPrimaryX = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvX, primaryBiome)).rgb;
                float3 colPrimaryY = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvY, primaryBiome)).rgb;
                float3 colPrimaryZ = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvZ, primaryBiome)).rgb;

                float3 colSecondaryX = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvX, secondaryBiome)).rgb;
                float3 colSecondaryY = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvY, secondaryBiome)).rgb;
                float3 colSecondaryZ = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvZ, secondaryBiome)).rgb;

                float3 blendedX = lerp(colPrimaryX, colSecondaryX, blendFactor);
                float3 blendedY = lerp(colPrimaryY, colSecondaryY, blendFactor);
                float3 blendedZ = lerp(colPrimaryZ, colSecondaryZ, blendFactor);

                float3 finalColor = blendedX * weights.x + blendedY * weights.y + blendedZ * weights.z;

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normalize(i.worldNormal), lightDir));
                float3 litColor = finalColor * _LightColor0.rgb * NdotL;

                float3 col = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvX, 0)); // Sample biome 0 only


                return float4(col, 1.0);
            }

            ENDHLSL
        }
    }
}
*/
Shader "Custom/TriplanarBiome"
{
    Properties
    {
        _Scale("Texture Scale", Float) = 10.0
        _BlendSharpness("Blend Sharpness", Range(1, 10)) = 2
        _BiomeTexArray("Biome Texture Array", 2DArray) = "" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 biomeData : TEXCOORD2; // biomeA, biomeB, blend, unused
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                nointerpolation float biomeIndex : TEXCOORD2;
            };

            float _Scale;
            float _BlendSharpness;
            UNITY_DECLARE_TEX2DARRAY(_BiomeTexArray);

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.biomeData = v.biomeData;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int biomeA = clamp((int)round(i.biomeData.x), 0, 15);
                float blend = saturate(i.biomeData.z);

                // Triplanar UVs
                float2 uvX = frac(i.worldPos.yz * _Scale);
                float2 uvY = frac(i.worldPos.xz * _Scale);
                float2 uvZ = frac(i.worldPos.xy * _Scale);

                float3 weights = pow(abs(normalize(i.worldNormal)), _BlendSharpness);
                weights /= max(weights.x + weights.y + weights.z, 1e-5);

                // Sample biome A
                float3 color =
                    UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvX, biomeA)) * weights.x +
                    UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvY, biomeA)) * weights.y +
                    UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvZ, biomeA)) * weights.z;




                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float NdotL = saturate(dot(normalize(i.worldNormal), lightDir));
                float3 litColor = color * lightColor * NdotL;

                return float4(litColor, 1.0);

            }

            ENDHLSL
        }
    }
}