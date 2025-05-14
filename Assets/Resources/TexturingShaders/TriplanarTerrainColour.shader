Shader "Custom/TriplanarBiomeColor"
{
    Properties
    {
        _Scale("Triplanar Scale", Float) = 10.0
        _BlendSharpness("Blend Sharpness", Range(1, 10)) = 2
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
                float4 biomeData : TEXCOORD2; // x = biomeA, y = biomeB, z = blend
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                nointerpolation float4 biomeData : TEXCOORD2;
            };

            float _Scale;
            float _BlendSharpness;

            // Replace texture array with color array passed from C#
            StructuredBuffer<float4> _BiomeColors;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.biomeData = v.biomeData;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                int biomeA = clamp((int)round(i.biomeData.x), 0, 255);
                int biomeB = clamp((int)round(i.biomeData.y), 0, 255);
                float blend = saturate(i.biomeData.z);

                float3 normal = normalize(i.worldNormal);
                float3 weights = pow(abs(normal), _BlendSharpness);
                weights /= max(dot(weights, 1.0), 1e-5);

                // Fetch biome colors from the C# provided buffer
                float3 colorA = _BiomeColors[biomeA].rgb;
                float3 colorB = _BiomeColors[biomeB].rgb;
                float3 color = lerp(colorA, colorB, blend);

                // Light calculation
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float NdotL = saturate(dot(normal, lightDir));
                float3 litColor = color * lightColor * NdotL;

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }
}
