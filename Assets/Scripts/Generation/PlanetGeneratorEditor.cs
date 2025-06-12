using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor
{
    private List<bool> foldouts = new List<bool>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PlanetGenerator generator = (PlanetGenerator)target;

        // Draw Terrain Layers Section
        EditorGUILayout.LabelField("Terrain Layers", EditorStyles.boldLabel);

        // Make sure the foldout list is the correct size
        while (foldouts.Count < generator.planetSettings.terrainLayers.Count)
            foldouts.Add(false);

        for (int i = 0; i < generator.planetSettings.terrainLayers.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            // Draw editable object field
            generator.planetSettings.terrainLayers[i] = (TerrainLayerSO)EditorGUILayout.ObjectField(
                $"Layer {i}",
                generator.planetSettings.terrainLayers[i],
                typeof(TerrainLayerSO),
                false
            );

            if (generator.planetSettings.terrainLayers[i] != null)
            {
                foldouts[i] = EditorGUILayout.Foldout(foldouts[i], "Details", true);
                if (foldouts[i])
                {
                    EditorGUI.indentLevel++;
                    CreateEditor(generator.planetSettings.terrainLayers[i])?.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();

        EditorGUILayout.Space();

        // Buttons
        if (GUILayout.Button("Generate Sphere")) generator.GenerateSphereMesh();
        if (GUILayout.Button("Apply Terrain")) generator.GenerateTerrain(generator.planetSettings,generator.planetData);
        if (GUILayout.Button("Generate Sphere and Terrain")) generator.GeneratePlanetAndTerrain();
        if (GUILayout.Button("Generate Sphere and Terrain and Water")) generator.GeneratePlanetAndTerrainWater();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(generator);
        }
    }
}
