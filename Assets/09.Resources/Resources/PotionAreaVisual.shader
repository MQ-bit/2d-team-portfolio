Shader "Potion/Area"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _AreaColor ("Area Color", Color) = (0.3, 0.8, 0.6, 1)
        _AccentColor ("VFX Accent Color", Color) = (0.7, 1, 0.8, 1)
        _Surface ("Wobble, Detail, Opacity, Style", Vector) = (0.018, 0.65, 0.7, 0)
        _Animation ("Time, Appearance, Fade, Seed", Vector) = (0, 1, 1, 0)
        [HideInInspector] _PuddleShape ("Distance, Radii, Blend", Vector) = (0, 1, 1, 0.25)
        [HideInInspector] _PuddleFrame ("Direction, Neck, Enabled", Vector) = (1, 0, 0.55, 0)
        [HideInInspector] _PuddleBounds ("Size, Center Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _PuddleSpread ("Reach, Softness, Enabled, Complete", Vector) = (0, 0.1, 0, 0)
        [HideInInspector] _PuddleFirstColor ("First Potion Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _PuddleSecondColor ("Second Potion Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };
        CBUFFER_START(UnityPerMaterial)
            float4 _AreaColor;
            float4 _AccentColor;
            float4 _Surface;
            float4 _Animation;
            float4 _PuddleShape;
            float4 _PuddleFrame;
            float4 _PuddleBounds;
            float4 _PuddleSpread;
            float4 _PuddleFirstColor;
            float4 _PuddleSecondColor;
        CBUFFER_END

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            return output;
        }

        float Hash(float2 p)
        {
            return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
        }

        float SmoothMinimum(float a, float b, float blend)
        {
            float h = saturate(0.5 + 0.5 * (b - a) / blend);
            return lerp(b, a, h) - blend * h * (1.0 - h);
        }

        float PuddleDistance(float2 uv, float growth)
        {
            float2 flat = ((uv - 0.5) * _PuddleBounds.xy + _PuddleBounds.zw)
                * float2(1.0, 2.0) / growth;
            float2 direction = _PuddleFrame.xy;
            float2 puddlePosition = float2(dot(flat, direction), dot(flat, float2(-direction.y, direction.x)));
            float halfDistance = _PuddleShape.x;
            float first = length(puddlePosition + float2(halfDistance, 0)) - _PuddleShape.y;
            float second = length(puddlePosition - float2(halfDistance, 0)) - _PuddleShape.z;
            float bridge = length(float2(puddlePosition.x - clamp(puddlePosition.x, -halfDistance, halfDistance),
                puddlePosition.y)) - _PuddleFrame.z;
            return SmoothMinimum(SmoothMinimum(first, second, _PuddleShape.w), bridge, _PuddleShape.w);
        }

        float2 PuddleLobeDistances(float2 uv)
        {
            float2 flat = ((uv - 0.5) * _PuddleBounds.xy + _PuddleBounds.zw) * float2(1.0, 2.0);
            float2 direction = _PuddleFrame.xy;
            float2 lobePosition = float2(dot(flat, direction), dot(flat, float2(-direction.y, direction.x)));
            return float2(length(lobePosition + float2(_PuddleShape.x, 0)) - _PuddleShape.y,
                length(lobePosition - float2(_PuddleShape.x, 0)) - _PuddleShape.z);
        }

        float Noise(float2 p)
        {
            float2 cell = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
        }

        float Cracks(float2 p)
        {
            float2 cell = floor(p);
            float2 f = frac(p);
            float nearest = 8.0;
            float nextNearest = 8.0;
            [unroll] for (int y = -1; y <= 1; y++)
            {
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 neighbor = float2(x, y);
                    float2 cellOffset = float2(Hash(cell + neighbor), Hash(cell + neighbor + 19.3));
                    float distanceToPoint = length(neighbor + cellOffset - f);
                    if (distanceToPoint < nearest)
                    {
                        nextNearest = nearest;
                        nearest = distanceToPoint;
                    }
                    else nextNearest = min(nextNearest, distanceToPoint);
                }
            }
            return 1.0 - smoothstep(0.015, 0.075, nextNearest - nearest);
        }

        float Bubbles(float2 p, float t)
        {
            float2 cell = floor(p);
            float2 center = 0.3 + float2(Hash(cell), Hash(cell + 8.2)) * 0.4;
            float phase = frac(t * 0.45 + Hash(cell + 31.7));
            float radius = lerp(0.04, 0.3, phase);
            float ring = 1.0 - smoothstep(0.018, 0.045, abs(length(frac(p) - center) - radius));
            return ring * sin(phase * 3.14159);
        }

        float Sparkles(float2 p, float t)
        {
            float2 cell = floor(p);
            float2 offset = frac(p) - 0.5;
            float cross = exp(-abs(offset.x) * 55.0 - abs(offset.y) * 8.0)
                + exp(-abs(offset.y) * 55.0 - abs(offset.x) * 8.0);
            float blink = pow(saturate(sin(t * 2.0 + Hash(cell) * 6.28318)), 6.0);
            return saturate(cross) * blink * step(0.55, Hash(cell + 13.1));
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 p = input.uv * 2.0 - 1.0;
            float t = _Animation.x;
            float seed = _Animation.w;
            float appearance = _Animation.y;
            float growth = lerp(0.15, 1.0, appearance);
            float2 q = p / growth;
            float radius = length(q);
            if (_PuddleFrame.w > 0.5)
                radius = 1.0 + PuddleDistance(input.uv, growth) / min(_PuddleShape.y, _PuddleShape.z);
            float flow = Noise(q * 3.5 + float2(t * 0.4 + seed, -t * 0.3));
            float fine = Noise(q * 9.0 + float2(-t * 0.6, t * 0.5 + seed));
            // Keep the moving edge inside the renderer's bounds.
            float edge = 0.965 + (flow * 2.0 - 1.0) * _Surface.x;
            if (_PuddleFrame.w > 0.5) edge = 1.0 - flow * _Surface.x;
            float style = _Surface.w;
            float softness = max(fwidth(radius) * 1.5, style > 5.5 && style < 6.5 ? 0.09 : 0.015);
            float mask = 1.0 - smoothstep(edge - softness, edge, radius);
            float rim = smoothstep(edge - 0.09, edge - 0.025, radius) * mask;
            float ripple = pow(saturate(0.5 + 0.5 * sin(radius * 25.0 - t * 4.0 + flow * 4.0)), 8.0);
            float detail = _Surface.y;
            float pattern = ripple * 0.14;
            float shade = lerp(0.85, 0.65 + flow * 0.5, detail);
            float coverage = 1.0;
            float pulse = 1.0;
            if (style > 0.5 && style < 1.5) // Flame: moving heat and scattered embers.
            {
                float heat = Noise(q * float2(6.0, 3.0) + float2(seed, -t * 1.4));
                pattern = smoothstep(0.52, 0.82, heat) * 0.55
                    + Sparkles(q * 9.0 + float2(0, -t * 0.3), t) * 0.7;
                shade = 0.5 + heat * 0.45;
                coverage = 0.75 + heat * 0.25;
            }
            else if (style > 1.5 && style < 2.5) // Ring bubbles, like the poison / pink VFX.
            {
                pattern = Bubbles(q * 4.0 + seed, t) * 0.7;
                shade = 0.6 + flow * 0.3;
            }
            else if (style > 2.5 && style < 3.5) // Ice keeps its crystalline structure still.
            {
                pattern = Cracks(q * 4.0 + seed) * 0.6 + Sparkles(q * 7.0, t) * 0.45;
                shade = 0.65 + Noise(q * 5.0 + seed) * 0.25;
            }
            else if (style > 3.5 && style < 4.5) // Life / white orb: soft pulses and little stars.
            {
                pattern = ripple * 0.3 + Sparkles(q * 6.0 + seed, t) * 0.8;
                shade = 0.8 + flow * 0.15;
                pulse = 0.88 + sin(t * 2.5) * 0.12;
            }
            else if (style > 4.5 && style < 5.5) // Lightning changes branches in discrete bursts.
            {
                float tick = floor(t * 3.0);
                float bolts = Cracks(q * 3.5 + float2(seed, tick * 0.7));
                float flash = pow(saturate(sin(t * 12.0 + seed)), 6.0);
                pattern = bolts * (0.25 + flash * 0.75);
                shade = 0.45 + flash * 0.25;
                coverage = 0.55 + bolts * 0.45;
            }
            else if (style > 5.5 && style < 6.5) // Smoke / toxic billows stay diffuse.
            {
                float cloud = flow * 0.7 + fine * 0.3;
                pattern = smoothstep(0.55, 0.8, cloud) * 0.3;
                shade = 0.55 + cloud * 0.35;
                coverage = 0.35 + cloud * 0.65;
                rim *= 0.25;
            }
            else if (style > 6.5) // Thermal shock / thorn VFX: stable fractures with a pulse.
            {
                pattern = Cracks(q * 3.0 + seed) * (0.45 + sin(t * 3.0) * 0.15);
                shade = 0.45 + Noise(q * 5.0 + seed) * 0.25;
            }
            float highlight = saturate(rim * 0.25 + detail * pattern);
            float3 color = lerp(_AreaColor.rgb * shade, _AccentColor.rgb, highlight);
            float colorAlpha = _AreaColor.a;
            if (_PuddleSpread.z > 0.5)
            {
                float2 lobeDistances = PuddleLobeDistances(input.uv);
                float reaction = _PuddleSpread.w > 0.5 ? 1.0 : 1.0 - smoothstep(
                    _PuddleSpread.x - _PuddleSpread.y, _PuddleSpread.x,
                    max(lobeDistances.x, lobeDistances.y));
                float originalMask = 1.0 - smoothstep(-_PuddleSpread.y, 0.0,
                    min(lobeDistances.x, lobeDistances.y));
                float secondWeight = smoothstep(-_PuddleSpread.y, _PuddleSpread.y,
                    lobeDistances.x - lobeDistances.y);
                float4 originalColor = lerp(_PuddleFirstColor, _PuddleSecondColor, secondWeight);
                // Original lobes remain visible; the new neck and combined palette
                // appear only behind the advancing overlap reaction front.
                mask = lerp(originalMask, mask, reaction);
                color = lerp(originalColor.rgb * shade, color, reaction);
                colorAlpha = lerp(originalColor.a, colorAlpha, reaction);
            }
            float alpha = mask * colorAlpha * _Surface.z * coverage * pulse * appearance * _Animation.z;
            return half4(color, alpha);
        }
        ENDHLSL

        Pass
        {
            Name "PotionArea2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "PotionAreaForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
