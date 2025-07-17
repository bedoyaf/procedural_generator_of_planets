using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// editor that helps to show relevant contents of planet generator, heavily created by chat gpt
/// </summary>
[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor
{
    private SerializedProperty planetSOProp;

    private bool showPlanetSOSettingsFoldout = false;

    private List<bool> terrainLayerFoldouts = new List<bool>();

    private void OnEnable()
    {
        planetSOProp = serializedObject.FindProperty("planetSO");

        PlanetGenerator generator = (PlanetGenerator)target;
        if (generator != null && generator.planetSO != null && generator.planetSO.meshSettings != null)
        {
            while (terrainLayerFoldouts.Count < generator.planetSO.meshSettings.terrainLayers.Count)
                terrainLayerFoldouts.Add(false);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        PlanetGenerator generator = (PlanetGenerator)target;

        EditorGUILayout.PropertyField(planetSOProp, true);

        EditorGUILayout.Space(10);

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.name == "m_Script" ||
                iterator.name == "planetSO" ||
                iterator.name == "waterSettings" ||
                iterator.name == "waterGameObject" ||
                iterator.name == "waterIceLineStart"||
                iterator.name == "waterIceLineEnd")
            {
                continue;
            }

            EditorGUILayout.PropertyField(iterator, true);
        }

        EditorGUILayout.Space(10);

        if (planetSOProp != null && planetSOProp.objectReferenceValue != null)
        {
            showPlanetSOSettingsFoldout = EditorGUILayout.Foldout(showPlanetSOSettingsFoldout, "PlanetSO Details", true);
            if (showPlanetSOSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                SerializedObject nestedPlanetSO = new SerializedObject(planetSOProp.objectReferenceValue);
                SerializedProperty currentProp = nestedPlanetSO.GetIterator();

                bool hasWater = false;
                SerializedProperty hasWaterProp = nestedPlanetSO.FindProperty("hasWater");
                if (hasWaterProp != null) hasWater = hasWaterProp.boolValue;

                if (currentProp.NextVisible(true))
                {
                    do
                    {
                        if (currentProp.name == "m_Script") continue;
                        if ((currentProp.name == "waterSettings" || currentProp.name == "waterIceLineStart" || currentProp.name == "waterIceLineEnd" || currentProp.name == "waterColor"||currentProp.name == "IceColor") && !hasWater) continue;

                        EditorGUILayout.PropertyField(currentProp, true);
                    }
                    while (currentProp.NextVisible(false));
                }

                nestedPlanetSO.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Terrain Layers", EditorStyles.boldLabel);

        if (generator.planetSO != null && generator.planetSO.meshSettings != null && generator.planetSO.meshSettings.terrainLayers != null)
        {
            // Znovu zajistíme správnou velikost seznamu foldoutù
            while (terrainLayerFoldouts.Count < generator.planetSO.meshSettings.terrainLayers.Count)
                terrainLayerFoldouts.Add(false);

            for (int i = 0; i < generator.planetSO.meshSettings.terrainLayers.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                generator.planetSO.meshSettings.terrainLayers[i] = (TerrainLayerSO)EditorGUILayout.ObjectField(
                    $"Layer {i}",
                    generator.planetSO.meshSettings.terrainLayers[i],
                    typeof(TerrainLayerSO),
                    false
                );

                if (generator.planetSO.meshSettings.terrainLayers[i] != null)
                {
                    terrainLayerFoldouts[i] = EditorGUILayout.Foldout(terrainLayerFoldouts[i], "Details", true);
                    if (terrainLayerFoldouts[i])
                    {
                        EditorGUI.indentLevel++;
                        CreateEditor(generator.planetSO.meshSettings.terrainLayers[i])?.OnInspectorGUI();
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a PlanetSO and ensure its Mesh Settings are configured to see Terrain Layers.", MessageType.Info);
        }
        EditorGUILayout.Space();

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset All")) generator.ResetAll();
    //    if (GUILayout.Button("Generate Sphere")) generator.GenerateSphereMesh();
        if (GUILayout.Button("Generate Planet")) generator.GeneratePlanet();
     //   if (GUILayout.Button("Generate Sphere and Terrain and Water")) generator.GeneratePlanetAndTerrainWater();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(generator);
            if (generator.planetSO != null)
            {
                EditorUtility.SetDirty(generator.planetSO);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}