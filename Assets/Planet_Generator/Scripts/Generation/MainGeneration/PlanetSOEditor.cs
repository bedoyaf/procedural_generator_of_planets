using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// editor that helps to show relevant contents of planetSO, heavily created by chatgpt
/// </summary>
[CustomEditor(typeof(PlanetSO))]
public class PlanetSOEditor : Editor
{
    private SerializedProperty waterSettingsProp;
    private SerializedProperty meshSettingsProp;
    private SerializedProperty waterIceLineStartProp;
    private SerializedProperty waterIceLineEndProp;
    private SerializedProperty waterColorProp;
    private SerializedProperty iceColorProp;
    private bool showMeshSettings = true;
    private bool showWaterSettingsFoldout = false;

    private PlanetSO planet;
    private List<bool> foldouts = new();

    private void OnEnable()
    {
        waterSettingsProp = serializedObject.FindProperty("waterSettings");
        meshSettingsProp = serializedObject.FindProperty("meshSettings");
        waterIceLineStartProp = serializedObject.FindProperty("waterIceLineStart");
        waterIceLineEndProp = serializedObject.FindProperty("waterIceLineEnd");
        waterColorProp = serializedObject.FindProperty("waterColor");
        iceColorProp = serializedObject.FindProperty("IceColor");
        planet = (PlanetSO)target;

        if (planet.meshSettings != null)
        {
            int count = planet.meshSettings.terrainLayers.Count;
            while (foldouts.Count < count)
                foldouts.Add(false);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script") // <- skript nechceme upravovat
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(prop, true);
                GUI.enabled = true;
                continue;
            }
            if (prop.name == "meshSettings" || prop.name == "waterSettings" ||
                prop.name == "waterIceLineStart" || prop.name == "waterIceLineEnd"||
                prop.name == "waterColor" || prop.name == "IceColor"
                )
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        EditorGUILayout.Space(10);

        if (meshSettingsProp != null)
        {
            showMeshSettings = EditorGUILayout.Foldout(showMeshSettings, "Mesh Settings", true);
            if (showMeshSettings)
            {
                EditorGUI.indentLevel++;

                SerializedProperty currentProperty = meshSettingsProp.Copy();

                bool canEnterChildren = true;
                while (currentProperty.NextVisible(canEnterChildren) && currentProperty.depth == meshSettingsProp.depth + 1)
                {
                    canEnterChildren = false;
                    EditorGUILayout.PropertyField(currentProperty, true);
                }

                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.Space(10);
        if (planet.hasWater && waterSettingsProp != null)
        {
            EditorGUILayout.LabelField("Water Settings", EditorStyles.boldLabel);
            showWaterSettingsFoldout = EditorGUILayout.Foldout(showWaterSettingsFoldout, "Water Mesh Settings", true);
            if (showWaterSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                SerializedProperty currentProperty = waterSettingsProp.Copy();

                bool canEnterChildren = true;
                while (currentProperty.NextVisible(canEnterChildren) && currentProperty.depth == waterSettingsProp.depth + 1)
                {
                    canEnterChildren = false;
                    EditorGUILayout.PropertyField(currentProperty, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(waterIceLineStartProp, true);
            EditorGUILayout.PropertyField(waterIceLineEndProp, true);
            EditorGUILayout.PropertyField(waterColorProp, true);
            EditorGUILayout.PropertyField(iceColorProp, true);
        }

        EditorGUILayout.Space(10);

        if (planet.meshSettings != null && planet.meshSettings.terrainLayers != null)
        {
            EditorGUILayout.LabelField("Terrain Layers", EditorStyles.boldLabel);

            while (foldouts.Count < planet.meshSettings.terrainLayers.Count)
                foldouts.Add(false);

            for (int i = 0; i < planet.meshSettings.terrainLayers.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                planet.meshSettings.terrainLayers[i] = (TerrainLayerSO)EditorGUILayout.ObjectField(
                    $"Layer {i}",
                    planet.meshSettings.terrainLayers[i],
                    typeof(TerrainLayerSO),
                    false
                );

                if (planet.meshSettings.terrainLayers[i] != null)
                {
                    foldouts[i] = EditorGUILayout.Foldout(foldouts[i], "Details", true);
                    if (foldouts[i])
                    {
                        EditorGUI.indentLevel++;
                        CreateEditor(planet.meshSettings.terrainLayers[i])?.OnInspectorGUI();
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}