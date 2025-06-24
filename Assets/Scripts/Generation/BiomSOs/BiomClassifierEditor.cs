// BiomeClassifierEditor.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BiomeClassifierSO))]
public class BiomeClassifierEditor : Editor
{
    private bool showHeights = true;
    private bool showTemps = true;
    private bool showSlopes = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        BiomeClassifierSO data = (BiomeClassifierSO)target;

        showHeights = EditorGUILayout.Foldout(showHeights, "Height Ranges", true);
        if (showHeights)
        {
            EditorGUI.indentLevel++;
            data.ocean = DrawFloatRange("Ocean", data.ocean);
            data.lowHeight = DrawFloatRange("Low", data.lowHeight);
            data.mediumHeight = DrawFloatRange("Medium", data.mediumHeight);
            data.highHeight = DrawFloatRange("High", data.highHeight);
            data.mountainHeight = DrawFloatRange("Mountain", data.mountainHeight);
            EditorGUI.indentLevel--;
        }

        showTemps = EditorGUILayout.Foldout(showTemps, "Temperature Ranges", true);
        if (showTemps)
        {
            EditorGUI.indentLevel++;
            data.coldTemp = DrawFloatRange("Cold", data.coldTemp);
            data.temperateTemp = DrawFloatRange("Temperate", data.temperateTemp);
            data.hotTemp = DrawFloatRange("Hot", data.hotTemp);
            EditorGUI.indentLevel--;
        }

        showSlopes = EditorGUILayout.Foldout(showSlopes, "Slope Ranges", true);
        if (showSlopes)
        {
            EditorGUI.indentLevel++;
            data.flatSlope = DrawFloatRange("Flat", data.flatSlope);
            data.mildlySteepSlope = DrawFloatRange("Mild", data.mildlySteepSlope);
            data.steepSlope = DrawFloatRange("Steep", data.steepSlope);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }

    private FloatRange DrawFloatRange(string label, FloatRange range)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        range.min = EditorGUILayout.FloatField("Min", range.min);
        range.max = EditorGUILayout.FloatField("Max", range.max);
        return range;
    }
}
