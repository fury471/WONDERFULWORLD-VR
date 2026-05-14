Shader "Wonderland/Vegetation/Toon Grass URP"
{
    Properties
    {
        _BaseMap("Blade Texture", 2D) = "white" {}
        _BaseColor("Root Color", Color) = (0.32, 0.46, 0.18, 1)
        _TipColor("Tip Color", Color) = (0.68, 0.76, 0.36, 1)
        _HighlightColor("Highlight Color", Color) = (0.86, 0.72, 0.42, 1)
        _HighlightStrength("Highlight Strength", Range(0, 1)) = 0.15
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.45
        _WindStrength("Wind Strength", Range(0, 0.25)) = 0.035
        _WindFrequency("Wind Frequency", Range(0, 8)) = 1.7
        _WindScale("Wind Scale", Range(0.1, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _TipColor;
                half4 _HighlightColor;
                half _HighlightStrength;
                half _Cutoff;
                half _WindStrength;
                half _WindFrequency;
                half _WindScale;
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
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionOS = input.positionOS.xyz;
                float bladeMask = saturate(input.uv.y);
                float3 positionWS = TransformObjectToWorld(positionOS);
                float wind = sin((_Time.y * _WindFrequency) + (positionWS.x + positionWS.z) * _WindScale);
                positionOS.xz += wind * _WindStrength * bladeMask;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(tex.a - _Cutoff);

                half heightMask = saturate(input.uv.y);
                half4 color = lerp(_BaseColor, _TipColor, heightMask) * tex;
                color.rgb = lerp(color.rgb, _HighlightColor.rgb, _HighlightStrength * heightMask);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
