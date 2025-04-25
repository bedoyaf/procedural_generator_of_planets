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
                nointerpolation float biomeIndex : TEXCOORD2; // Assigned in mesh or procedural code
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
                o.biomeIndex = v.biomeIndex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldPos = i.worldPos * _Scale;
                float2 uvX = worldPos.yz - floor(worldPos.yz);
                float2 uvY = worldPos.xz - floor(worldPos.xz);
                float2 uvZ = worldPos.xy - floor(worldPos.xy);

                float3 normal = abs(normalize(i.worldNormal));
                float3 weights = pow(normal, _BlendSharpness);
                weights /= max(weights.x + weights.y + weights.z, 1e-5);


                int biomeIndex = clamp((int)round(i.biomeIndex), 0, 15); // assuming max 16 layers

                float3 xProj = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvX, biomeIndex)).rgb;
                float3 yProj = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvY, biomeIndex)).rgb;
                float3 zProj = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvZ, biomeIndex)).rgb;

                float3 color = xProj * weights.x + yProj * weights.y + zProj * weights.z;

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