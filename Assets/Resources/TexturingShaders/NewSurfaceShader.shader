Shader "Custom/BiomeTriplanarShaderSurfaceZmrd"
{
    Properties
    {
        _Scale ("Triplanar Scale", Float) = 10.0
        _BiomeTexArray ("Biome Texture Array", 2DArray) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            float _Scale;
            UNITY_DECLARE_TEX2DARRAY(_BiomeTexArray);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 texcoord2 : TEXCOORD2; // biome index in texcoord2.x
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float biomeIndex : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.biomeIndex = v.texcoord2.x;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = abs(normalize(i.worldNormal));
                float3 weights = normal / max(dot(normal, 1.0), 1e-5);

                float2 uvX = frac(i.worldPos.yz * _Scale);
                float2 uvY = frac(i.worldPos.xz * _Scale);
                float2 uvZ = frac(i.worldPos.xy * _Scale);

               // float biome = i.biomeIndex;
               int biome = (int)(i.biomeIndex +0.5); // Nearest integer

                float3 col =
                    UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvX, biome)).rgb * weights.x +
                    UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvY, biome)).rgb * weights.y +
                    UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, float3(uvZ, biome)).rgb * weights.z;

                return float4(col, 1.0);
            }

            ENDCG
        }
    }
}
