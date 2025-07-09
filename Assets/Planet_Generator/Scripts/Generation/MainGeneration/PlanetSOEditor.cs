using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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

        // Init foldouts if needed
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

        // Draw everything except meshSettings manually
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            // Pøeskoèíme meshSettings, abychom ho vykreslili ruènì
            if (prop.name == "meshSettings") continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        EditorGUILayout.Space(10);

        // Draw custom meshSettings foldout
        if (meshSettingsProp != null)
        {
            showMeshSettings = EditorGUILayout.Foldout(showMeshSettings, "Mesh Settings", true);
            if (showMeshSettings)
            {
                EditorGUI.indentLevel++;

                // --- ZDE JE KLÍÈOVÁ ZMÌNA ---
                // Vykreslíme dìti (fieldy) meshSettingsProp ruènì
                SerializedProperty currentProperty = meshSettingsProp.Copy();
                // true znamená, že vstoupíme do dìtí (fieldù) meshSettingsProp
                // false znamená, že nechceme procházet další sourozenecké property, které následují po meshSettings
                bool canEnterChildren = true;
                while (currentProperty.NextVisible(canEnterChildren) && currentProperty.depth == meshSettingsProp.depth + 1)
                {
                    canEnterChildren = false; // Po prvním vstupu už nechceme automaticky vstupovat do dìtí
                    EditorGUILayout.PropertyField(currentProperty, true);
                }
                // --- KONEC KLÍÈOVÉ ZMÌNY ---

                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space(10);

        // Show terrain layers from biomeCollection
        if (planet.meshSettings != null && planet.meshSettings.terrainLayers != null)
        {
            EditorGUILayout.LabelField("Terrain Layers", EditorStyles.boldLabel);

            // Ensure correct size
            while (foldouts.Count < planet.meshSettings.terrainLayers.Count)
                foldouts.Add(false);

            for (int i = 0; i < planet.meshSettings.terrainLayers.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                // Editable object field
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