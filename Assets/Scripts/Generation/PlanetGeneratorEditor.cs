using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlanetGenerator generator = (PlanetGenerator)target;

        if (generator.currentShader != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ShaderGenerator Variables", EditorStyles.boldLabel);

            SerializedObject serializedB = new SerializedObject(generator.currentShader);
            serializedB.Update();

            SerializedProperty prop = serializedB.GetIterator();
            prop.NextVisible(true); // Skip "m_Script"

            while (prop.NextVisible(false))
            {
                EditorGUILayout.PropertyField(prop, true);
            }

            serializedB.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Generate Sphere"))
        {
            generator.GeneratePlanet();
        }
        if (GUILayout.Button("Apply terrain"))
        {
            generator.RunComputeShader();
        }
    }
}
