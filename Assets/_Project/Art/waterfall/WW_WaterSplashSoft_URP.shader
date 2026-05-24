Shader "WonderfulWorld/Water/Soft Splash URP"
{
    Properties
    {
        [MainColor]_BaseColor("Splash Color", Color) = (0.78, 0.95, 1.0, 0.72)
        _CoreColor("Core Foam", Color) = (1.0, 1.0, 1.0, 0.9)
        _Cutout("Cutout", Range(0.0, 1.0)) = 0.22
        _Softness("Softness", Range(0.001, 1.0)) = 0.34
        _NoiseScale("Noise Scale", Range(1.0, 24.0)) = 9.0
        _NoiseStrength("Noise Strength", Range(0.0, 1.0)) = 0.42
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "Splash"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _CoreColor;
                half _Cutout;
                half _Softness;
                half _NoiseScale;
                half _NoiseStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(41.13, 289.47));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x),
                    lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x),
                    u.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 centered = input.uv * 2.0 - 1.0;
                float r = length(centered);
                float angle = atan2(centered.y, centered.x);
                float burstNoise = Noise(float2(angle * 2.6, r * _NoiseScale - _Time.y * 1.4));
                float ragged = saturate(1.0 - r + (burstNoise - 0.5) * _NoiseStrength);
                float alpha = smoothstep(_Cutout, _Cutout + _Softness, ragged);
                alpha *= 1.0 - smoothstep(0.62, 1.0, r);

                half3 color = lerp(_BaseColor.rgb, _CoreColor.rgb, saturate(alpha * 0.8 + (1.0 - r) * 0.35));
                color = floor(color * 6.0 + 0.5) / 6.0;
                color = MixFog(color, input.fogFactor);

                return half4(color * input.color.rgb, alpha * _BaseColor.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
