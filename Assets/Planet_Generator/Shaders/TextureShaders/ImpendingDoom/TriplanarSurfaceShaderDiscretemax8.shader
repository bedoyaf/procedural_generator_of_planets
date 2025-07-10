Shader "Custom/TriplanarDiscreteMax8"
{
    Properties
    {
        _Biomes ("Biome Texture Array", 2DArray) = "" {}
        _Scale  ("Texture Scale", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"



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

            float3 GetLocalWorldPos(float3 worldPos)
            {
                return worldPos - _WorldSpaceCameraPos; 
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 uvw = TriplanarUV(GetLocalWorldPos(i.worldPos));
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

 
                float4 shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                float shadow = MainLightRealtimeShadow(shadowCoord);

                MainLight mainLight = GetMainLight(shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normal, lightDir));
                float lit = NdotL * shadow;

                return float4(col.rgb * lit, 1);
            }
            ENDHLSL

        }
    }

    FallBack "Diffuse"
}
