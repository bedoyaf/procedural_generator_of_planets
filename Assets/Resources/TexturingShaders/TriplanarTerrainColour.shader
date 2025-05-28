Shader "Custom/MyBiomeBlendShader"
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

            // Correct HLSL declaration
            float _Scale;
            float _BlendSharpness;
            UNITY_DECLARE_TEX2DARRAY(_Biomes);


            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 biomeIndices : TEXCOORD2; // x = primary, y = secondary
                float4 biomeWeights : TEXCOORD3; // x = primary weight, y = secondary weight
                float3 objectPos : TEXCOORD4; // Add object-space position
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                nointerpolation float4 biomeIndices : TEXCOORD2;
              /*  nointerpolation*/ float4 biomeWeights : TEXCOORD3;
                float3 objectPos : TEXCOORD4; // Add object-space position
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.biomeIndices = v.biomeIndices;
                o.biomeWeights = v.biomeWeights;
                o.objectPos = v.vertex.xyz; // Pass the object-space vertex position
                return o;
            }

            float3 triplanarUV(float3 objectPos)
            {
                return objectPos * _Scale;
            }

            float4 SampleBiome(float3 uvw, float3 normal, float index, float primaryWeight) // Added primaryWeight
            {
                float3 blendWeights = saturate(abs(normal));
                blendWeights /= dot(blendWeights, 1.0);

                float3 uv = uvw;
                float4 xTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uv.yz, (int)index));
                float4 yTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uv.xz, (int)index));
                float4 zTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uv.xy, (int)index));

                float blendThreshold = 0; // Adjust this threshold as needed

              //  if (primaryWeight > blendThreshold)
              //  {
                    return xTex * blendWeights.x + yTex * blendWeights.y + zTex * blendWeights.z;
               // }
              /*  else
                {
                   return (xTex + yTex + zTex) / 3.0;
                }*/
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 uvw = triplanarUV(i.objectPos);
                float biomeA = i.biomeIndices.x;
                float biomeB = i.biomeIndices.y;
                float weightA = i.biomeWeights.x;
                float weightB = i.biomeWeights.y;

                float4 colA = SampleBiome(uvw, i.worldNormal, biomeA, weightA); // Pass weightA
                float4 colB = SampleBiome(uvw, i.worldNormal, biomeB, weightB); // Pass weightB


                float4 color = lerp(colB, colA, weightA / (weightA + weightB + 1e-5));

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float NdotL = saturate(dot(normalize(i.worldNormal), lightDir));
                float3 litColor = color * lightColor * NdotL;

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}