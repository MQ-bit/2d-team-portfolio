#ifndef POTION_BLAZING_CURRENT_INCLUDED
#define POTION_BLAZING_CURRENT_INCLUDED

float CurrentHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// Each small region charges independently; no full-area synchronized strobe.
float CurrentDischarge(float2 p, float time, float seed)
{
    float2 cell = floor(p * 2.1 + seed);
    float clock = time * 1.65 + CurrentHash(cell + 7.3) * 2.0;
    float cycle = floor(clock);
    float phase = frac(clock);
    float envelope = smoothstep(0.0, 0.055, phase)
        * (1.0 - smoothstep(0.13, 0.4, phase));
    return envelope * step(0.35, CurrentHash(cell + cycle * 13.7));
}

// A short, broken, branching arc rather than a continuously glowing crack grid.
float CurrentArc(float2 p, float time, float seed)
{
    float2 grid = p * 2.1 + seed;
    float2 cell = floor(grid);
    float2 uv = frac(grid);
    float cycle = floor(time * 1.65 + CurrentHash(cell + 7.3) * 2.0);
    float segment = uv.x * 6.0;
    float index = floor(segment);
    float a = CurrentHash(cell + float2(index * 3.7, cycle * 5.1));
    float b = CurrentHash(cell + float2((index + 1.0) * 3.7, cycle * 5.1));
    float arcHeight = 0.5 + (lerp(a, b, frac(segment)) - 0.5) * 0.42;
    float branch = arcHeight + (uv.x - 0.48) * lerp(-0.9, 0.9, CurrentHash(cell + cycle));
    float distanceToArc = min(abs(uv.y - arcHeight), abs(uv.y - branch) + abs(uv.x - 0.55) * 0.12);
    float width = max(fwidth(uv.y) * 1.2, 0.008);
    float core = 1.0 - smoothstep(width, width * 2.5, distanceToArc);
    float glow = exp(-distanceToArc * 24.0) * 0.28;
    float ends = smoothstep(0.03, 0.18, uv.x) * (1.0 - smoothstep(0.82, 0.97, uv.x));
    return (core + glow) * ends * CurrentDischarge(p, time, seed);
}

#endif
