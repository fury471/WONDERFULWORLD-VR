Shader "Wonderland/UI/Notice Board Toon URP"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.78, 0.58, 0.36, 1)

        [Header(Toon Ramp)]
        _ShadowColor ("Shadow Color", Color) = (0.32, 0.22, 0.16, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 0.86, 0.62, 1)
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
        _RampSoftness ("Ramp Softness", Range(0.001, 0.2)) = 0.05
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.4
        _LightInfluence ("Scene Light Influence", Range(0, 1)) = 0.55
        _AmbientFloor ("Scene-Scaled Fill", Range(0, 1)) = 0.22
        _ShadowStrength ("Stylized Shadow Strength", Range(0, 1)) = 0.55
        _FogInfluence ("Fog Influence", Range(0, 1)) = 1

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 0.92, 0.74, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 4.0
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.18

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.08, 0.05, 0.04, 1)
        _OutlineWidth ("Outline Width (pixels)", Range(0, 6)) = 1.4
        _OutlineFadeNear ("Outline Fade Near", Float) = 0.5
        _OutlineFadeFar ("Outline Fade Far", Float) = 25

        [Header(Misc)]
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.45
        [Toggle] _AlphaClip ("Alpha Clip", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
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

        // ----------------------------------------------------------------
        // Outline pass — inverted hull
        // ----------------------------------------------------------------
        Pass
        {
            Name "ToonOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineFadeNear;
                float _OutlineFadeFar;
                half _Cutoff;
                half _AlphaClip;
                half4 _EmissionColor;
                half _EmissionStrength;
            CBUFFER_END

            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                // Smoothed-normal baked by the postprocessor: xyz = (n+1)*0.5, w = 1 if valid.
                // Falls back to NORMAL when w < 0.5 (mesh wasn't postprocessed).
                float4 smoothNormalData : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            OutlineVaryings vertOutline(OutlineAttributes input)
            {
                OutlineVaryings output = (OutlineVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float distToCam = length(GetCameraPositionWS() - posWS);
                float fade = saturate((_OutlineFadeFar - distToCam) / max(_OutlineFadeFar - _OutlineFadeNear, 0.0001));

                // Prefer baked smoothed normals so the inverted-hull-equivalent push is continuous at hard edges.
                float3 normalSourceOS = input.normalOS;
                if (input.smoothNormalData.w > 0.5)
                {
                    normalSourceOS = input.smoothNormalData.xyz * 2.0 - 1.0;
                }
                normalSourceOS = normalize(normalSourceOS);

                // Project the normal into screen-pixel space, then offset positionCS by an explicit pixel count.
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(normalSourceOS);
                float3 normalVS = TransformWorldToViewDir(normalWS);
                float2 normalCS = mul((float2x3)UNITY_MATRIX_P, normalVS);

                // After mul, normalCS is in clip-space-pre-divide units. Convert to screen-pixel direction.
                float2 pixelDir = float2(normalCS.x * _ScreenParams.x, normalCS.y * _ScreenParams.y);
                float pixelLen = max(length(pixelDir), 1e-5);
                pixelDir /= pixelLen;

                // Offset by _OutlineWidth pixels exactly: clip += pixelDir * px * 2/screen * w
                float widthPx = _OutlineWidth * fade;
                positionCS.xy += pixelDir * widthPx * 2.0 * positionCS.w / _ScreenParams.xy;

                output.positionCS = positionCS;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 fragOutline(OutlineVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 color = MixFog(_OutlineColor.rgb, input.fogFactor);
                return half4(color, _OutlineColor.a);
            }
            ENDHLSL
        }

        // ----------------------------------------------------------------
        // Lit pass — toon banded with stylized rim
        // ----------------------------------------------------------------
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
            #pragma multi_compile_instancing

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
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineFadeNear;
                float _OutlineFadeFar;
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

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
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_AlphaClip > 0.5h)
                {
                    clip(albedo.a - _Cutoff);
                }

                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lightDir = normalize(mainLight.direction);

                // Cel ramp
                half ndotl = saturate(dot(normalWS, lightDir));
                half lit = smoothstep(_RampThreshold - _RampSoftness, _RampThreshold + _RampSoftness, ndotl * mainLight.shadowAttenuation);
                half3 ramp = lerp(_ShadowColor.rgb, _HighlightColor.rgb, lit);
                half3 toonLight = lerp(ramp * mainLight.color.rgb, half3(1.0h, 1.0h, 1.0h), _AmbientStrength);

                // Scene-aware wrap shading
                half wrappedNdotL = saturate(dot(normalWS, lightDir) * 0.5h + 0.5h);
                half stylizedShade = lerp(1.0h, wrappedNdotL * mainLight.shadowAttenuation, _ShadowStrength);
                half3 ambient = max(SampleSH(normalWS), 0.0h.xxx);
                half3 sceneLight = ambient * 0.7h + mainLight.color.rgb * stylizedShade;
                half sceneEnergy = saturate(
                    dot(ambient, half3(0.2126h, 0.7152h, 0.0722h)) +
                    dot(mainLight.color.rgb, half3(0.2126h, 0.7152h, 0.0722h)) * 0.25h);
                sceneLight = max(sceneLight, (_AmbientFloor * sceneEnergy).xxx);

                half3 color = albedo.rgb * lerp(toonLight, sceneLight, _LightInfluence);

                // Painted rim
                half3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                half rim = pow(saturate(1.0h - dot(normalWS, viewDirWS)), _RimPower);
                rim *= smoothstep(0.0h, 0.6h, ndotl * mainLight.shadowAttenuation);
                color += _RimColor.rgb * rim * _RimStrength;

                // Emission
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb * _EmissionStrength;
                color += emission;

                color = lerp(color, MixFog(color, input.fogFactor), _FogInfluence);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // ----------------------------------------------------------------
        // Shadow caster
        // ----------------------------------------------------------------
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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

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
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineFadeNear;
                float _OutlineFadeFar;
                half _Cutoff;
                half _AlphaClip;
                half4 _EmissionColor;
                half _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
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
