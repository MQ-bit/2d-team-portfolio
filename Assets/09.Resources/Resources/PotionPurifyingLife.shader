Shader "Potion/PurifyingLife"
{
    Properties
    {
        _HealingMistAnimation ("Progress, Time", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AreaClipMask ("Footprint", 2D) = "white" {}
        [HideInInspector] _AreaClipRect ("Footprint Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _AreaClipEnabled ("Clip Enabled", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; float layer : TEXCOORD1; };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float layer : TEXCOORD1; float2 supportWS : TEXCOORD2; };
        CBUFFER_START(UnityPerMaterial)
            float4 _HealingMistAnimation;
            float4 _AreaClipRect;
            float _AreaClipEnabled;
        CBUFFER_END
        TEXTURE2D(_AreaClipMask);
        SAMPLER(sampler_AreaClipMask);
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.layer = input.layer;
            float3 support = input.positionOS;
            if (input.layer > 0.5) support.y = 0;
            output.supportWS = TransformObjectToWorld(support).xy;
            return output;
        }
        float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        float Noise(float2 p)
        {
            float2 cell = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
        }
        half4 Frag(Varyings input) : SV_Target
        {
            if (_AreaClipEnabled > 0.5)
            {
                float2 uv = (input.supportWS - _AreaClipRect.xy) / _AreaClipRect.zw;
                clip(min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                clip(SAMPLE_TEXTURE2D(_AreaClipMask, sampler_AreaClipMask, uv).r - 0.5);
            }
            float progress = _HealingMistAnimation.x, time = _HealingMistAnimation.y;
            float fade = smoothstep(0.0, 0.16, progress) * (1.0 - smoothstep(0.42, 1.0, progress));
            float2 p = input.uv * 2.0 - 1.0;
            float density = 0, motes = 0;
            if (input.layer < 0.5)
            {
                float radius = length(p);
                float mist = Noise(p * 3.0 + float2(time * 0.22, -time * 0.18));
                float wave = exp(-pow((radius - lerp(0.25, 0.82, progress)) * 7.0, 2.0));
                density = (0.13 + mist * 0.18 + wave * 0.16) * (1.0 - smoothstep(0.65, 1.0, radius));
            }
            else
            {
                float y = input.uv.y;
                float cloud = Noise(float2(p.x * 3.2 + time * 0.15, y * 4.0 - time * 0.4));
                density = exp(-p.x * p.x * 3.5) * smoothstep(0.0, 0.12, y)
                    * (1.0 - smoothstep(0.22 + progress * 0.3, 0.7 + progress * 0.2, y)) * cloud * 0.22;
                [unroll] for (int i = 0; i < 12; i++)
                {
                    float seed = Hash(float2(i, 7.1));
                    float rise = progress * 0.55 + seed * 0.3;
                    float x = sin(i * 2.4) * 0.78 + sin(time + i) * 0.025;
                    float2 d = float2(p.x - x, y - rise);
                    float softDot = exp(-dot(d * float2(55, 75), d * float2(55, 75)));
                    float cross = exp(-abs(d.x) * 220 - abs(d.y) * 60)
                        + exp(-abs(d.y) * 220 - abs(d.x) * 60);
                    motes += (softDot * 0.45 + cross * 0.18) * sin(progress * 3.14159);
                }
            }
            float3 color = lerp(float3(0.48, 0.9, 0.74), float3(0.9, 1.0, 0.94), saturate(motes + density));
            return half4(color, saturate(density + motes) * fade);
        }
        ENDHLSL
        Pass
        {
            Name "PurifyingLife2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "PurifyingLifeForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
