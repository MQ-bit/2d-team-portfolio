Shader "Potion/FireVortex"
{
    Properties
    {
        _FireAccent ("Flame Core", Color) = (1, 0.42, 0.075, 1)
        _FireAnimation ("Time, Visibility", Vector) = (0, 1, 0, 0)
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
            float2 uv : TEXCOORD0;
            float4 flame : TEXCOORD1;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 shape : TEXCOORD0;
            float4 flame : TEXCOORD1;
        };
        CBUFFER_START(UnityPerMaterial)
            float4 _FireAccent;
            float4 _FireAnimation;
        CBUFFER_END
        Varyings Vert(Attributes input)
        {
            Varyings output;
            float variation = input.flame.x;
            float rim = input.flame.y;
            float phase = _FireAnimation.x * (1.6 + variation * 1.0) + input.flame.z;
            float flicker = sin(phase) * 0.03 + sin(phase * 1.37 + variation) * 0.014;
            float height = lerp(0.21 + variation * 0.22 + flicker,
                0.065 + variation * 0.055 + flicker * 0.25, rim) * 0.82;
            float baseWidth = lerp(0.075 + variation * 0.055, 0.024 + variation * 0.012, rim);
            float y = input.uv.y;
            float bend = (sin(y * 3.2 - phase * 0.8) * 0.046
                + sin(y * 6.0 + phase * 0.45 + variation * 3.0) * 0.018)
                * smoothstep(0.0, 0.85, y) * lerp(1.0, 0.45, rim);
            float breathe = 1.0 + sin(phase * 0.7 + variation * 2.0) * 0.16;
            float halfWidth = max(baseWidth * 1.7 * breathe, lerp(0.07, 0.2, saturate(y - 0.4)));
            float3 position = input.positionOS;
            position.xy += float2((input.uv.x * halfWidth + bend) * 0.5, y * height * 1.8) * input.flame.w;
            output.positionCS = TransformObjectToHClip(position);
            output.shape = float2(input.uv.x * halfWidth, y);
            output.flame = input.flame;
            return output;
        }
        float Hash(float2 p)
        {
            return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
        }
        float Noise(float2 p)
        {
            float2 cell = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
        }
        half4 Frag(Varyings input) : SV_Target
        {
            clip(input.flame.w - 0.5);
            float variation = input.flame.x;
            float rim = input.flame.y;
            float t = _FireAnimation.x;
            float2 p = input.shape;
            float phase = t * (1.6 + variation * 1.0) + input.flame.z;
            float baseWidth = lerp(0.075 + variation * 0.055, 0.024 + variation * 0.012, rim);
            float breathe = 1.0 + sin(phase * 0.7 + variation * 2.0) * 0.16;
            float cap = saturate(p.y / 1.08);
            float width = max(0.006, sqrt(saturate(1.0 - cap * cap)) * baseWidth * breathe);
            float tongue = exp(-pow(p.x / width, 2.0));
            float vertical = smoothstep(0.0, 0.06, p.y) * (1.0 - smoothstep(0.55, 1.08, p.y));
            float noise = Noise(p * float2(28.0, 4.0) + float2(variation * 17.0, -t * 0.85));
            float fine = Noise(p * float2(53.0, 8.0) + float2(variation * 31.0, -t * 1.2));
            float turbulence = noise * 0.75 + fine * 0.25;
            float density = tongue * vertical * lerp(0.55, 1.0,
                smoothstep(0.14 + saturate(p.y) * 0.28, 0.65, turbulence + tongue * 0.28));
            density *= 0.92 + sin(phase * 1.1) * 0.08;
            float temperature = saturate((1.0 - saturate(p.y)) * 0.65 + turbulence * 0.25);
            float3 flameColor = lerp(float3(0.95, 0.022, 0.012), float3(1.0, 0.16, 0.02),
                smoothstep(0.18, 0.52, temperature));
            flameColor = lerp(flameColor, lerp(float3(1.0, 0.7, 0.13), _FireAccent.rgb, 0.35),
                smoothstep(0.52, 0.8, temperature));
            flameColor = lerp(flameColor, float3(1.0, 0.95, 0.76), smoothstep(0.82, 0.98, temperature));
            float smoke = exp(-pow(p.x / (0.07 + saturate(p.y) * 0.1), 2.0))
                * smoothstep(0.75, 1.05, p.y) * (1.0 - smoothstep(1.15, 1.55, p.y)) * noise;
            float rise = frac(t * (0.24 + variation * 0.2) + input.flame.z);
            float2 ember = p - float2(sin(phase * 0.5) * 0.035, 0.2 + rise * 1.1);
            float spark = exp(-dot(ember * float2(1.0, 0.4), ember * float2(1.0, 0.4)) * 28000.0)
                * sin(rise * 3.14159) * step(lerp(0.35, 0.7, rim), variation);
            float visibility = _FireAnimation.y;
            float flameAlpha = (1.0 - exp(-density * 1.7)) * visibility;
            float smokeAlpha = smoke * lerp(0.08, 0.02, rim) * visibility * (1.0 - flameAlpha);
            float sparkAlpha = spark * visibility;
            float3 color = flameColor * flameAlpha * 1.15
                + lerp(float3(0.045, 0.035, 0.045), float3(0.19, 0.17, 0.15), noise) * smokeAlpha
                + lerp(float3(1.0, 0.045, 0.015), float3(1.0, 0.55, 0.13), fine) * sparkAlpha;
            return half4(color, saturate(flameAlpha + smokeAlpha + sparkAlpha));
        }
        ENDHLSL
        Pass
        {
            Name "FireVortex2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "FireVortexForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
