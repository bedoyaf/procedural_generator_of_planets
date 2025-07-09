Shader "Custom/TriplanarDiscreteTripling"
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
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            UNITY_DECLARE_TEX2DARRAY(_Biomes);
            float _Scale;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;

                float3 biomeIndices : TEXCOORD2;   
                float3 biomeWeights : TEXCOORD3;  

                float3 objPos : TEXCOORD4;        
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;

                nointerpolation float3 biomeIndices : TEXCOORD2;
                float3            biomeWeights      : TEXCOORD3;

                float3 objPos : TEXCOORD4;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                o.biomeIndices = v.biomeIndices;
                o.biomeWeights = v.biomeWeights;
                o.objPos       = v.objPos;
                return o;
            }

            float3 TriplanarUV(float3 objPos) { return objPos * _Scale; }

            float4 SampleBiome(float3 uvw, float3 nrm, int layer)
            {
                float3 bw = saturate(abs(nrm));
                bw /= max(dot(bw, 1.0), 1e-5);

                float4 x = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.yz, layer));
                float4 y = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.xz, layer));
                float4 z = UNITY_SAMPLE_TEX2DARRAY(_Biomes, float3(uvw.xy, layer));

                return x * bw.x + y * bw.y + z * bw.z;
            }

            float3 GetLocalWorldPos(float3 worldPos)
            {
                return worldPos - unity_ObjectToWorld._m03_m13_m23;
            }

            float4 frag (v2f i) : SV_Target
            {
                
                float3 uvw    = TriplanarUV(GetLocalWorldPos(i.worldPos));
                float3 normal = normalize(i.worldNormal);

                int   idx0 = (int)i.biomeIndices.x;
                int   idx1 = (int)i.biomeIndices.y;
                int   idx2 = (int)i.biomeIndices.z;

                float w0 = i.biomeWeights.x;
                float w1 = i.biomeWeights.y;
                float w2 = i.biomeWeights.z;

                float4 col =
                      SampleBiome(uvw, normal, idx0) * w0 +
                      SampleBiome(uvw, normal, idx1) * w1 +
                      SampleBiome(uvw, normal, idx2) * w2;

                float3 L   = normalize(_WorldSpaceLightPos0.xyz);
                float  NdotL = saturate(dot(normal, L));
                float3 lit = col.rgb * _LightColor0.rgb * NdotL;

                return float4(lit, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}
