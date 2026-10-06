Shader "Potion/Area"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _AreaClipMask ("Clipped Footprint", 2D) = "white" {}
        [HideInInspector] _AreaClipRect ("Footprint World Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _AreaClipEnabled ("Clip Enabled", Float) = 0
        _AreaColor ("Area Color", Color) = (0.3, 0.8, 0.6, 1)
        _AccentColor ("VFX Accent Color", Color) = (0.7, 1, 0.8, 1)
        _Surface ("Wobble, Detail, Opacity, Style", Vector) = (0.018, 0.65, 0.7, 0)
        _Animation ("Time, Appearance, Fade, Seed", Vector) = (0, 1, 1, 0)
        _Boundary ("Irregularity, Density, Motion Speed, Motion Amount", Vector) = (0.17, 4.5, 0.35, 0.45)
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
        #include "PotionBlazingCurrent.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 positionWS : TEXCOORD1;
        };
        CBUFFER_START(UnityPerMaterial)
            float4 _AreaClipRect;
            float _AreaClipEnabled;
            float4 _AreaColor;
            float4 _AccentColor;
            float4 _Surface;
            float4 _Animation;
            float4 _Boundary;
            float4 _PuddleShape;
            float4 _PuddleFrame;
            float4 _PuddleBounds;
            float4 _PuddleSpread;
            float4 _PuddleFirstColor;
            float4 _PuddleSecondColor;
        CBUFFER_END
        TEXTURE2D(_AreaClipMask);
        SAMPLER(sampler_AreaClipMask);

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.positionWS = TransformObjectToWorld(input.positionOS).xy;
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

        float BoundaryNoise(float2 p, float angular)
        {
            float2 cell = floor(p);
            float2 f = frac(p);
            // Linear interpolation gives frost a faceted edge; liquids keep round lobes.
            f = lerp(f * f * (3.0 - 2.0 * f), f, angular);
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

        float AbsorptionWave(float radius, float t)
        {
            float phase = frac(t * 0.45);
            float waveRadius = 1.0 - phase;
            return exp(-pow((radius - waveRadius) * 22.0, 2.0)) * sin(phase * 3.14159);
        }

        half4 Frag(Varyings input) : SV_Target
        {
            if (_AreaClipEnabled > 0.5)
            {
                float2 clipUV = (input.positionWS - _AreaClipRect.xy) / _AreaClipRect.zw;
                clip(min(min(clipUV.x, clipUV.y), min(1.0 - clipUV.x, 1.0 - clipUV.y)));
                clip(SAMPLE_TEXTURE2D(_AreaClipMask, sampler_AreaClipMask, clipUV).r - 0.5);
            }
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
            float style = _Surface.w;
            float2 boundaryPosition = q;
            float boundaryStrength = clamp(_Boundary.x, 0.0, 0.2);
            if (_PuddleFrame.w > 0.5)
            {
                float2 flat = ((input.uv - 0.5) * _PuddleBounds.xy + _PuddleBounds.zw)
                    * float2(1.0, 2.0) / growth;
                float smallerRadius = min(_PuddleShape.y, _PuddleShape.z);
                boundaryPosition = flat / smallerRadius;
                float along = abs(dot(flat, _PuddleFrame.xy));
                float neck = (1.0 - smoothstep(_PuddleShape.x * 0.65,
                    _PuddleShape.x + smallerRadius * 0.15, along)) * step(0.001, _PuddleShape.x);
                boundaryStrength *= lerp(1.0, 0.2, neck);
            }
            // Every potion now shares broad rounded lobes, with smaller liquid ripples.
            float density = max(2.0, _Boundary.y);
            float2 drift = float2(t * _Boundary.z * 0.7, -t * _Boundary.z * 0.5) * _Boundary.w;
            float broadEdge = BoundaryNoise(boundaryPosition * density * 0.55 + seed + drift, 0.0);
            float smallEdge = BoundaryNoise(boundaryPosition * density * 1.7 + seed + 23.7 - drift, 0.0);
            float irregular = saturate(0.5 + (broadEdge - 0.5) * 1.3 + (smallEdge - 0.5) * 0.35);
            float motion = sin(dot(boundaryPosition, float2(4.7, 3.1))
                + t * _Boundary.z + seed) * 0.5 + 0.5;
            float movingAmount = saturate(_Boundary.w);
            irregular = lerp(irregular, motion, movingAmount);
            // Deform inward only: the gameplay collider and the final wall mask stay intact.
            float edge = (_PuddleFrame.w > 0.5 ? 1.0 : 0.985)
                - boundaryStrength * saturate(irregular)
                - min(_Surface.x, boundaryStrength * 0.15) * motion;
            float edgeSoftness = (style > 2.5 && style < 3.5) || (style > 14.5 && style < 15.5) ? 0.045
                : style > 9.5 ? 0.06 + (1.0 - fine) * 0.035
                : style > 5.5 && style < 6.5 ? 0.09 : 0.015;
            float softness = max(fwidth(radius) * 1.5, edgeSoftness);
            float mask = 1.0 - smoothstep(edge - softness, edge, radius);
            float rim = smoothstep(edge - 0.09, edge - 0.025, radius) * mask;
            float ripple = pow(saturate(0.5 + 0.5 * sin(radius * 25.0 - t * 4.0 + flow * 4.0)), 8.0);
            float detail = _Surface.y;
            float pattern = ripple * 0.14;
            float shade = lerp(0.85, 0.65 + flow * 0.5, detail);
            float coverage = 1.0;
            float pulse = 1.0;
            float thermalFissures = 0.0;
            float currentArc = 0.0;
            float currentCharge = 0.0;
            float absorption = 0.0;
            float elementHighlight = 0.0;
            if (style > 0.5 && style < 1.5) // Flame: moving heat and scattered embers.
            {
                float heat = Noise(q * float2(6.0, 3.0) + float2(seed, -t * 1.4));
                pattern = smoothstep(0.52, 0.82, heat) * 0.55
                    + Sparkles(q * 9.0 + float2(0, -t * 0.3), t) * 0.7;
                shade = 0.5 + heat * 0.45;
                coverage = 0.75 + heat * 0.25;
            }
            else if (style > 1.5 && style < 2.5) // Poison: dark pools with bright bubbling rings.
            {
                pattern = Bubbles(q * 3.5 + seed, t) * 1.25;
                shade = 0.45 + flow * 0.35;
            }
            else if (style > 2.5 && style < 3.5) // Freeze wave, cold billows and drifting ice motes.
            {
                float frozen = Noise(q * 5.0 + seed);
                float cracks = Cracks(q * 3.5 + seed);
                float mist = Noise(q * 4.5 + float2(t * 0.38 + seed, -t * 0.3));
                float billow = Noise(q * 7.0 + float2(-t * 0.26, t * 0.4 + seed));
                float mistBand = smoothstep(0.3, 0.68, radius)
                    * (1.0 - smoothstep(0.82, 1.0, radius));
                // The first flash follows the spreading floor; later waves stay soft.
                float birth = sin(saturate(appearance) * 3.14159);
                float freezeFront = exp(-pow((radius - 0.86) * 24.0, 2.0)) * birth;
                float waveRadius = frac(t * 0.55);
                float wave = exp(-pow((radius - waveRadius) * 22.0, 2.0))
                    * sin(waveRadius * 3.14159) * (0.55 + mist * 0.45);
                float motes = Sparkles(q * 10.0 + float2(seed + t * 0.15, -t * 0.5), t * 1.4)
                    * smoothstep(0.18, 0.5, radius);
                float coldCloud = mistBand * smoothstep(0.26, 0.74, mist * 0.7 + billow * 0.3);
                pattern = cracks * 0.45 + motes * 0.42 + coldCloud * 0.85
                    + freezeFront * 0.9 + wave * 0.38;
                shade = 0.65 + frozen * 0.22 + birth * 0.08;
                coverage = saturate(0.4 + frozen * 0.2 + cracks * 0.12 + coldCloud * 0.32
                    + freezeFront * 0.22 + wave * 0.1 + motes * 0.2);
                rim *= 0.6;
            }
            else if (style > 3.5 && style < 4.5) // Healing: broad outward ripples and a soft central glow.
            {
                float wave = pow(saturate(0.5 + 0.5 * sin(radius * 12.0 - t * 1.6)), 4.0);
                float glow = exp(-radius * radius * 4.0);
                pattern = wave * 0.85 + glow * 0.35;
                shade = 0.92 + flow * 0.08;
                pulse = 0.96 + sin(t * 1.5) * 0.04;
                rim *= 1.5;
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
            else if (style > 13.5 && style < 16.5) // Preserve each element under inward healing waves.
            {
                absorption = AbsorptionWave(radius, t);
                float core = exp(-radius * radius * 9.0);
                if (style < 14.5) // Poison's original pool and expanding bubbles.
                {
                    float bubbles = Bubbles(q * 3.5 + seed, t);
                    elementHighlight = bubbles;
                    pattern = bubbles * 0.8 + absorption * 0.65 + core * 0.15;
                    shade = 0.45 + flow * 0.35;
                    coverage = saturate(0.6 + flow * 0.15 + bubbles * 0.18 + absorption * 0.16);
                }
                else if (style < 15.5) // Frost's frozen facets, fissures and low cold clouds.
                {
                    float frozen = Noise(q * 5.0 + seed);
                    float fissures = Cracks(q * 3.5 + seed);
                    float mist = Noise(q * 4.5 + float2(t * 0.38 + seed, -t * 0.3));
                    float coldBand = smoothstep(0.3, 0.68, radius)
                        * (1.0 - smoothstep(0.82, 1.0, radius));
                    elementHighlight = fissures * 0.55 + coldBand * mist * 0.45;
                    pattern = elementHighlight + absorption * 0.65
                        + Sparkles(q * 10.0 + seed, t) * 0.3;
                    shade = 0.65 + frozen * 0.22;
                    coverage = saturate(0.48 + frozen * 0.18 + coldBand * mist * 0.22 + absorption * 0.15);
                }
                else // Familiar purple branches conduct pale green recovery pulses.
                {
                    float tick = floor(t * 3.0);
                    float bolts = Cracks(q * 3.5 + float2(seed, tick * 0.7));
                    float flash = pow(saturate(sin(t * 12.0 + seed)), 6.0);
                    currentArc = CurrentArc(q, t, seed) * 0.9;
                    elementHighlight = bolts * (0.38 + flash * 0.95) + currentArc;
                    pattern = elementHighlight + absorption * 0.6 + core * 0.2;
                    shade = 0.45 + flow * 0.2;
                    coverage = saturate(0.5 + bolts * 0.32 + currentArc * 0.2 + absorption * 0.2);
                }
                rim *= 0.4;
            }
            else if (style > 12.5 && style < 13.5) // Conductive heat with short local discharges.
            {
                currentArc = CurrentArc(q, t, seed);
                currentCharge = CurrentDischarge(q, t, seed);
                float heat = Noise(q * 4.0 + seed);
                float coals = smoothstep(0.5, 0.8, fine) * smoothstep(0.35, 0.65, heat);
                pattern = coals * 0.3 + currentArc * 0.9;
                shade = 0.42 + heat * 0.3;
                coverage = saturate(0.62 + heat * 0.12 + coals * 0.15 + currentArc * 0.25);
                rim *= 0.2;
            }
            else if (style > 11.5 && style < 12.5) // Vampiric fire: burning coals threaded with healing ripples.
            {
                float coals = smoothstep(0.5, 0.85, fine);
                float rings = AbsorptionWave(radius, t);
                float core = exp(-radius * radius * 7.0);
                pattern = rings * 0.85 + core * 0.4 + coals * 0.25 + Sparkles(q * 7.0 + seed, t) * 0.2;
                shade = 0.5 + flow * 0.25;
                coverage = saturate(0.48 + flow * 0.15 + rings * 0.2 + coals * 0.12);
                rim *= 0.3;
            }
            else if (style > 10.5) // Thermal shock: fractured ground, pressure waves and steam.
            {
                float frozen = Noise(q * 5.0 + seed);
                float fissures = Cracks(q * 3.5 + seed);
                thermalFissures = fissures;
                float steam = Noise(q * 4.5 + float2(t * 0.38 + seed, -t * 0.5));
                float steamBand = smoothstep(0.2, 0.55, radius)
                    * (1.0 - smoothstep(0.84, 1.0, radius));
                float shockRadius = frac(t * 0.9);
                float shock = exp(-pow((radius - shockRadius) * 18.0, 2.0))
                    * pow(1.0 - shockRadius, 1.5);
                pattern = fissures * 0.65 + steamBand * smoothstep(0.3, 0.75, steam) * 0.65
                    + shock * 1.1;
                shade = 0.45 + frozen * 0.25;
                coverage = saturate(0.5 + frozen * 0.16 + fissures * 0.12 + steamBand * steam * 0.2 + shock * 0.16);
                rim *= 0.45;
            }
            else if (style > 9.5) // Fire: visible heated footprint, soot and scattered coals.
            {
                float soot = Noise(q * 4.0 + seed);
                float coals = smoothstep(0.58, 0.85, fine) * smoothstep(0.25, 0.65, soot);
                float boundary = smoothstep(edge - 0.16, edge - 0.035, radius);
                pattern = coals * 0.4 + boundary * (0.1 + fine * 0.35);
                shade = 0.4 + soot * 0.3;
                // A continuous translucent base makes the entire effect radius readable.
                coverage = saturate(0.64 + soot * 0.12 + boundary * 0.18 + coals * 0.16);
                pulse = 1.0;
                rim = 0.0;
            }
            else if (style > 7.5) // Healing spring: transparent water, foam and expanding rings.
            {
                float wave = frac(radius * 2.7 - t * 0.55);
                float rings = exp(-pow((wave - 0.5) * 19.0, 2.0));
                float glow = exp(-radius * radius * 14.0);
                float foam = smoothstep(0.55, 0.85, fine) * smoothstep(0.5, 0.9, radius);
                float glints = Sparkles(q * 6.0 + seed, t);
                pattern = rings * 0.85 + glow * 0.8 + foam * 0.5 + glints;
                coverage = 0.12 + rings * 0.4 + glow * 0.65 + foam * 0.25 + glints * 0.5;
                shade = 0.9 + flow * 0.1;
                pulse = 0.95 + sin(t * 1.7) * 0.05;
                rim *= 0.65;
            }
            else if (style > 6.5) // Thermal shock / thorn VFX: stable fractures with a pulse.
            {
                pattern = Cracks(q * 3.0 + seed) * (0.45 + sin(t * 3.0) * 0.15);
                shade = 0.45 + Noise(q * 5.0 + seed) * 0.25;
            }
            float highlight = saturate(rim * 0.25 + detail * pattern);
            float3 color = lerp(_AreaColor.rgb * shade, _AccentColor.rgb, highlight);
            if (style > 13.5 && style < 16.5)
            {
                float3 elementColor = style < 14.5 ? float3(0.7, 0.88, 0.08)
                    : style < 15.5 ? float3(0.72, 0.89, 1.0) : float3(0.83, 0.64, 1.0);
                color = lerp(_AreaColor.rgb * shade, elementColor, saturate(elementHighlight * detail));
                float core = exp(-radius * radius * 9.0);
                color = lerp(color, _AccentColor.rgb, saturate(absorption * 0.65 + core * 0.22));
                if (style > 15.5)
                    color = lerp(color, float3(0.93, 0.82, 1.0), saturate(currentArc * 0.8));
            }
            else if (style > 12.5 && style < 13.5)
            {
                float3 heat = lerp(_AreaColor.rgb * shade, float3(0.8, 0.2, 0.045),
                    smoothstep(0.4, 0.8, fine) * (0.3 + currentCharge * 0.15));
                // Violet corona surrounds the warm discharge; the core stays golden-white.
                float3 corona = lerp(float3(0.62, 0.2, 0.88), _AccentColor.rgb,
                    smoothstep(0.35, 1.0, currentArc));
                color = lerp(heat, corona, saturate(currentArc * 0.7));
                color = lerp(color, float3(1.0, 0.95, 0.76), saturate(currentArc - 0.3));
            }
            else if (style > 11.5 && style < 12.5)
            {
                float core = exp(-radius * radius * 7.0);
                float rings = AbsorptionWave(radius, t);
                float3 heat = lerp(_AreaColor.rgb * shade, float3(0.95, 0.26, 0.055), flow * 0.5);
                float3 healing = lerp(float3(0.2, 0.72, 0.68), _AccentColor.rgb, core * 0.35);
                color = lerp(heat, healing, saturate(core * 0.7 + rings * 0.65));
            }
            else if (style > 10.5)
            {
                float fissures = thermalFissures;
                float heat = smoothstep(0.3, 0.8, flow);
                float shockRadius = frac(t * 0.9);
                float shock = exp(-pow((radius - shockRadius) * 18.0, 2.0))
                    * pow(1.0 - shockRadius, 1.5);
                float3 ash = lerp(_AreaColor.rgb * shade, _AccentColor.rgb, saturate(detail * pattern) * 0.3);
                float3 fracture = lerp(float3(0.9, 0.23, 0.06), _AccentColor.rgb,
                    saturate(shock * 0.85 + (1.0 - heat) * 0.25));
                color = lerp(ash, fracture, saturate(fissures * (0.5 + heat * 0.25) + shock * 0.45));
            }
            else if (style > 9.5)
            {
                float soot = Noise(q * 4.0 + seed);
                float boundary = smoothstep(edge - 0.16, edge - 0.035, radius);
                float3 ash = lerp(float3(0.24, 0.025, 0.018), _AreaColor.rgb, soot);
                float3 heatColor = lerp(float3(0.48, 0.055, 0.025), float3(0.8, 0.2, 0.055), flow);
                float3 edgeColor = lerp(heatColor, _AccentColor.rgb, fine * 0.18);
                color = lerp(ash, heatColor, 0.2 + smoothstep(0.25, 0.8, flow) * 0.25);
                color = lerp(color, edgeColor, boundary * (0.12 + fine * 0.25));
                color = lerp(color, _AccentColor.rgb, saturate(detail * pattern) * 0.22);
            }
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
