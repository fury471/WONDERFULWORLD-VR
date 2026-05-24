Shader "Wonderland/Vegetation/Ground Petal Detail URP"
{
    Properties
    {
        _BaseMap("Petal Scatter Texture", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (1, 0.94, 0.96, 1)
        _GroundBlendColor("Ground Blend Color", Color) = (0.62, 0.54, 0.45, 1)
        _GroundBlendStrength("Ground Blend Strength", Range(0, 1)) = 0.08
        _LightInfluence("Light Influence", Range(0, 1)) = 0.36
        _AmbientFloor("Scene-Scaled Fill", Range(0, 1)) = 0.24
        _ShadowStrength("Stylized Shadow Strength", Range(0, 1)) = 0.28
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.08
        _GroundOffset("Ground Offset", Range(0, 0.08)) = 0.018
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _TintColor;
                half4 _GroundBlendColor;
                half _GroundBlendStrength;
                half _LightInfluence;
                half _AmbientFloor;
                half _ShadowStrength;
                half _Cutoff;
                half _GroundOffset;
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
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 flatPositionOS = float3(input.positionOS.x, _GroundOffset, input.positionOS.y);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(flatPositionOS);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                output.normalWS = TransformObjectToWorldNormal(float3(0, 1, 0));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(tex.a - 0.01h);

                half4 color = tex * _TintColor;
                color.rgb = lerp(color.rgb, _GroundBlendColor.rgb, _GroundBlendStrength * (1.0h - tex.a));

                Light mainLight = GetMainLight();
                half3 normalWS = normalize(input.normalWS);
                half wrappedNdotL = saturate(dot(normalWS, mainLight.direction) * 0.5h + 0.5h);
                half stylizedShade = lerp(1.0h, wrappedNdotL, _ShadowStrength);
                half3 ambient = max(SampleSH(normalWS), 0.0h.xxx);
                half3 sceneLight = ambient * 0.65h + mainLight.color * stylizedShade;
                half sceneEnergy = saturate(
                    dot(ambient, half3(0.2126h, 0.7152h, 0.0722h)) +
                    dot(mainLight.color, half3(0.2126h, 0.7152h, 0.0722h)) * 0.25h);
                sceneLight = max(sceneLight, (_AmbientFloor * sceneEnergy).xxx);
                color.rgb *= lerp(sceneEnergy.xxx, sceneLight, _LightInfluence);

                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = tex.a * _TintColor.a;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
