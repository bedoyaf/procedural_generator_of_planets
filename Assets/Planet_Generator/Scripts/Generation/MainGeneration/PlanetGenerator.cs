using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;

[RequireComponent(typeof(MeshFilter))]
public class PlanetGenerator : MonoBehaviour
{
    [Header("Setup")]
    public PlanetSO planetSO;
    private int currentlyUsedSeed = -1;

    [Header("Sphere Mesh Settings")]

    [Header("Generation Pipeline")]
    private SphereMeshGenerator sphereMeshGenerator = new SphereMeshGenerator();

    [Header("BiomStuff")]
    private BiomePipeline biomePipeline = new BiomePipeline();

    [Header("References")]
  
    // --- Ocean ---
    [SerializeField] public SphereMeshSettings waterSettings;

    [SerializeField] private GameObject waterGameObject;
    [SerializeField] private Material waterMaterial;


    [SerializeField] private Material materialDiscreteMax8;
    [SerializeField] private Material materialDiscreteTripling;
    [SerializeField] private Material materialSmoothMax8;
    [SerializeField] private Material materialSmoothTripling;

 //   [SerializeField, Range(0,1)] private float waterIceLineStart = 0.82f;
 //   [SerializeField, Range(0, 1)] private float waterIceLineEnd = 0.87f;

    public PlanetData planetData = new PlanetData();
    public PlanetData waterSphereData = new PlanetData();

    void Update()
    {
       UpdateSeed();
    }

    private void UpdateSeed()
    {
        if (currentlyUsedSeed != planetSO.seed)
        {
            UnityEngine.Random.InitState(planetSO.seed);
            currentlyUsedSeed = planetSO.seed;
        }
    }

    [ContextMenu("Generate Planet (Mesh + Terrain)")]
    public void GeneratePlanetAndTerrain()
    {
        UpdateSeed();
        planetData.meshFilter = GetComponent<MeshFilter>();
        if (biomePipeline.RegeratedMesh) ResetMesh();
        // Generate sphere data first
        if (GenerateSphereData(planetSO.meshSettings, planetData))
        {
            GenerateTerrain(planetSO.meshSettings,planetData);
        }
    }

    [ContextMenu("Generate Planet with water (Mesh + Terrain)")]
    public void GeneratePlanetAndTerrainWater()
    {
        UpdateSeed();
        planetData.meshFilter = GetComponent<MeshFilter>();
        DateTime before = DateTime.Now;

        if (biomePipeline.RegeratedMesh) ResetMesh();
        // Generate sphere data first
        if (GenerateSphereData(planetSO.meshSettings,planetData))
        {
            GenerateTerrain(planetSO.meshSettings,planetData);
        }
        GenerateWaterSphere();

        DateTime after = DateTime.Now;
        TimeSpan duration = after.Subtract(before);
        Debug.Log("Duration in milliseconds: " + duration.Milliseconds);
    }

    [ContextMenu("Generate Sphere Mesh Only")]
    public void GenerateSphereMesh()
    {
        planetData.meshFilter = GetComponent<MeshFilter>();

        if (biomePipeline.RegeratedMesh) ResetMesh();
        if (GenerateSphereData(planetSO.meshSettings,planetData))
        {
            ApplyDataToMesh(true,planetSO.meshSettings,planetData); 
        }
    }

    public void GenerateWaterSphere()
    {
        waterSettings.isWaterSphere = true;
        if (waterGameObject == null)
        {
            waterGameObject = new GameObject("WaterSphere");

            waterGameObject.transform.SetParent(transform);
            waterGameObject.transform.localPosition = Vector3.zero;
            waterGameObject.transform.localRotation = Quaternion.identity;
            waterGameObject.transform.localScale = Vector3.one;
        }
        if(waterGameObject.GetComponent<MeshFilter>()==null)
        {
            waterGameObject.AddComponent<MeshFilter>();
        }
        waterSphereData.meshFilter = waterGameObject.GetComponent<MeshFilter>();
        if (waterGameObject.GetComponent<MeshRenderer>()==null)
        {
            waterGameObject.AddComponent<MeshRenderer>();
        }
        waterSphereData.meshRenderer = waterGameObject.GetComponent<MeshRenderer>();
        waterSphereData.gameobject = waterGameObject;
        waterSphereData.meshFilter = waterGameObject.GetComponent<MeshFilter>();

        if (GenerateSphereData(waterSettings, waterSphereData))
        {
            GenerateTerrain(waterSettings, waterSphereData);
        }
    }


