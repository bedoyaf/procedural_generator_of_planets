using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// editor that helps to show relevant contents of planetSO, heavily created by chat gpt
/// </summary>
[CustomEditor(typeof(PlanetSO))]
public class PlanetSOEditor : Editor
{
    private SerializedProperty meshSettingsProp;
    private bool showMeshSettings = true;

    private PlanetSO planet;
    private List<bool> foldouts = new();

    private void OnEnable()
    {
        meshSettingsProp = serializedObject.FindProperty("meshSettings");
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
            if (prop.name == "meshSettings") continue;

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