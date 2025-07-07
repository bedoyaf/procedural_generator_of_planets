Shader "Custom/MyBiomeBlendShader4Biomes"
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
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float _Scale;
            UNITY_DECLARE_TEX2DARRAY(_Biomes);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 biomeIndices : TEXCOORD2;
                float4 biomeWeights : TEXCOORD3;
                float3 objectPos : TEXCOORD4;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                nointerpolation float4 biomeIndices : TEXCOORD2;
                float4 biomeWeights : TEXCOORD3;
                float3 objectPos : TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
              //  o.worldNormal = UnityObjectToWorldNormal(v.normal);
              o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                o.biomeIndices = v.biomeIndices;
                o.biomeWeights = v.biomeWeights;
                o.objectPos = v.vertex.xyz;
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

            float3 GetLocalWorldPos(float3 worldPos)
            {
                return worldPos - unity_ObjectToWorld._m03_m13_m23;
            }


            float4 frag(v2f i) : SV_Target
            {
            //    return float4(i.biomeIndices.x/12, i.biomeIndices.y/12,0,0);

                float3 uvw    = triplanarUV(GetLocalWorldPos(i.worldPos));
                float3 normal = normalize(i.worldNormal);

                int index0 = (int)i.biomeIndices.x;
                int index1 = (int)i.biomeIndices.y;
                int index2 = (int)i.biomeIndices.z;
                int index3 = (int)i.biomeIndices.w;

                float weight0 = i.biomeWeights.x;
                float weight1 = i.biomeWeights.y;
                float weight2 = i.biomeWeights.z;
                float weight3 = i.biomeWeights.w;

                float4 color0 = SampleBiome(uvw, normal, index0) * weight0;
                float4 color1 = SampleBiome(uvw, normal, index1) * weight1;
                float4 color2 = SampleBiome(uvw, normal, index2) * weight2;
                float4 color3 = SampleBiome(uvw, normal, index3) * weight3;

                float4 finalColor = color0 + color1 + color2 + color3;

                // Simple Lambert lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir));
                float3 litColor = finalColor.rgb * _LightColor0.rgb * NdotL;


        //        return float4(weight0,weight1,weight2,0);
                
                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Diffuse"
}
