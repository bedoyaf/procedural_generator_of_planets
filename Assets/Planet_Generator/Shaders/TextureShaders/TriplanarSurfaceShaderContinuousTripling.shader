Shader "Custom/TriplanarContinuousTripling"
{
    Properties
    {
        _Biomes ("Biome Texture Array", 2DArray) = "" {}
        _Scale ("Texture Scale", Float) = 1.0
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
            #pragma target 3.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _Scale;
            TEXTURE2D_ARRAY(_Biomes);
            SAMPLER(sampler_Biomes);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 biomeIndices : TEXCOORD2;
                float4 biomeWeights : TEXCOORD3;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                nointerpolation float4 biomeIndices : TEXCOORD2;
                float4 biomeWeights : TEXCOORD3;
                float3 localPos : TEXCOORD4;
            };

            v2f vert(appdata v)
            {
           //     o.worldNormal = TransformObjectToWorldNormal(v.normal);
                v2f o;
                 float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.worldPos = worldPos;
                o.pos = TransformWorldToHClip(worldPos);
                o.localPos = v.vertex.xyz; // lokální (object space) pozice
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.biomeIndices = v.biomeIndices;
                o.biomeWeights = v.biomeWeights;
                return o;
            }

            float3 triplanarUV(float3 localPos)
            {
                return localPos * _Scale;
            }

            float4 SampleBiome(float3 uvw, float3 normal, int index)
            {
                float3 blendWeights = saturate(abs(normal));
                blendWeights /= max(dot(blendWeights, 1.0), 1e-5);

                float4 x = SAMPLE_TEXTURE2D_ARRAY(_Biomes, sampler_Biomes, uvw.yz, index);
                float4 y = SAMPLE_TEXTURE2D_ARRAY(_Biomes, sampler_Biomes, uvw.xz, index);
                float4 z = SAMPLE_TEXTURE2D_ARRAY(_Biomes, sampler_Biomes, uvw.xy, index);

                return x * blendWeights.x + y * blendWeights.y + z * blendWeights.z;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 uvw = triplanarUV(i.localPos);
                float3 normal = normalize(i.worldNormal);

                int indices[4] = {
                    (int)i.biomeIndices.x,
                    (int)i.biomeIndices.y,
                    (int)i.biomeIndices.z,
                    (int)i.biomeIndices.w
                };

                float weights[4] = {
                    i.biomeWeights.x,
                    i.biomeWeights.y,
                    i.biomeWeights.z,
                    i.biomeWeights.w
                };

                float4 finalColor = float4(0, 0, 0, 0);

                for (int k = 0; k < 4; ++k)
                {
                    int index = indices[k];
                    float weight = weights[k];

                    if (index >= 0 && weight > 0.0001)
                    {
                        finalColor += SampleBiome(uvw, normal, index) * weight;
                    }
                }

                // Jednoduché osvìtlení (Lambert)
                                InputData inputData;
                inputData.positionWS = i.worldPos;
                inputData.normalWS = normal;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - i.worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                inputData.fogCoord = ComputeFogFactor(i.pos.z);
                inputData.vertexLighting = float3(0, 0, 0); // optional
                inputData.bakedGI = float3(0, 0, 0);        // optional
                inputData.normalizedScreenSpaceUV = float2(0, 0);
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = finalColor.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.metallic = 0.0;
                surfaceData.specular = float3(0.2, 0.2, 0.2); // Blinn-Phong expects this
                surfaceData.smoothness = 0.5;
                surfaceData.occlusion = 1.0;
                surfaceData.emission = float3(0, 0, 0);
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                float4 litColor = UniversalFragmentBlinnPhong(inputData, surfaceData);
                return litColor;
            }

            ENDHLSL
        }
    }

    FallBack "Diffuse"
}