    private bool GenerateSphereData(SphereMeshSettings settings, PlanetData data)
    {
       // if(sphereMeshGenerator==null)
        data.meshDataGenerated = sphereMeshGenerator.Generate( settings.resolution);

        if (data.meshDataGenerated)
        {
            data.baseVertices = sphereMeshGenerator.BaseVertices;
            data.numVertices = sphereMeshGenerator.NumVertices;




            // Initialize CPU arrays for final vertices/heights
            data.processedVertices = new Vector3[data.numVertices];
            data.processedHeights = new float[data.numVertices]; // Heights are calculated later

            biomePipeline.UpdateBiomPipeline( data.processedHeights);

            // Initialize the terrain pipeline processor with the correct vertex count
            if (data.terrainPipelineProcessor == null) data.terrainPipelineProcessor = new TerrainPipelineProcessor();
            data.pipelineInitialized = data.terrainPipelineProcessor.Initialize(data.numVertices);
            if (!data.pipelineInitialized)
            {
                Debug.LogError("Failed to initialize terrain pipeline processor.");
                planetData.meshDataGenerated = false; 
                return false;
            }

            if (data.generatedMesh == null)
            {
          //      Debug.Log("skibidi radius " + settings.radius);
                data.generatedMesh = new Mesh { name = "Procedural Planet" };
                data.meshFilter.sharedMesh = data.generatedMesh; 
            }
            data.generatedMesh.Clear(); 

            data.generatedMesh.indexFormat = data.numVertices > 65535 ?
               UnityEngine.Rendering.IndexFormat.UInt32 :
               UnityEngine.Rendering.IndexFormat.UInt16;

            data.generatedMesh.vertices = sphereMeshGenerator.BaseVertices.ToArray();

            data.generatedMesh.triangles = sphereMeshGenerator.Triangles;

            Debug.Log("Sphere data generated successfully.");
            return true;
        }
        else
        {
            data.baseVertices = null;
            data.processedVertices = null;
            data.processedHeights = null;
            data.numVertices = 0;
            data.pipelineInitialized = false;
            if (data.generatedMesh != null) data.generatedMesh.Clear();
            Debug.LogError("Sphere data generation failed.");
            return false;
        }
    }

    public void GenerateTerrain(SphereMeshSettings settings,PlanetData data)
    {
        // 1. Check Prerequisites
        if (!data.meshDataGenerated)
        {
            Debug.LogWarning("Sphere mesh data not generated yet. Generating data first.");
            if (!GenerateSphereData(settings,data)) // Attempt to generate data
            {
                Debug.LogError("Cannot generate terrain because sphere data generation failed.");
                return; // Stop if data generation failed
            }
        }
        if (!data.pipelineInitialized)
        {
            Debug.LogError("Cannot generate terrain, pipeline processor not initialized.");
            return;
        }

        float[] finalHeights = data.terrainPipelineProcessor.ProcessTerrain(settings.terrainLayers, data.baseVertices, planetSO.seed);

        if (finalHeights != null)
        {
            data.processedHeights = finalHeights;
            biomePipeline.UpdateBiomPipeline(data.processedHeights);

            ApplyDataToMesh(false, settings, data); 
        }
        else
        {
            Debug.LogError("Terrain pipeline processing failed. Mesh not updated with terrain.");
        }
    }

    private void ResetMesh()
    {
         if(planetData.meshFilter!=null)planetData.meshFilter.sharedMesh = null;

 //       MeshFilter meshFilter;

        planetData.generatedMesh=null;
        planetData.baseVertices=null;      // Raw unit sphere vertices
        planetData.processedHeights=null;    // Final height multipliers from GPU
        planetData.processedVertices=null; // Final world-space vertices for mesh
        planetData.numVertices = 0;
        planetData.meshDataGenerated = false; // Tracks if sphere data exists
        planetData.pipelineInitialized = false; // Tracks if processor is ready
    }

