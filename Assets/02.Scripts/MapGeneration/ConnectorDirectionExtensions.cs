using UnityEngine;

public static class ConnectorDirectionExtensions
{
    public static Vector2 ToVector2(this ConnectorDirection direction)
    {
        return direction switch
        {
            ConnectorDirection.North => Vector2.up,
            ConnectorDirection.East => Vector2.right,
            ConnectorDirection.South => Vector2.down,
            ConnectorDirection.West => Vector2.left,
            _ => Vector2.zero
        };
    }

    public static ConnectorDirection OppositeDirection(this ConnectorDirection direction)
    {
        return direction switch
        {
            ConnectorDirection.North => ConnectorDirection.South,
            ConnectorDirection.East => ConnectorDirection.West,
            ConnectorDirection.South => ConnectorDirection.North,
            ConnectorDirection.West => ConnectorDirection.East,
            _ => direction
        };
    }

    public static bool IsHorizontal(this ConnectorDirection direction)
    {
        return direction == ConnectorDirection.East || direction == ConnectorDirection.West;
    }
}