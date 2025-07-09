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
            // P�esko��me meshSettings, abychom ho vykreslili ru�n�
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

                // --- ZDE JE KL��OV� ZM�NA ---
                // Vykresl�me d�ti (fieldy) meshSettingsProp ru�n�
                SerializedProperty currentProperty = meshSettingsProp.Copy();
                // true znamen�, �e vstoup�me do d�t� (field�) meshSettingsProp
                // false znamen�, �e nechceme proch�zet dal�� sourozeneck� property, kter� n�sleduj� po meshSettings
                bool canEnterChildren = true;
                while (currentProperty.NextVisible(canEnterChildren) && currentProperty.depth == meshSettingsProp.depth + 1)
                {
                    canEnterChildren = false; // Po prvn�m vstupu u� nechceme automaticky vstupovat do d�t�
                    EditorGUILayout.PropertyField(currentProperty, true);
                }
                // --- KONEC KL��OV� ZM�NY ---

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