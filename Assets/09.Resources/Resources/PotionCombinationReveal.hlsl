#ifndef POTION_COMBINATION_REVEAL_INCLUDED
#define POTION_COMBINATION_REVEAL_INCLUDED

// Match the ground reaction front at the fixed local anchor of each upper effect.
float PotionCombinationReveal(float2 root)
{
    if (_PuddleFrame.w < 0.5 || _PuddleSpread.z < 0.5 || _PuddleSpread.w > 0.5) return 1.0;
    float2 flat = (root * _PuddleBounds.xy + _PuddleBounds.zw) * float2(1.0, 2.0);
    float2 direction = _PuddleFrame.xy;
    float2 p = float2(dot(flat, direction), dot(flat, float2(-direction.y, direction.x)));
    float first = length(p + float2(_PuddleShape.x, 0)) - _PuddleShape.y;
    float second = length(p - float2(_PuddleShape.x, 0)) - _PuddleShape.z;
    float softness = max(0.0001, _PuddleSpread.y);
    return 1.0 - smoothstep(_PuddleSpread.x - softness, _PuddleSpread.x, max(first, second));
}

#endif
