Shader "Custom/BiomBlendingGragmentWeights"
{
    Properties
    {
        _Biomes("Biome Texture Array", 2DArray) = "" {}
        _Scale("Texture Scale", Float) = 1.0
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
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float _Scale;
            UNITY_DECLARE_TEX2DARRAY(_Biomes);

            // Max biomes, can increase if needed
            #define MAX_BIOMES 8
            float4 _BiomeData[MAX_BIOMES]; // (center, range, heightAffinity, slopeAffinity)
            int _BiomeCount;
            float _BlendDistance;
            float _HeightBlendMin;
            float _HeightBlendMax;
            float _HeightBlendCurve;
            float3 _PlanetCenter;
            float4 _BiomeSlopeData[MAX_BIOMES];


            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 objectPos : TEXCOORD2;
                float height : TEXCOORD3;
                float slope : TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.objectPos = v.vertex.xyz;

                float3 normal = UnityObjectToWorldNormal(v.normal);
                float3 planetUp = normalize(o.worldPos - _PlanetCenter);
                o.height = length(o.worldPos - _PlanetCenter) - 1;
                o.slope = 1.0 - abs(dot(normalize(normal), planetUp));
                return o;
            }

            float3 triplanarUV(float3 objectPos)
            {
                return objectPos * _Scale;
            }

            float4 SampleBiome(float3 uvw, float3 normal, int index)
            {
                float3 blendWeights = saturate(abs(normal));
                blendWeights /= max(dot(blendWeights, 1.0), 1e-5);

                float4 xTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.yz, index));
                float4 yTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.xz, index));
                float4 zTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.xy, index));

                return xTex * blendWeights.x + yTex * blendWeights.y + zTex * blendWeights.z;
            }

            float4 frag(v2f i) : SV_Target
            {
                float height = i.height;
                float slope = i.slope * 90;
           //     float height = length(i.worldPos - _PlanetCenter) - 1;
                float3 planetUp = normalize(i.worldPos - _PlanetCenter); // Local up direction from planet center
             //   float slope = 1.0 - abs(dot(normalize(i.worldNormal), planetUp));
                float3 uvw = triplanarUV(i.objectPos);
                float3 normal = normalize(i.worldNormal);

                float totalScore = 0;
                float4 weights = 0;
                int4 indices = 0;

                slope = slope*90;
                for (int j = 0; j < _BiomeCount; j++)
                {

                    float4 data = _BiomeData[j];
                    float heightCenter = data.x;
                    float heightRange = data.y;
                    float heightAffinity = data.z;
                    float slopeAffinity = data.w;

                //    float slopeBlendFactor = lerp(_HeightBlendMin, _HeightBlendMax, pow(slope, _HeightBlendCurve));
                //    float effectiveRange = heightrange * _blenddistance * slopeBlendFactor + 1e-5;
                    float slopeMin = _BiomeSlopeData[j].x;
                    float slopeMax = _BiomeSlopeData[j].y;
                    float slopeCenter = (slopeMin + slopeMax) * 0.5;
                    float slopeRange = (slopeMax - slopeMin) * 0.5;

                    float slopeScore = saturate(1.0 - abs(slope - slopeCenter) / (slopeRange * _BlendDistance + 1e-5));

                    // final score
   

                //    return float4(float3(slopeMin/90,0,0), 1.0);



                  //  float slopeScore = saturate(1.0 - abs(slope - slopeCenter) / (slopeRange * _BlendDistance + 1e-5));

                    float heightScore = saturate(1.0 - abs(height - heightCenter) / (heightRange * _BlendDistance)/*effectiveRange*/);


                    float score = heightScore * heightAffinity /*+ slopeScore * slopeAffinity*2*/; 
                 //   return float4(float3(heightScore,0,0), 1.0);

                 //   float score = heightScore * 1 /*+ slope * slopeAffinity*/;

                    if (score > weights.x)
                    {
                        weights = float4(score, weights.x, weights.y, weights.z);
                        indices = int4(j, indices.x, indices.y, indices.z);
                    }
                    else if (score > weights.y)
                    {
                        weights = float4(weights.x, score, weights.y, weights.z);
                        indices = int4(indices.x, j, indices.y, indices.z);
                    }
                    else if (score > weights.z)
                    {
                        weights = float4(weights.x, weights.y, score, weights.z);
                        indices = int4(indices.x, indices.y, j, indices.z);
                    }
                    else if (score > weights.w)
                    {
                        weights.w = score;
                        indices.w = j;
                    }
                }

                weights /= max(dot(weights, 1.0), 1e-6);

                float4 col0 = SampleBiome(uvw, normal, indices.x) * weights.x;
                float4 col1 = SampleBiome(uvw, normal, indices.y) * weights.y;
                float4 col2 = SampleBiome(uvw, normal, indices.z) * weights.z;
                float4 col3 = SampleBiome(uvw, normal, indices.w) * weights.w;

                float4 finalColor = col0 + col1 + col2 + col3;

                // Simple Lambert lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir));
                float3 litColor = finalColor.rgb * _LightColor0.rgb * NdotL;

                return float4(litColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
