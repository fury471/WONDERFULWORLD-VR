Shader "WonderfulWorld/Water/Stylized Waterfall URP"
{
    Properties
    {
        [MainColor]_BaseColor("Deep Water", Color) = (0.08, 0.42, 0.55, 0.72)
        _ShallowColor("Lit Water", Color) = (0.34, 0.86, 0.95, 0.82)
        _FoamColor("Foam", Color) = (0.88, 0.98, 1.0, 0.95)
        _ShadowColor("Ink Shadow", Color) = (0.025, 0.13, 0.18, 0.45)
        _FlowSpeed("Flow Speed", Range(0.1, 6.0)) = 1.8
        _FlowScale("Flow Scale", Range(0.5, 16.0)) = 5.5
        _FoamAmount("Foam Amount", Range(0.0, 1.0)) = 0.58
        _EdgeFoam("Edge Foam", Range(0.0, 1.0)) = 0.42
        _BottomFoam("Bottom Foam", Range(0.0, 1.0)) = 0.78
        _Alpha("Alpha", Range(0.0, 1.0)) = 0.78
        _SwayStrength("Mesh Sway", Range(0.0, 0.35)) = 0.055
        _SwayScale("Sway Scale", Range(0.1, 8.0)) = 1.6
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
            Name "Waterfall"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                half4 _ShadowColor;
                half _FlowSpeed;
                half _FlowScale;
                half _FoamAmount;
                half _EdgeFoam;
                half _BottomFoam;
                half _Alpha;
                half _SwayStrength;
                half _SwayScale;
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
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += Noise(p) * amplitude;
                    p = p * 2.03 + 17.13;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float flow = _Time.y * _FlowSpeed;
                float wave = sin((input.uv.y * 7.0 + flow) + input.uv.x * 2.4) * 0.5 + 0.5;
                float edgeMask = saturate(abs(input.uv.x - 0.5) * 2.0);
                positionOS.x += (wave - 0.5) * _SwayStrength * lerp(0.45, 1.0, edgeMask);
                positionOS.z += sin(flow * 1.4 + input.uv.y * _SwayScale * 6.283) * _SwayStrength * 0.5;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y * _FlowSpeed;
                float2 flowUv = float2((uv.x - 0.5) * _FlowScale, uv.y * _FlowScale + time);
                float longStreaks = Fbm(flowUv * float2(0.45, 1.85));
                float brokenStreaks = Fbm(flowUv * float2(1.25, 0.7) + float2(9.0, time * 0.28));
                float veins = smoothstep(0.47, 0.92, longStreaks * 0.68 + brokenStreaks * 0.55);

                float side = saturate(abs(uv.x - 0.5) * 2.0);
                float edgeFoam = smoothstep(0.58, 1.0, side) * _EdgeFoam;
                float bottomFoam = smoothstep(0.58, 1.0, 1.0 - uv.y) * _BottomFoam;
                float topBreak = smoothstep(0.82, 1.0, uv.y) * 0.22;
                float foam = saturate(veins * _FoamAmount + edgeFoam + bottomFoam + topBreak);

                half3 water = lerp(_BaseColor.rgb, _ShallowColor.rgb, saturate(uv.y * 0.28 + veins * 0.38));
                half3 shadow = _ShadowColor.rgb * smoothstep(0.0, 0.6, brokenStreaks) * (1.0 - foam);
                half3 color = lerp(water - shadow * _ShadowColor.a, _FoamColor.rgb, foam);

                color = floor(color * 7.0 + 0.5) / 7.0;
                color = MixFog(color, input.fogFactor);

                half edgeAlpha = smoothstep(0.0, 0.09, uv.x) * (1.0 - smoothstep(0.91, 1.0, uv.x));
                half verticalAlpha = smoothstep(0.0, 0.045, uv.y) * (1.0 - smoothstep(0.94, 1.0, uv.y));
                half alpha = saturate(_Alpha * edgeAlpha * verticalAlpha + foam * 0.22);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
