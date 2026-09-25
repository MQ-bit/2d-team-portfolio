using UnityEngine;

public static class BezierCalculator
{
    /// <summary>
    /// 베지어 곡선의 control point를 계산하는 함수. start와 end를 연결하는 직선에 수직인 방향으로 arcHeight만큼 이동한 점을 반환.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="end">끝 위치</param>
    /// <param name="arcHeight">아치 높이</param>
    /// <param name="direction">방향</param>
    /// <returns>control point</returns>
    public static Vector3 CalculateSideControlPoint(Vector3 start, Vector3 end, float arcHeight, Vector3 perpendicular = default)
    {

        Vector3 direction = end - start;

        if (perpendicular == default)
        {
            perpendicular = new Vector3(-direction.y, direction.x, 0f);
        }

        if (perpendicular.sqrMagnitude > 0f)
        {
            perpendicular.Normalize();
        }

        return (start + end) * 0.5f + perpendicular * arcHeight;
    }

    /// <summary>
    /// 2차 베지어 곡선을 계산하는 함수.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="control">control point</param>
    /// <param name="end">끝 위치</param>
    /// <param name="t">시간 (0~1)</param>
    /// <returns>곡선 상의 위치</returns>
    public static Vector3 CalculateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverseT = 1f - t;

        return inverseT * inverseT * start + 2f * inverseT * t * control + t * t * end;
    }

    public static Vector3 CalculateWorldUpControlPoint(Vector3 start, Vector3 end, float arcHeight)
    {
        return (start + end) * 0.5f + Vector3.up * arcHeight; //2D 화면에서 위로 살짝 솟았다가 떨어지는 형태.
    }

}
