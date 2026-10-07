Shader "Potion/Clipped VFX"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        _FrostPoisonPalette ("Ice and Poison Palette", Float) = 0
        _FrozenCurrentPalette ("Ice and Electricity Palette", Float) = 0
        _ToxicCurrentPalette ("Poison and Electricity Palette", Float) = 0
        [HideInInspector] _AreaClipMask ("Clipped Footprint", 2D) = "white" {}
        [HideInInspector] _AreaClipRect ("Footprint World Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _AreaClipEnabled ("Clip Enabled", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 clipPositionWS : TEXCOORD1;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FrostPoisonPalette;
                float _FrozenCurrentPalette;
                float _ToxicCurrentPalette;
                float4 _MainTex_TexelSize;
                float4 _AreaClipRect;
                float _AreaClipEnabled;
            CBUFFER_END
            TEXTURE2D(_AreaClipMask);
            SAMPLER(sampler_AreaClipMask);
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.clipPositionWS = TransformObjectToWorld(input.positionOS).xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if (_AreaClipEnabled > 0.5)
                {
                    float2 uv = (input.clipPositionWS - _AreaClipRect.xy) / _AreaClipRect.zw;
                    clip(min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                    clip(SAMPLE_TEXTURE2D(_AreaClipMask, sampler_AreaClipMask, uv).r - 0.5);
                }
                half4 sprite = CommonUnlitFragment(input, half4(1, 1, 1, 1));
                if (_FrostPoisonPalette > 0.5)
                {
                    // Replace the baked violet hue while keeping the animation's shading and alpha.
                    half brightness = max(sprite.r, max(sprite.g, sprite.b));
                    half ice = smoothstep(0.35, 0.8, brightness);
                    half3 poisonColor = half3(0.48, 0.85, 0.18);
                    half3 iceColor = half3(0.65, 0.95, 1.0);
                    sprite.rgb = lerp(poisonColor, iceColor, ice) * brightness;
                }
                else if (_FrozenCurrentPalette > 0.5)
                {
                    // Blue ice facets carry short, scattered cyan-white discharge glints.
                    half brightness = max(sprite.r, max(sprite.g, sprite.b));
                    half3 iceColor = lerp(half3(0.16, 0.45, 0.8), half3(0.58, 0.86, 1.0),
                        smoothstep(0.15, 0.75, brightness));
                    float tick = floor(_Time.y * 12.0);
                    float2 cell = floor(input.uv * _MainTex_TexelSize.zw / 4.0);
                    float charge = frac(sin(dot(cell, float2(127.1, 311.7)) + tick * 17.3) * 43758.5453);
                    half discharge = smoothstep(0.68, 0.94, charge) * smoothstep(0.3, 0.8, brightness);
                    sprite.rgb = lerp(iceColor, half3(0.88, 1.0, 1.0), discharge) * brightness;
                    sprite.rgb *= 1.0 + discharge * 0.3;
                }
                else if (_ToxicCurrentPalette > 0.5)
                {
                    // Toxic green strands retain a violet corona and brief white discharge cores.
                    half brightness = max(sprite.r, max(sprite.g, sprite.b));
                    half3 poisonColor = lerp(half3(0.18, 0.46, 0.06), half3(0.62, 0.92, 0.16),
                        smoothstep(0.15, 0.7, brightness));
                    float2 cell = floor(input.uv * _MainTex_TexelSize.zw / 6.0);
                    float phase = dot(cell, float2(127.1, 311.7));
                    float tick = floor(_Time.y * 10.0);
                    float charge = frac(sin(phase + tick * 17.3) * 43758.5453);
                    half discharge = smoothstep(0.72, 0.95, charge);
                    half3 currentColor = lerp(half3(0.64, 0.38, 1.0), half3(0.94, 1.0, 0.78),
                        smoothstep(0.45, 0.85, brightness));
                    sprite.rgb = lerp(poisonColor, currentColor, discharge) * brightness;
                    sprite.rgb *= 1.0 + discharge * 0.35;
                }
                return sprite * input.color;
            }
            ENDHLSL
        }
    }
}
