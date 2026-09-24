using UnityEngine;

/// <summary>
/// 포션 투척 궤적 계산을 위한 유틸리티 클래스
/// </summary>
public static class ThrowTrajectory
{
    public static Vector3 EvaluateGroundPoint(Vector3 startPosition, Vector3 targetPosition, float t)
    {
        return Vector3.Lerp(startPosition, targetPosition, Mathf.Clamp01(t));
    }

    public static float EvaluateHeight(float arcHeight, float t)
    {
        t = Mathf.Clamp01(t);
        return 4f * arcHeight * t * (1f - t);
    }

    public static Vector3 EvaluateVisualPoint(Vector3 startPosition, Vector3 targetPosition, float arcHeight, float t)
    {
        Vector3 groundPosition = EvaluateGroundPoint(startPosition, targetPosition, t);
        float height = EvaluateHeight(arcHeight, t);
        
        return groundPosition + Vector3.up * height;
    }
}