using UnityEngine;

[CreateAssetMenu(fileName = "ThirstDialogueData", menuName = "Scriptable Objects/ThirstDialogueData")]
public sealed class ThirstDialogueData : ScriptableObject
{
    [Header("Thirst Warning Stage")]
    [SerializeField, TextArea(2, 4)]
    private string[] _warningDialogueLines;

    [Header("Thirst Critical Stage")]
    [SerializeField, TextArea(2, 4)]
    private string[] _criticalDialogueLines;

    public string GetRandomLine(ThirstWarningLevel level)
    {
        return level switch
        {
            ThirstWarningLevel.Warning => PickRandomValidLine(_warningDialogueLines),
            ThirstWarningLevel.Critical => PickRandomValidLine(_criticalDialogueLines),
            _ => string.Empty
        };
    }

    private static string PickRandomValidLine(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return string.Empty;
        }
        string result = lines[Random.Range(0, lines.Length)];
        if (result == null || string.IsNullOrWhiteSpace(result))
        {
            return string.Empty;
        }

        return result;
    }
}