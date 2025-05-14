Shader "Custom/ScreenSpaceOcean"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OceanColor ("Ocean Color", Color) = (0, 0.3, 0.7, 1)
        _OceanCenter ("Ocean Center", Vector) = (0,0,0,0)
        _OceanRadius ("Ocean Radius", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            float4 _OceanColor;
            float4 _OceanCenter;
            float _OceanRadius;

            float3 RayDirection(float2 uv)
            {
                float4 clip = float4(uv * 2 - 1, 0, 1);
                float4 view = mul(unity_CameraInvProjection, clip);
                view.xyz /= view.w;
                return normalize(view.xyz);
            }

            float2 RaySphereIntersect(float3 center, float radius, float3 rayOrigin, float3 rayDir)
            {
                float3 offset = rayOrigin-center;
                const float a = 1;
                float b = 2*dot(offset,rayDir);
                float c = dot(offset,offset)-radius*radius;
                float discriminant = b*b-4*a*c;

                if(discriminant>0)
                {
                    float s = sqrt(discriminant);
                    float dstToSphereNear = max(0,(-b-s)/(2*a));
                    float dstToSPhereFar = (-b+s)/(2*a);

                    if(dstToSPhereFar>=0)
                    {
                        return float2(dstToSphereNear,dstToSPhereFar-dstToSphereNear);
                    }
                }
                return float2(100000,0);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Scene color
                fixed4 originalCol = tex2D(_MainTex, uv);

                // Depth
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float linearDepth = LinearEyeDepth(depth);

                // Reconstruct view ray
                float3 viewDir = RayDirection(uv);
                float3 camPos = _WorldSpaceCameraPos;

                // Ray-sphere intersection
                float2 hit = RaySphereIntersect(_OceanCenter.xyz, _OceanRadius, camPos, viewDir);

                if (hit.x < 0) return originalCol;

                float oceanDepth = hit.y;
                float sceneDepth = linearDepth * length(viewDir);

                float oceanViewDepth = min(oceanDepth, sceneDepth - hit.x);

                if (oceanViewDepth > 0)
                {
                    float depthFactor = saturate(oceanViewDepth / 5.0); // controls fade by depth
                    return lerp(originalCol, _OceanColor, depthFactor);
                }

                return originalCol;
            }

            ENDCG
        }
    }
    FallBack Off
}
