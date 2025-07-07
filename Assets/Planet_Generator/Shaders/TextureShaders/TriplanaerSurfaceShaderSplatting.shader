Shader "Custom/MyBiomeBlendShader4BiomesSplatting"
{
    Properties
    {
        // These two replace your per-vertex data
        _IndexMap ("Biome Index Map (RGBA)", 2D) = "black" {}
        _WeightMap ("Biome Weight Map (RGBA)", 2D) = "black" {}
        
        _Biomes ("Biome Texture Array", 2DArray) = "" {}
        _Scale ("Texture Scale", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _IndexMap;
            sampler2D _WeightMap;
            UNITY_DECLARE_TEX2DARRAY(_Biomes);
            float _Scale;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0; // We only need the primary UV now!
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2; // Pass the primary UV to the fragment shader
                float3 objectPos : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv; // Pass the mesh UVs
                o.objectPos = v.vertex.xyz;
                return o;
            }
            
            // Your triplanar function is good
            float4 SampleBiome(float3 uvw, float3 normal, float index)
            {
          //      index = floor(index + 0.5); 

                float3 blendWeights = saturate(abs(normal));
                blendWeights /= max(dot(blendWeights, 1.0), 1e-5);

                float4 xTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.yz, index));
                float4 yTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.xz, index));
                float4 zTex = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.xy, index));

                return xTex * blendWeights.x + yTex * blendWeights.y + zTex * blendWeights.z;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 1. Sample the control maps using the interpolated mesh UV
                float4 indicesFloat = tex2D(_IndexMap, i.uv);
                float4 weights = tex2D(_WeightMap, i.uv);
                
                // The indices are stored as colors [0,1], so scale them back to [0,255]
               // indices *= 255.0f;
                float index0 = floor(indicesFloat.r * 255.0f + 0.5f); 
                float index1 = floor(indicesFloat.g * 255.0f + 0.5f);
                float index2 = floor(indicesFloat.b * 255.0f + 0.5f);
                float index3 = floor(indicesFloat.a * 255.0f + 0.5f);

                // 2. Perform the triplanar sampling and blending
                float3 uvw = i.objectPos * _Scale; // Or use worldPos if you prefer
                float3 normal = normalize(i.worldNormal);
                
                float4 color0 = SampleBiome(uvw, normal, index0) * weights.r;
                float4 color1 = SampleBiome(uvw, normal, index1) * weights.g;
                float4 color2 = SampleBiome(uvw, normal, index2) * weights.b;
                float4 color3 = SampleBiome(uvw, normal, index3) * weights.a;

                float4 finalColor = color0 + color1 + color2 + color3;
                
                // 3. Apply lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir));
                float3 litColor = finalColor.rgb * _LightColor0.rgb * NdotL;


               // return float4(indices.r / 255.0, 0, 0, 1); // Kolik je index R

        //        return float4(indices.r / 255.0, weights.r, 0, 1);

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}