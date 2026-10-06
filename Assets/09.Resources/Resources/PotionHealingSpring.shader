Shader "Potion/HealingSpring"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SpringColor ("Water Color", Color) = (0.22, 0.72, 0.8, 1)
        _SpringAccent ("Light Color", Color) = (0.78, 1, 1, 1)
        _SpringAnimation ("Time, Visibility", Vector) = (0, 1, 0, 0)
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
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
        CBUFFER_START(UnityPerMaterial)
            float4 _SpringColor;
            float4 _SpringAccent;
            float4 _SpringAnimation;
        CBUFFER_END
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            return output;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            float2 p = float2(input.uv.x * 2.0 - 1.0, input.uv.y);
            float t = _SpringAnimation.x;
            float bend = p.x - sin(p.y * 5.0 - t * 1.3) * 0.018;
            float beam = exp(-bend * bend * 180.0) * 0.32
                + exp(-bend * bend * 1800.0) * 0.32;
            beam *= smoothstep(0.01, 0.1, p.y) * (1.0 - smoothstep(0.45, 1.0, p.y));
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
                float2 d = p - float2(x, 0.04 + phase * 0.8);
                float cross = exp(-abs(d.x) * 300.0 - abs(d.y) * 60.0)
                    + exp(-abs(d.y) * 350.0 - abs(d.x) * 45.0);
                sparkles += cross * sin(phase * 3.14159);
            }
            float light = beam + orbRing * 0.85 + orb * 0.6 + halo;
            float3 color = lerp(_SpringColor.rgb, _SpringAccent.rgb, saturate(light));
            color = lerp(color, float3(1.0, 0.93, 0.55), saturate(sparkles));
            float edge = 1.0 - smoothstep(0.85, 1.0, abs(p.x));
            float alpha = saturate(light + sparkles * 0.65) * edge * _SpringAnimation.y;
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
