Shader "WonderfulWorld/VFXHooks/UnlitRipple_lwa"
{
    Properties
    {
        [MainColor]_BaseColor("Base Color", Color) = (0.4, 0.8, 1.0, 0.6)
        _EdgeSoftness("Edge Softness", Range(0.001, 1.0)) = 0.18
        _PulseSpeed("Pulse Speed", Range(0.1, 10.0)) = 2.2
        _PulseStrength("Pulse Strength", Range(0.0, 1.0)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WonderfulWorldVfxGlobals_lwa.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _EdgeSoftness;
                half _PulseSpeed;
                half _PulseStrength;
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
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * 2.0 - 1.0;
                float r = length(uv);

                // A subtle pulse tied to lotus or firework events (whichever happened last).
                float eventTime = max(_WW_LastLotusPosTime.w, _WW_LastFireworkPosTime.w);
                float phase = (_Time.y - eventTime) * _PulseSpeed;
                float pulse = saturate(1.0 - abs(sin(phase)) * 0.85) * _PulseStrength;

                float edge = smoothstep(1.0, 1.0 - max(0.001, _EdgeSoftness), r);
                half alpha = _BaseColor.a * edge * (1.0 + pulse);

                // Fog-color tint hook (useful for showing weather mood in VFX).
                half3 tint = lerp(_BaseColor.rgb, _WW_WeatherFogColor.rgb, 0.25);

                return half4(tint, alpha);
            }
            ENDHLSL
        }
    }
}
