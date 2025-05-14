Shader "Custom/TriplanarBiomeGradient"
{
    Properties
    {
        _Scale("Texture Scale", Float) = 10.0
        _BlendSharpness("Blend Sharpness", Range(1, 10)) = 2
        _BiomeGradient("Biome Gradient", 2D) = "white" {}
        _BaseRadius("Base Radius", Float) = 1.0
        _MaxHeight("Max Height", Float) = 1.2
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

            sampler2D _BiomeGradient;
            float _Scale;
            float _BlendSharpness;
            float _BaseRadius;
            float _MaxHeight;

            StructuredBuffer<float> _VertexHeights;
            int _VertexCount;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                uint vertexIndex : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.vertexIndex = v.vertexID;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                int idx = i.vertexIndex;
                float rawHeight = (idx < _VertexCount) ? _VertexHeights[idx] : 0.0;
               
               // return float4(rawHeight.xxx, 1);
               float heightNormalized = saturate((rawHeight - _BaseRadius) / (_MaxHeight - _BaseRadius));
            //   return float4(heightNormalized.xxx, 1);
               
                float3 biomeColor = tex2D(_BiomeGradient, float2(heightNormalized, 0)).rgb;
             //   return biomeColor;
                
                // Triplanar blend
                float3 worldPos = i.worldPos * _Scale;
                float2 uvX = worldPos.yz - floor(worldPos.yz);
                float2 uvY = worldPos.xz - floor(worldPos.xz);
                float2 uvZ = worldPos.xy - floor(worldPos.xy);

                float3 normal = abs(normalize(i.worldNormal));
                float3 weights = pow(normal, _BlendSharpness);
                weights /= max(dot(weights, float3(1,1,1)), 1e-5);

                float3 blendedColor = biomeColor * (weights.x + weights.y + weights.z);

                // Apply simple lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float NdotL = saturate(dot(normalize(i.worldNormal), lightDir));
                float3 litColor = blendedColor * lightColor * NdotL;

             //   return float4(rawHeight.xxx, 1);
                return float4(litColor, 1.0);
            }
            ENDHLSL
        }
    }
}
