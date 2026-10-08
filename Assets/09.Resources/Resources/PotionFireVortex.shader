Shader "Potion/FireVortex"
{
    Properties
    {
        _FireAccent ("Flame Core", Color) = (1, 0.42, 0.075, 1)
        _FireGroundColor ("Ground Heat", Color) = (0.58, 0.055, 0.025, 1)
        _FireAnimation ("Time, Visibility, Ground Growth, Age", Vector) = (0, 1, 1, 1)
        _FireCurrent ("Discharge Enabled, Seed, Height, Width", Vector) = (0, 0, 0.68, 0.85)
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
            float3 life : TEXCOORD2;
            float discharge : TEXCOORD3;
        };
        CBUFFER_START(UnityPerMaterial)
            float4 _FireAccent;
            float4 _FireGroundColor;
            float4 _FireAnimation;
            float4 _FireCurrent;
            float4 _PuddleShape;
            float4 _PuddleFrame;
            float4 _PuddleBounds;
            float4 _PuddleSpread;
        CBUFFER_END
        #include "PotionCombinationReveal.hlsl"
        #include "PotionBlazingCurrent.hlsl"
        float FlameGrowth(float phaseOffset)
        {
            float seed = frac(sin(phaseOffset * 7.13) * 43758.5453);
            float delay = frac(phaseOffset / 6.283185) * 0.3;
            float duration = lerp(0.45, 1.0, seed);
            float age = max(0.0, _FireAnimation.w - delay);
            float progress = smoothstep(0.0, duration, age);
            // Uneven ignition briefly catches and recedes instead of scaling uniformly.
            float flutter = (sin(age * (9.0 + seed * 7.0) + phaseOffset) * 0.09
                + sin(age * 23.0 + phaseOffset * 1.7) * 0.04) * sin(progress * 3.14159);
            return saturate(progress + flutter);
        }
        float FlamePulse(float phaseOffset)
        {
            // Smooth random surges keep neighboring flames out of rhythm after ignition.
            float seed = frac(phaseOffset * 2.413);
            float tick = _FireAnimation.x * lerp(3.0, 5.5, seed) + phaseOffset * 3.0;
            float cell = floor(tick);
            float blend = frac(tick);
            blend = blend * blend * (3.0 - 2.0 * blend);
            float first = frac(sin(cell * 127.1 + phaseOffset * 311.7) * 43758.5453);
            float second = frac(sin((cell + 1.0) * 127.1 + phaseOffset * 311.7) * 43758.5453);
            return lerp(first, second, blend);
        }
        Varyings Vert(Attributes input)
        {
            Varyings output;
            float variation = input.flame.x;
            float rim = input.flame.y;
            float growth = FlameGrowth(input.flame.z);
            float surge = FlamePulse(input.flame.z);
            float discharge = CurrentDischarge(input.positionOS.xy * 2.0,
                _FireAnimation.x, _FireCurrent.y) * _FireCurrent.x;
            float phase = _FireAnimation.x * (1.6 + variation * 1.0) + input.flame.z;
            float flicker = sin(phase) * 0.018 + sin(phase * 1.37 + variation) * 0.008;
            float height = lerp(0.145 + variation * 0.135 + flicker,
                0.055 + variation * 0.04 + flicker * 0.25, rim) * 0.82;
            height *= lerp(0.82, 1.1, surge);
            height *= lerp(1.0, _FireCurrent.z * (1.0 + discharge * 0.25), _FireCurrent.x);
            float baseWidth = lerp(0.047 + variation * 0.034, 0.02 + variation * 0.01, rim);
            baseWidth *= lerp(1.0, _FireCurrent.w, _FireCurrent.x);
            float y = input.uv.y;
            float bend = (sin(y * 3.2 - phase * 0.8) * 0.028
                + sin(y * 6.0 + phase * 0.45 + variation * 3.0) * 0.01)
                * smoothstep(0.0, 0.85, y) * lerp(1.0, 0.45, rim);
            float breathe = (1.0 + sin(phase * 0.7 + variation * 2.0) * 0.09)
                * lerp(1.06, 0.9, surge);
            float halfWidth = max(baseWidth * 1.7 * breathe, lerp(0.07, 0.2, saturate(y - 0.4)));
            float3 position = input.positionOS;
            // Roots follow the ground; height and width grow more slowly from small embers.
            position.xy *= _FireAnimation.z;
            position.xy += float2((input.uv.x * halfWidth * lerp(0.35, 1.0, sqrt(growth))
                + bend * lerp(0.2, 1.0, growth)) * 0.5,
                y * height * 1.8 * lerp(0.12, 1.0, growth))
                * input.flame.w * _FireAnimation.z;
            output.positionCS = TransformObjectToHClip(position);
            output.shape = float2(input.uv.x * halfWidth, y);
            output.flame = input.flame;
            output.life = float3(growth, surge, PotionCombinationReveal(input.positionOS.xy));
            output.discharge = discharge;
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
            float baseWidth = lerp(0.047 + variation * 0.034, 0.02 + variation * 0.01, rim);
            baseWidth *= lerp(1.0, _FireCurrent.w, _FireCurrent.x);
            float breathe = (1.0 + sin(phase * 0.7 + variation * 2.0) * 0.09)
                * lerp(1.06, 0.9, input.life.y);
            float cap = saturate(p.y / 1.08);
            float width = max(0.006, sqrt(saturate(1.0 - cap * cap)) * baseWidth * breathe);
            float tongue = exp(-pow(p.x / width, 2.0));
            float vertical = smoothstep(-0.1, 0.04, p.y) * (1.0 - smoothstep(0.55, 1.08, p.y));
            float noise = Noise(p * float2(28.0, 4.0) + float2(variation * 17.0, -t * 0.85));
            float fine = Noise(p * float2(53.0, 8.0) + float2(variation * 31.0, -t * 1.2));
            float turbulence = noise * 0.75 + fine * 0.25;
            float density = tongue * vertical * lerp(0.55, 1.0,
                smoothstep(0.14 + saturate(p.y) * 0.28, 0.65, turbulence + tongue * 0.28));
            density *= 0.82 + input.life.y * 0.12 + sin(phase * 1.1) * 0.03;
            float temperature = saturate((1.0 - saturate(p.y)) * 0.65 + turbulence * 0.25);
            float3 flameColor = lerp(float3(0.95, 0.022, 0.012), float3(1.0, 0.16, 0.02),
                smoothstep(0.18, 0.52, temperature));
            flameColor = lerp(flameColor, lerp(float3(1.0, 0.7, 0.13), _FireAccent.rgb, 0.35),
                smoothstep(0.52, 0.8, temperature));
            flameColor = lerp(flameColor, float3(1.0, 0.82, 0.48), smoothstep(0.82, 0.98, temperature));
            // Match the lower flame to the heated floor before transitioning to orange.
            float3 contactColor = lerp(_FireGroundColor.rgb, _FireAccent.rgb, 0.3);
            contactColor = lerp(contactColor, float3(0.48, 0.13, 0.62), _FireCurrent.x * 0.45);
            flameColor = lerp(contactColor, flameColor, smoothstep(0.0, 0.3, p.y));
            // Ionized cores flare inside the flame instead of adding a separate lightning column.
            float ion = input.discharge * tongue * (1.0 - smoothstep(0.25, 0.85, p.y));
            float corona = input.discharge * (1.0 - tongue)
                * (1.0 - smoothstep(0.25, 0.8, p.y));
            flameColor = lerp(flameColor, float3(0.68, 0.25, 0.94), corona * 0.55);
            flameColor = lerp(flameColor, float3(1.0, 0.92, 0.62), ion * 0.75);
            float contact = exp(-pow(p.x / (baseWidth * 1.35), 2.0) - pow(p.y / 0.1, 2.0));
            float smoke = exp(-pow(p.x / (0.07 + saturate(p.y) * 0.1), 2.0))
                * smoothstep(0.75, 1.05, p.y) * (1.0 - smoothstep(1.15, 1.55, p.y)) * noise;
            float rise = frac(t * (0.24 + variation * 0.2) + input.flame.z);
            float2 ember = p - float2(sin(phase * 0.5) * 0.035, 0.2 + rise * 1.1);
            float spark = exp(-dot(ember * float2(1.0, 0.4), ember * float2(1.0, 0.4)) * 28000.0)
                * sin(rise * 3.14159) * step(lerp(0.78, 0.92, rim), variation);
            float growth = input.life.x;
            float visibility = _FireAnimation.y * lerp(0.5, 1.0, growth) * input.life.z;
            visibility *= lerp(1.0, smoothstep(0.12, 0.42, variation) * 0.85, _FireCurrent.x);
            float flameAlpha = (1.0 - exp(-density * 1.1)) * visibility;
            float smokeAlpha = smoke * lerp(0.04, 0.015, rim) * visibility * growth * (1.0 - flameAlpha);
            float sparkAlpha = spark * visibility * growth * 0.45;
            float contactAlpha = contact * 0.22 * visibility * (1.0 - flameAlpha);
            float3 color = flameColor * flameAlpha + contactColor * contactAlpha
                + lerp(float3(0.045, 0.035, 0.045), float3(0.19, 0.17, 0.15), noise) * smokeAlpha
                + lerp(float3(1.0, 0.045, 0.015), float3(1.0, 0.55, 0.13), fine) * sparkAlpha;
            return half4(color, saturate(flameAlpha + contactAlpha + smokeAlpha + sparkAlpha));
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
