Shader "Wonderland/Props/Toon Band Lit URP"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.28, 0.22, 0.18, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 0.86, 0.62, 1)
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.48
        _RampSoftness ("Ramp Softness", Range(0.001, 0.2)) = 0.018
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.38
        _LightInfluence ("Scene Light Influence", Range(0, 1)) = 0.48
        _AmbientFloor ("Scene-Scaled Fill", Range(0, 1)) = 0.18
        _ShadowStrength ("Stylized Shadow Strength", Range(0, 1)) = 0.52
        _FogInfluence ("Fog Influence", Range(0, 1)) = 1
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.45
        _AlphaClip ("Alpha Clip", Float) = 0
        _Cull ("Cull", Float) = 2
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _HighlightColor;
                half _RampThreshold;
                half _RampSoftness;
                half _AmbientStrength;
                half _LightInfluence;
                half _AmbientFloor;
                half _ShadowStrength;
                half _FogInfluence;
                half _Cutoff;
                half _AlphaClip;
                half4 _EmissionColor;
                half _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_AlphaClip > 0.5h)
                {
                    clip(albedo.a - _Cutoff);
                }

                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndotl = saturate(dot(normalWS, normalize(mainLight.direction)));
                half lit = smoothstep(_RampThreshold - _RampSoftness, _RampThreshold + _RampSoftness, ndotl * mainLight.shadowAttenuation);
                half3 ramp = lerp(_ShadowColor.rgb, _HighlightColor.rgb, lit);
                half3 toonLight = lerp(ramp * mainLight.color.rgb, half3(1.0h, 1.0h, 1.0h), _AmbientStrength);

                half wrappedNdotL = saturate(dot(normalWS, normalize(mainLight.direction)) * 0.5h + 0.5h);
                half stylizedShade = lerp(1.0h, wrappedNdotL * mainLight.shadowAttenuation, _ShadowStrength);
                half3 ambient = max(SampleSH(normalWS), 0.0h.xxx);
                half3 sceneLight = ambient * 0.65h + mainLight.color.rgb * stylizedShade;
                half sceneEnergy = saturate(
                    dot(ambient, half3(0.2126h, 0.7152h, 0.0722h)) +
                    dot(mainLight.color.rgb, half3(0.2126h, 0.7152h, 0.0722h)) * 0.25h);
                sceneLight = max(sceneLight, (_AmbientFloor * sceneEnergy).xxx);

                half3 color = albedo.rgb * lerp(toonLight, sceneLight, _LightInfluence);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb * _EmissionStrength;
                color += emission;
                color = lerp(color, MixFog(color, input.fogFactor), _FogInfluence);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _AlphaClip;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_AlphaClip > 0.5h)
                {
                    clip(albedo.a - _Cutoff);
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
