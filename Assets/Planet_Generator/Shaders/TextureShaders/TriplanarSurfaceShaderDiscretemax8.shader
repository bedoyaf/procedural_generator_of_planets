 Shader "Custom/TriplanarDiscreteMax8"
{
    Properties
    {
        _Biomes ("Biome Texture Array", 2DArray) = "" {}
        _Scale  ("Texture Scale", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
            TEXTURE2D_ARRAY(_Biomes);
            SAMPLER(sampler_Biomes);
            float _Scale;

            struct appdata
            {
                float4 vertex       : POSITION;
                float3 normal       : NORMAL;
                float4 biomeWeights0: TEXCOORD2;
                float4 biomeWeights1: TEXCOORD3;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 objectPos   : TEXCOORD2; 
                float4 biomeWeights0 : TEXCOORD4;
                float4 biomeWeights1 : TEXCOORD5;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);

                o.pos = TransformWorldToHClip(worldPos);
                o.worldPos    = worldPos;
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.objectPos   = v.vertex.xyz; 

                o.biomeWeights0 = v.biomeWeights0;
                o.biomeWeights1 = v.biomeWeights1;
                return o;
            }

            float3 TriplanarUV(float3 objPos) { return objPos * _Scale; }

            float4 SampleBiome(float3 uvw, float3 nrm, int layer)
            {
                float3 bw = saturate(abs(nrm));
                bw /= max(dot(bw, 1.0), 1e-5);

                float4 x = SAMPLE_TEXTURE2D_ARRAY(_Biomes, sampler_Biomes, uvw.yz, layer);
                float4 y = SAMPLE_TEXTURE2D_ARRAY(_Biomes, sampler_Biomes, uvw.xz, layer);
                float4 z = SAMPLE_TEXTURE2D_ARRAY(_Biomes, sampler_Biomes, uvw.xy, layer);

                return x * bw.x + y * bw.y + z * bw.z;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 uvw = TriplanarUV(i.objectPos); 
                float3 normal = normalize(i.worldNormal);

                float biomeWeights[8] = {
                    i.biomeWeights0.x, i.biomeWeights0.y, i.biomeWeights0.z, i.biomeWeights0.w,
                    i.biomeWeights1.x, i.biomeWeights1.y, i.biomeWeights1.z, i.biomeWeights1.w
                };

                float4 col = float4(0,0,0,0);
                [unroll(8)]
                for (int j = 0; j < 8; ++j)
                {
                    col += SampleBiome(uvw, normal, j) * biomeWeights[j];
                }

                 //taken from https://youtu.be/1bm0McKAh9E?si=yNkCzr3JfheEN7mk adjusted with chat gpt and personal tweaks
                InputData inputData;
                inputData.positionWS = i.worldPos;
                inputData.normalWS = normal;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - i.worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                inputData.fogCoord = ComputeFogFactor(i.pos.z);
                inputData.vertexLighting = float3(0, 0, 0); 
                inputData.bakedGI = float3(0, 0, 0);        
                inputData.normalizedScreenSpaceUV = float2(0, 0);
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = col.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.metallic = 0.0;
                surfaceData.specular = float3(0.2, 0.2, 0.2); 
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