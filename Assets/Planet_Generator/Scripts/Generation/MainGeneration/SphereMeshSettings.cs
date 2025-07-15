using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scriptable object that allows the customization and setup of the spheres values and terrain algorithms
/// </summary>
[System.Serializable]
public class SphereMeshSettings
{
    [Header("Sphere Mesh Settings")]
    [Range(0, 300)] public int resolution = 100;
    [Range(0.1f, 100)] public float radius = 1;

    [Header("Generation Pipeline")]
    [SerializeField] public List<TerrainLayerSO> terrainLayers = new List<TerrainLayerSO>();

    [HideInInspector] public bool isWaterSphere = false;

}