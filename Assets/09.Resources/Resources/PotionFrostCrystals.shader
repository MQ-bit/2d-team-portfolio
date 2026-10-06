Shader "Potion/FrostCrystals"
{
    Properties
    {
        _IceAccent ("Ice Tint", Color) = (0.75, 0.96, 1, 1)
        _IceAnimation ("Growth, Visibility, Time, Steam", Vector) = (1, 1, 0, 0)
        [HideInInspector] _AreaClipMask ("Ground Footprint", 2D) = "white" {}
        [HideInInspector] _AreaClipRect ("Footprint Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _AreaClipEnabled ("Clip Enabled", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color : COLOR;
            float2 root : TEXCOORD0;
            float3 mist : TEXCOORD1;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4 color : COLOR;
            float3 glint : TEXCOORD0;
            float4 mist : TEXCOORD1;
            float2 supportWS : TEXCOORD2;
        };
        CBUFFER_START(UnityPerMaterial)
            float4 _IceAccent;
            float4 _IceAnimation;
            float4 _AreaClipRect;
            float _AreaClipEnabled;
            float4 _PuddleShape;
            float4 _PuddleFrame;
            float4 _PuddleBounds;
            float4 _PuddleSpread;
        CBUFFER_END
        #include "PotionCombinationReveal.hlsl"
        TEXTURE2D(_AreaClipMask);
        SAMPLER(sampler_AreaClipMask);
        Varyings Vert(Attributes input)
        {
            Varyings output;
            float3 position = input.positionOS;
            // Crystals emerge in small staggered groups as the freeze reaches the rim.
            float phase = dot(input.root, float2(37.2, 19.7));
            float delay = frac(sin(phase) * 43758.5453) * 0.14;
            position.xy = input.root * _IceAnimation.x
                + (position.xy - input.root) * smoothstep(0.15 + delay, 0.8 + delay, _IceAnimation.x);
            if (input.mist.z > 0.5)
            {
                position.xy = input.positionOS.xy * _IceAnimation.x;
                position.x += sin(_IceAnimation.z * 1.1 + phase + input.mist.y * 2.0)
                    * 0.018 * input.mist.y * _IceAnimation.x;
            }
            output.positionCS = TransformObjectToHClip(position);
            output.color = input.color;
            float reveal = PotionCombinationReveal(input.root);
            output.color.a *= reveal;
            output.glint = float3(input.positionOS.xy - input.root, phase);
            output.mist = float4(input.mist, reveal);
            output.supportWS = TransformObjectToWorld(float3(position.x,
                input.root.y * _IceAnimation.x, input.positionOS.z)).xy;
            return output;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            if (input.mist.z > 0.5)
            {
                if (_AreaClipEnabled > 0.5)
                {
                    float2 uv = (input.supportWS - _AreaClipRect.xy) / _AreaClipRect.zw;
                    clip(min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                    clip(SAMPLE_TEXTURE2D(_AreaClipMask, sampler_AreaClipMask, uv).r - 0.5);
                }
                float2 p = input.mist.xy;
                float time = _IceAnimation.z;
                float phase = input.glint.z;
                float rise = frac(time * lerp(0.45, 1.1, _IceAnimation.w) + frac(sin(phase) * 43758.5453));
                float center = 0.16 + rise * 0.64;
                float curl = sin(p.y * 8.0 - time * 1.8 + phase) * 0.2;
                float cloud = exp(-pow((p.x + curl) * 1.5, 2.0)
                    - pow((p.y - center) * 3.5, 2.0));
                float wisps = 0.8 + sin(p.x * 6.0 + p.y * 12.0 - time * 2.0 + phase) * 0.15;
                float edges = smoothstep(0.0, 0.1, p.y) * (1.0 - smoothstep(0.72, 1.0, p.y))
                    * (1.0 - smoothstep(0.65, 1.0, abs(p.x)));
                half alpha = cloud * wisps * edges * sin(rise * 3.14159)
                    * lerp(0.42, 0.55, _IceAnimation.w) * _IceAnimation.y * input.mist.w;
                half3 color = lerp(half3(0.86, 0.97, 1.0), _IceAccent.rgb, 0.2);
                color = lerp(color, half3(0.95, 0.96, 0.94), _IceAnimation.w * 0.65);
                return half4(color * alpha, alpha);
            }
            // Thermal shock uses this mesh for steam only; no persistent ice shards.
            if (_IceAnimation.w > 0.5) return half4(0, 0, 0, 0);
            half alpha = input.color.a * _IceAnimation.y;
            half3 color = input.color.rgb * lerp(half3(1, 1, 1), _IceAccent.rgb, 0.35);
            // A narrow light sweep catches each facet at a different time.
            float sweep = pow(saturate(sin(dot(input.glint.xy, float2(48, 32))
                - _IceAnimation.z * 3.2 + input.glint.z)), 16.0);
            float facet = smoothstep(0.6, 0.95, input.color.g);
            float birth = sin(saturate((_IceAnimation.x - 0.15) / 0.85) * 3.14159);
            color = lerp(color, half3(0.9, 1, 1), saturate(sweep * facet * 0.65 + birth * 0.2));
            alpha = saturate(alpha + sweep * facet * 0.12 * _IceAnimation.y * input.mist.w);
            return half4(color * alpha, alpha);
        }
        ENDHLSL
        Pass
        {
            Name "IceCrystals2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "IceCrystalsForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
