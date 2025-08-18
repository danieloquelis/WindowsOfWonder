Shader "Custom/Bubble"
{
    Properties
    {
        _SceneTex("Scene (Passthrough)", 2D) = "white" {}
        _Thickness("Film Thickness", Range(0.1, 2.0)) = 1.0
        _RefractStrength("Refraction Strength", Range(0.0, 0.1)) = 0.05
        _ReflectStrength("Reflection Strength", Range(0.0, 1)) = 0.8
        _RainbowIntensity("Rainbow Intensity", Range(0, 2)) = 1.0
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3.0
        _FresnelStrength("Fresnel Strength", Range(0, 2)) = 1.0
        _SpecColor("Specular Color", Color) = (1,1,1,1)
        _Shininess("Shininess", Range(8,128)) = 32
        _LightAlphaInfluence("Light Alpha Influence", Range(-1,1)) = 0.5
        _TintColor("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS: TEXCOORD1;
                float2 uv       : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            sampler2D _SceneTex;
            float4 _SceneTex_TexelSize;
            float _Thickness;
            float _RefractStrength;
            float _ReflectStrength;
            float _RainbowIntensity;
            float _FresnelPower;
            float _FresnelStrength;
            float4 _SpecColor;
            float _Shininess;
            float _LightAlphaInfluence;
            float4 _TintColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.normalWS = normalize(TransformObjectToWorldNormal(v.normal));
                o.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);
                o.uv = v.uv;
                return o;
            }

            float3 ThinFilmRainbow(float3 normal, float3 viewDir, float intensity, float thickness)
            {
                float ndotv = saturate(dot(normal, viewDir));
                float shift = ndotv * thickness * 20.0; // film interference shift
                float r = 0.5 + 0.5 * sin(shift);
                float g = 0.5 + 0.5 * sin(shift + 2.094); // 120°
                float b = 0.5 + 0.5 * sin(shift + 4.188); // 240°
                return float3(r,g,b) * intensity;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);

                // Main directional light
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);

                // Diffuse factor
                float NdotL = saturate(dot(N, L));

                // Specular (Blinn-Phong) – always fully visible
                float spec = pow(saturate(dot(N, H)), _Shininess);
                float3 specular = spec * _SpecColor.rgb * mainLight.color.rgb;

                // Fresnel effect
                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower) * _FresnelStrength;

                // Refraction UV
                float2 refOffset = N.xy * _RefractStrength;
                float2 refrUV = i.uv + refOffset;

                // Passthrough sample
                float3 sceneCol = tex2D(_SceneTex, refrUV).rgb;

                // Rainbow colors
                float3 rainbow = ThinFilmRainbow(N, V, _RainbowIntensity, _Thickness);

                // Rainbow stronger in shadow, weaker in light
                float rainbowWeight = lerp(1.0, 0.3, NdotL);

                // Scene contribution stronger in shadow
                float sceneWeight = lerp(1.0, 0.5, NdotL);

                // Apply tint before adding specular
                float3 tinted = (sceneCol * sceneWeight * _ReflectStrength + rainbow * fresnel * rainbowWeight) * _TintColor.rgb;

                // Final color = tinted + specular
                float3 color = tinted + specular;

                // Alpha controlled by Fresnel + diffuse light influence + specular
                float alpha = clamp(fresnel + NdotL * _LightAlphaInfluence + spec, 0.0, 1.0);

                return float4(color, alpha);
            }

            ENDHLSL
        }
    }
}
