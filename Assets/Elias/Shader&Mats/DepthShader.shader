Shader "Custom/URPDepthParallax"
{
    Properties
    {
        [MainTexture] _BaseMap("Color Texture", 2D) = "white" {}
        _DepthMap("Height/Depth Texture", 2D) = "gray" {}
        _HeightScale("Height Scale", Range(0,1)) = 0.04
        _MinSteps("Min Steps", Range(4,32)) = 8
        _MaxSteps("Max Steps", Range(8,128)) = 32
        _RefineSteps("Binary Refine Steps", Range(0,8)) = 4
        [Toggle] _InvertDepth("Invert Depth", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDirTS : TEXCOORD1;
            };

            // Textures & samplers
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DepthMap);
            SAMPLER(sampler_DepthMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DepthMap_ST;
                float _HeightScale;
                float _MinSteps;
                float _MaxSteps;
                float _RefineSteps;
                float _InvertDepth;
            CBUFFER_END

            float SampleHeight(float2 uv)
            {
                float h = SAMPLE_TEXTURE2D(_DepthMap, sampler_DepthMap, uv).r;
                return lerp(h, 1.0 - h, saturate(_InvertDepth));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);

                // Tiling/offset
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                // TBN for view direction
                float3 nWS = normalize(TransformObjectToWorldNormal(IN.normal));
                float3 tWS = normalize(TransformObjectToWorldDir(IN.tangent.xyz));
                float3 bWS = cross(nWS, tWS) * IN.tangent.w;
                float3x3 TBN = float3x3(tWS, bWS, nWS);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - worldPos);
                OUT.viewDirTS = mul(TBN, viewDirWS);

                return OUT;
            }

            // Parallax occlusion (simplified forward march + optional binary refinement)
            float2 ParallaxUV(float2 uv, float3 viewDirTS)
{
    viewDirTS = normalize(viewDirTS);

    // Use a fixed number of steps for GPU-friendly loops
    const int kMaxSteps = 32;

    float2 parallaxDir = -viewDirTS.xy / max(viewDirTS.z, 1e-4);
    float2 deltaUV = parallaxDir * (_HeightScale / kMaxSteps);

    float2 uvCurr = uv;
    float layerHeight = 1.0;
    float layerDelta = 1.0 / kMaxSteps;

    // Fixed-count loop avoids gradient warnings
    [loop]
    for (int i = 0; i < kMaxSteps; i++)
    {
        float h = SampleHeight(uvCurr);
        if (layerHeight <= h) break;   // intersection found
        uvCurr += deltaUV;
        layerHeight -= layerDelta;
    }

    return uvCurr;
}



            half4 frag(Varyings IN) : SV_Target
            {
                float2 uvP = ParallaxUV(IN.uv, IN.viewDirTS);
                uvP = saturate(uvP); // clamp to [0,1]
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvP);
            }
            ENDHLSL
        }
    }
}
