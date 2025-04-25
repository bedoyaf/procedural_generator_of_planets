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
        while (foldouts.Count < generator.terrainLayers.Count)
            foldouts.Add(false);

        for (int i = 0; i < generator.terrainLayers.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            // Draw editable object field
            generator.terrainLayers[i] = (TerrainLayerSO)EditorGUILayout.ObjectField(
                $"Layer {i}",
                generator.terrainLayers[i],
                typeof(TerrainLayerSO),
                false
            );

            if (generator.terrainLayers[i] != null)
            {
                foldouts[i] = EditorGUILayout.Foldout(foldouts[i], "Details", true);
                if (foldouts[i])
                {
                    EditorGUI.indentLevel++;
                    CreateEditor(generator.terrainLayers[i])?.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Biome Pipeline", EditorStyles.boldLabel);

        if (generator.biomPipeline != null)
        {
            generator.showBiomeSettings = EditorGUILayout.Foldout(generator.showBiomeSettings, "Biome Settings", true);
            if (generator.showBiomeSettings)
            {
                EditorGUI.indentLevel++;
                Editor editor = CreateEditor(generator.biomPipeline);
                if (editor != null)
                {
                    editor.OnInspectorGUI();
                }
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No BiomePipeline assigned.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // Buttons
        if (GUILayout.Button("Generate Sphere")) generator.GenerateSphereMesh();
        if (GUILayout.Button("Apply Terrain")) generator.GenerateTerrain();
        if (GUILayout.Button("Generate Sphere and Terrain")) generator.GeneratePlanetAndTerrain();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(generator);
        }
    }
}
