Shader "Potion/HealingSpring"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SpringColor ("Water Color", Color) = (0.22, 0.72, 0.8, 1)
        _SpringAccent ("Light Color", Color) = (0.78, 1, 1, 1)
        _SpringAnimation ("Time, Visibility, Vampiric, Electric", Vector) = (0, 1, 0, 0)
        [HideInInspector] _AreaClipMask ("Ground Footprint", 2D) = "white" {}
        [HideInInspector] _AreaClipRect ("Footprint Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _AreaClipEnabled ("Clip Enabled", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 supportWS : TEXCOORD1;
        };
        CBUFFER_START(UnityPerMaterial)
            float4 _SpringColor;
            float4 _SpringAccent;
            float4 _SpringAnimation;
            float4x4 _SpringGroundFromWorld;
            float4 _AreaClipRect;
            float _AreaClipEnabled;
            float4 _PuddleShape;
            float4 _PuddleFrame;
            float4 _PuddleBounds;
            float4 _PuddleSpread;
        CBUFFER_END
        #include "PotionCombinationReveal.hlsl"
        #include "PotionBlazingCurrent.hlsl"
        TEXTURE2D(_AreaClipMask);
        SAMPLER(sampler_AreaClipMask);
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.supportWS = TransformObjectToWorld(float3(input.positionOS.x, 0, input.positionOS.z));
            return output;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            if (_AreaClipEnabled > 0.5)
            {
                float2 clipUV = (input.supportWS.xy - _AreaClipRect.xy) / _AreaClipRect.zw;
                clip(min(min(clipUV.x, clipUV.y), min(1.0 - clipUV.x, 1.0 - clipUV.y)));
                clip(SAMPLE_TEXTURE2D(_AreaClipMask, sampler_AreaClipMask, clipUV).r - 0.5);
            }
            float2 groundRoot = mul(_SpringGroundFromWorld, float4(input.supportWS, 1)).xy;
            float reveal = PotionCombinationReveal(groundRoot);
            float2 p = float2(input.uv.x * 2.0 - 1.0, input.uv.y);
            float t = _SpringAnimation.x;
            float bend = p.x - sin(p.y * 5.0 - t * 1.3) * 0.018;
            float beam = exp(-bend * bend * 180.0) * 0.32
                + exp(-bend * bend * 1800.0) * 0.32;
            beam *= smoothstep(0.01, 0.1, p.y) * (1.0 - smoothstep(0.45, 1.0, p.y));
            beam *= lerp(1.0, 0.6, _SpringAnimation.z);
            float2 orbPosition = (p - float2(0, 0.12)) * float2(1.0, 1.6);
            float orbRadius = length(orbPosition);
            float orbRing = exp(-pow((orbRadius - 0.062) * 145.0, 2.0));
            float orb = exp(-orbRadius * orbRadius * 900.0);
            float halo = exp(-orbRadius * orbRadius * 90.0) * 0.3;
            float sparkles = 0.0;
            [unroll] for (int i = 0; i < 10; i++)
            {
                float seed = frac(sin((i + 1.0) * 127.1) * 43758.5453);
                float phase = frac(t * 0.18 + seed);
                float x = sin(i * 2.4 + phase * 2.0) * (0.25 + seed * 0.45);
                float height = 0.04 + phase * 0.8;
                // Absorbed motes converge on the healing core, not a second full-height effect.
                x = lerp(x, x * (1.0 - phase), _SpringAnimation.z);
                height = lerp(height, 0.12 + (1.0 - phase) * 0.55, _SpringAnimation.z);
                float2 d = p - float2(x, height);
                float cross = exp(-abs(d.x) * 300.0 - abs(d.y) * 60.0)
                    + exp(-abs(d.y) * 350.0 - abs(d.x) * 45.0);
                sparkles += cross * sin(phase * 3.14159);
            }
            float light = beam + orbRing * 0.85 + orb * 0.6 + halo;
            float electricArc = 0.0;
            if (_SpringAnimation.w > 0.5)
            {
                electricArc = CurrentArc(float2(p.x * 1.5, p.y * 1.8), t, 17.3)
                    * exp(-p.x * p.x * 45.0) * (1.0 - smoothstep(0.45, 0.75, p.y));
                light += electricArc * 0.7;
            }
            // Keep the recognizable healing core while incoming motes convey life absorption.
            light *= 1.0 + _SpringAnimation.z * sin(t * 2.4) * 0.12;
            float3 color = lerp(_SpringColor.rgb, _SpringAccent.rgb, saturate(light));
            color = lerp(color, lerp(float3(1.0, 0.93, 0.55), _SpringAccent.rgb, _SpringAnimation.z), saturate(sparkles));
            float edge = 1.0 - smoothstep(0.85, 1.0, abs(p.x));
            float alpha = saturate(light + sparkles * 0.65) * edge * _SpringAnimation.y * reveal;
            return half4(color, alpha);
        }
        ENDHLSL
        Pass
        {
            Name "HealingSpring2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "HealingSpringForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
