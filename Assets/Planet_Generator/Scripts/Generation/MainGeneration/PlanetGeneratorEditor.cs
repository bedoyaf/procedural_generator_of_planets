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
    private SerializedProperty waterSettingsProp;
    private SerializedProperty waterGameObjectProp;
    private SerializedProperty waterMaterialProp;
    private SerializedProperty materialDiscreteMax8Prop;
    private SerializedProperty materialDiscreteTriplingProp;
    private SerializedProperty materialSmoothMax8Prop;
    private SerializedProperty materialSmoothTriplingProp;

    private bool showPlanetSOSettingsFoldout = false;
    private bool showWaterSettingsFoldout = false;
    private bool showMaterialSettingsFoldout = false;

    private List<bool> terrainLayerFoldouts = new List<bool>();

    private void OnEnable()
    {
        planetSOProp = serializedObject.FindProperty("planetSO");
        waterSettingsProp = serializedObject.FindProperty("waterSettings");
        waterGameObjectProp = serializedObject.FindProperty("waterGameObject");
        waterMaterialProp = serializedObject.FindProperty("waterMaterial");
        materialDiscreteMax8Prop = serializedObject.FindProperty("materialDiscreteMax8");
        materialDiscreteTriplingProp = serializedObject.FindProperty("materialDiscreteTripling");
        materialSmoothMax8Prop = serializedObject.FindProperty("materialSmoothMax8");
        materialSmoothTriplingProp = serializedObject.FindProperty("materialSmoothTripling");

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
                iterator.name == "waterMaterial" ||
                iterator.name == "materialDiscreteMax8" ||
                iterator.name == "materialDiscreteTripling" ||
                iterator.name == "materialSmoothMax8" ||
                iterator.name == "materialSmoothTripling")
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

                if (currentProp.NextVisible(true))
                {
                    do
                    {
                        if (currentProp.name == "m_Script") continue;

                        EditorGUILayout.PropertyField(currentProp, true);
                    }
                    while (currentProp.NextVisible(false));
                }

                nestedPlanetSO.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space(10);

        showWaterSettingsFoldout = EditorGUILayout.Foldout(showWaterSettingsFoldout, "Water Settings", true);
        if (showWaterSettingsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(waterSettingsProp, true);
            EditorGUILayout.PropertyField(waterGameObjectProp, true);
            EditorGUILayout.PropertyField(waterMaterialProp, true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        showMaterialSettingsFoldout = EditorGUILayout.Foldout(showMaterialSettingsFoldout, "Material Settings", true);
        if (showMaterialSettingsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(materialDiscreteMax8Prop, true);
            EditorGUILayout.PropertyField(materialDiscreteTriplingProp, true);
            EditorGUILayout.PropertyField(materialSmoothMax8Prop, true);
            EditorGUILayout.PropertyField(materialSmoothTriplingProp, true);
            EditorGUI.indentLevel--;
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
        if (GUILayout.Button("Generate Sphere")) generator.GenerateSphereMesh();
        if (GUILayout.Button("Generate Sphere and Terrain")) generator.GeneratePlanetAndTerrain();
        if (GUILayout.Button("Generate Sphere and Terrain and Water")) generator.GeneratePlanetAndTerrainWater();

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