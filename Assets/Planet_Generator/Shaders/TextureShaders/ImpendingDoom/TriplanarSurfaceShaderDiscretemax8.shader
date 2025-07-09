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
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            UNITY_DECLARE_TEX2DARRAY(_Biomes);
            float _Scale;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

                float4 biomeWeights0 : TEXCOORD2; // váhy pro biomy 0–3
                float4 biomeWeights1 : TEXCOORD3; // váhy pro biomy 4–7
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;

                nointerpolation float4 biomeWeights0 : TEXCOORD4;
                nointerpolation float4 biomeWeights1 : TEXCOORD5;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.biomeWeights0 = v.biomeWeights0;
                o.biomeWeights1 = v.biomeWeights1;

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

            float4 frag(v2f i) : SV_Target
            {
                float3 uvw = TriplanarUV(GetLocalWorldPos(i.worldPos));
                float3 normal = normalize(i.worldNormal);

                float biomeWeights[8] = {
                    i.biomeWeights0.x, i.biomeWeights0.y, i.biomeWeights0.z, i.biomeWeights0.w,
                    i.biomeWeights1.x, i.biomeWeights1.y, i.biomeWeights1.z, i.biomeWeights1.w
                };

                float4 col = float4(0,0,0,0);
                for (int j = 0; j < 8; ++j)
                {
                    col += SampleBiome(uvw, normal, j) * biomeWeights[j];
                }

                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, L));
                float3 lit = col.rgb * _LightColor0.rgb * NdotL;

                return float4(lit, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}