    private void ApplyDataToMesh(bool useBaseHeights, SphereMeshSettings settings, PlanetData data)
    {
        if (!data.meshDataGenerated || data.generatedMesh == null)
        {
            Debug.LogError("Cannot apply data to mesh, prerequisites not met (mesh data or mesh object missing).");
            return;
        }

        if (useBaseHeights)
        {
            for (int i = 0; i < data.numVertices; i++)
            {
                data.processedVertices[i] = data.baseVertices[i].normalized * settings.radius;
            }
            Debug.Log("Applying base sphere shape to mesh.");
        }
        else
        {
            if (data.processedHeights == null || data.processedHeights.Length != data.numVertices)
            {
                Debug.LogError("Cannot apply terrain heights, processedHeights data is invalid.");
                return;
            }
            for (int i = 0; i < data.numVertices; i++)
            {                
                data.processedVertices[i] = data.baseVertices[i].normalized *(data.processedHeights[i]*settings.radius+settings.radius);//* radius;

                if (float.IsNaN(data.processedVertices[i].x) || float.IsInfinity(data.processedVertices[i].x))
                {
                    Debug.LogWarning($"Invalid vertex position calculated at index {i}. Clamping height.");
                    data.processedVertices[i] = data.baseVertices[i].normalized * settings.radius; // Reset to base
                }
            }
            Debug.Log("Applying terrain heights to mesh.");
        }

        // --- Update Mesh ---
        data.generatedMesh.vertices = data.processedVertices;

            // Set triangles if not in wireframe mode (or if wireframe failed)
            // Ensure triangles are set if switching from wireframe
            if (data.generatedMesh.GetTopology(0) != MeshTopology.Triangles)
            {
                data.generatedMesh.triangles = sphereMeshGenerator.Triangles;
            }
            data.generatedMesh.RecalculateNormals();
            // Optional: generatedMesh.RecalculateTangents();
        

        data.generatedMesh.RecalculateBounds(); // Crucial!
        data.meshFilter.sharedMesh = data.generatedMesh;
        Debug.Log("Mesh updated.");

        if(planetSO.generateBioms && !settings.isWaterSphere)
        {
            biomePipeline.Initialize(GetComponent<MeshRenderer>(), GetComponent<MeshFilter>(), planetSO.biomeClassifier, planetSO.biomeCollection);

            biomePipeline.UpdateMaterials(materialDiscreteMax8,materialDiscreteTripling,materialSmoothMax8,materialSmoothTripling);
            biomePipeline.UpdateBiomPipelineValues(planetSO.equatorTemperature,planetSO.poleTemperature,planetSO.temperatureNoiseScale,planetSO.temperatureNoiseStrength, planetSO.TextureScale);
            biomePipeline.ApplyTexturesToMesh(data.baseVertices, data.generatedMesh.normals, data.generatedMesh.triangles, planetSO.biomBlendType);
        }
        else if(settings.isWaterSphere)
        {
            data.meshRenderer.sharedMaterial = waterMaterial;
        //    waterMaterial.SetFloat("heightStart",waterIceLineStart);
         //   waterMaterial.SetFloat("heightEnd",waterIceLineEnd);
        }
    }

    private void ReleaseResources(SphereMeshSettings settings,PlanetData data)
    {
        data.terrainPipelineProcessor?.Dispose();
        data.pipelineInitialized = false;

        if (settings.terrainLayers != null)
        {
            foreach (var layer in settings.terrainLayers)
            {
                if (layer is CraterLayerSO craterLayer) 
                {
                    craterLayer.ReleaseBuffers(); 
                }
            }
        }
        Debug.Log("Compute resources released by PlanetGenerator.");
    }

    void OnDisable()
    {
        ReleaseResources(planetSO.meshSettings, planetData);
        ReleaseResources(waterSettings, waterSphereData);
    }

    void OnDestroy()
    {
        ReleaseResources(planetSO.meshSettings, planetData);
        ReleaseResources(waterSettings, waterSphereData);
        // Destroy the mesh asset if it's not needed elsewhere
        if (planetData.generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(planetData.generatedMesh);
            else DestroyImmediate(planetData.generatedMesh);
        }
        if (waterSphereData.generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(waterSphereData.generatedMesh);
            else DestroyImmediate(waterSphereData.generatedMesh);
        }
    }

    [ContextMenu("Reset All")]
    public void ResetAll()
    {
        ResetMesh();
        planetData = new PlanetData();

        if (waterGameObject != null)
        {
            DestroyImmediate(waterGameObject);
            waterGameObject = null;
        }
        waterSphereData = new PlanetData();

        biomePipeline = new BiomePipeline();

        currentlyUsedSeed = -1;

        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mf.sharedMesh.Clear();
        }

        Debug.Log("PlanetGenerator has been fully reset.");
    }
}