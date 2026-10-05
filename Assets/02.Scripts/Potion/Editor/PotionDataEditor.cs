using UnityEditor;

[CustomEditor(typeof(PotionData)), CanEditMultipleObjects]
public sealed class PotionDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "_vaporizationSettings");
        SerializedProperty element = serializedObject.FindProperty("_element");
        if (!element.hasMultipleDifferentValues &&
            element.intValue == (int)PotionElement.Vaporization)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("기화 전용 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_vaporizationSettings"));
        }
        serializedObject.ApplyModifiedProperties();
    }
}
