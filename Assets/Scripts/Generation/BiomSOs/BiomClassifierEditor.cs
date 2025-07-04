#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BiomeClassifierSO))]
public class BiomeClassifierSOEditor : Editor
{
    /* ---------- Serialized properties ---------- */
    private SerializedProperty heightsProp, heightRangesProp;
    private SerializedProperty tempsProp, tempRangesProp;
    private SerializedProperty slopesProp, slopeRangesProp;

    /* ---------- Inicializace ---------- */
    private void OnEnable()
    {
        heightsProp = serializedObject.FindProperty("heights");
        heightRangesProp = serializedObject.FindProperty("heightRanges");

        tempsProp = serializedObject.FindProperty("temperatures");
        tempRangesProp = serializedObject.FindProperty("temperaturesRanges");

        slopesProp = serializedObject.FindProperty("slopes");
        slopeRangesProp = serializedObject.FindProperty("slopeRanges");
    }

    /* ---------- Hlavní Inspector ---------- */
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Biome Attribute Lists", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(heightsProp, true);
        EditorGUILayout.PropertyField(tempsProp, true);
        EditorGUILayout.PropertyField(slopesProp, true);

        EditorGUILayout.Space(10);

        DrawAttributeSection("Height Types", heightsProp, heightRangesProp);
        EditorGUILayout.Space(6);

        DrawAttributeSection("Temperature Types", tempsProp, tempRangesProp);
        EditorGUILayout.Space(6);

        DrawAttributeSection("Slope Types", slopesProp, slopeRangesProp);

        serializedObject.ApplyModifiedProperties();
    }

    /* ---------- Jedna sekce (atributy + rozsahy) ---------- */
    private void DrawAttributeSection(string label,
                                      SerializedProperty attrList,
                                      SerializedProperty rangeList)
    {
       

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        // Ujisti se, že rangeList má stejnou délku jako attrList
        SyncRangeListSize(attrList, rangeList);

        for (int i = 0; i < attrList.arraySize; i++)
        {
            EditorGUILayout.BeginVertical("box");

            // 1) Atribut (ScriptableObject / SO)
            SerializedProperty attrElement = attrList.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(attrElement, new GUIContent($"Element {i}"));

            // 2) Rozsah (min / max)
            if (attrElement.objectReferenceValue != null)
            {
                SerializedProperty rangeElement = rangeList.GetArrayElementAtIndex(i);
                SerializedProperty minProp = rangeElement.FindPropertyRelative("min");
                SerializedProperty maxProp = rangeElement.FindPropertyRelative("max");

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(minProp, new GUIContent("Min"));
                EditorGUILayout.PropertyField(maxProp, new GUIContent("Max"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }

    /* ---------- Pomocná synchronizace velikosti ---------- */
    private static void SyncRangeListSize(SerializedProperty sourceList,
                                          SerializedProperty targetList)
    {
        if (targetList == null) return;

        while (targetList.arraySize < sourceList.arraySize)
            targetList.InsertArrayElementAtIndex(targetList.arraySize);

        while (targetList.arraySize > sourceList.arraySize)
            targetList.DeleteArrayElementAtIndex(targetList.arraySize - 1);
    }
}
#endif
