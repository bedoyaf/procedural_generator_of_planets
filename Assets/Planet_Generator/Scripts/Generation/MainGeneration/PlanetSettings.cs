using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetMeshSettings
{
    //   public string name = "Planet";
    //   public bool generateCollider = false;
//    public Material material;
    [Header("Sphere Mesh Settings")]
    [Range(0, 300)] public int resolution = 10;
    [Range(0.1f, 100)] public float radius = 1;

    [Header("Generation Pipeline")]
    [SerializeField] public List<TerrainLayerSO> terrainLayers = new List<TerrainLayerSO>();

    [HideInInspector] public bool isWaterSphere = false;
}

public class PlanetData
{
    public GameObject gameobject;
    // --- Dependencies ---
    public MeshFilter meshFilter;

    public Mesh generatedMesh;
    public MeshRenderer meshRenderer;
    public Vector3[] baseVertices;      // Raw unit sphere vertices
    public float[] processedHeights;    // Final height multipliers from GPU
    public Vector3[] processedVertices; // Final world-space vertices for mesh
    public int numVertices;
    public bool meshDataGenerated = false; // Tracks if sphere data exists
    public bool pipelineInitialized = false; // Tracks if processor is ready
}