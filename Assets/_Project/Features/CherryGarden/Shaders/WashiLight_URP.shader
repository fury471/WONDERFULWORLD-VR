Shader "Wonderland/CherryGarden/WashiLight_URP"
{
    Properties
    {
        _Color ("Tint", Color) = (0.811, 0.640, 0.403, 1)
        _Texture ("Pattern Texture", 2D) = "white" {}
        _Texture_Channel ("Pattern Channel", Float) = 0
        _Intensity ("Glow Intensity", Range(0, 3)) = 0.3
        _Min_Max_Intensity ("Min Max Intensity", Vector) = (0.5, 2, 0, 0)
        _TilingOffset ("World Tiling Offset", Vector) = (-4, 1.5, 0, 0)
        _World_U ("World U", Vector) = (0, 0, 1, 0)
        _World_V ("World V", Vector) = (0, 1, 0, 0)
        _PersonsShadowTexture ("Shadow Texture", 2D) = "white" {}
        _Persons_Shadow_Intensity ("Shadow Intensity", Range(0, 2)) = 1
        _Persons_Shadow_Speed ("Shadow Speed", Float) = 0.05
        _Persons_Noise_Desnity ("Shadow Density", Float) = 5
        _Border_Distance_Fade ("Border Fade", Range(0, 1)) = 0.15
        _Fade_Smoothness ("Fade Smoothness", Range(0.001, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);
            TEXTURE2D(_PersonsShadowTexture);
            SAMPLER(sampler_PersonsShadowTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Texture_ST;
                float _Texture_Channel;
                float _Intensity;
                float4 _Min_Max_Intensity;
                float4 _TilingOffset;
                float4 _World_U;
                float4 _World_V;
                float _Persons_Shadow_Intensity;
                float _Persons_Shadow_Speed;
                float _Persons_Noise_Desnity;
                float _Border_Distance_Fade;
                float _Fade_Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
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
                output.uv = TRANSFORM_TEX(input.uv, _Texture);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half SelectChannel(half4 value, float channel)
            {
                if (channel < 0.5) return value.r;
                if (channel < 1.5) return value.g;
                if (channel < 2.5) return value.b;
                return value.a;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 worldUV;
                worldUV.x = dot(input.positionWS, _World_U.xyz) * _TilingOffset.x + _TilingOffset.z;
                worldUV.y = dot(input.positionWS, _World_V.xyz) * _TilingOffset.y + _TilingOffset.w;

                half4 patternSample = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, worldUV);
                half pattern = SelectChannel(patternSample, _Texture_Channel);

                float2 shadowUV = input.uv * max(_Persons_Noise_Desnity, 0.001) + _Time.yy * _Persons_Shadow_Speed;
                half shadow = SAMPLE_TEXTURE2D(_PersonsShadowTexture, sampler_PersonsShadowTexture, shadowUV).r;
                shadow = lerp(1.0, shadow, saturate(_Persons_Shadow_Intensity));

                half fresnel = saturate(1.0 - dot(normalize(input.normalWS), half3(0, 1, 0)) * 0.35);
                half minGlow = (half)_Min_Max_Intensity.x;
                half maxGlow = max((half)_Min_Max_Intensity.y, minGlow + 0.001);
                half glow = lerp(minGlow, maxGlow, saturate(pattern + fresnel * 0.15));

                half4 color = _Color;
                color.rgb *= glow * (1.0h + (half)_Intensity) * shadow;

                half border = smoothstep(0.0, max((half)_Fade_Smoothness, 0.001), min(min(input.uv.x, input.uv.y), min(1.0 - input.uv.x, 1.0 - input.uv.y)) + (half)_Border_Distance_Fade);
                color.a *= saturate(border);

                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
}
