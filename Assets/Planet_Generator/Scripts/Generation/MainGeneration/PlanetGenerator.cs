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
//    [SerializeField] int seed = 0;
    private int currentlyUsedSeed = -1;

    [SerializeField] SphereMeshGenerator.SphereAlgorithm sphereAlgorithm = SphereMeshGenerator.SphereAlgorithm.Optimal;
   // [SerializeField] BiomeBlendType biomBlendType = BiomeBlendType.Discrete;

    [Header("Sphere Mesh Settings")]

    [Header("Generation Pipeline")]
    private SphereMeshGenerator sphereMeshGenerator = new SphereMeshGenerator();
    private TerrainPipelineProcessor terrainPipelineProcessor = new TerrainPipelineProcessor();

    [Header("BiomStuff")]
  //  public bool generateBioms = true;

    // [SerializeField] public BiomPipeline biomPipeline;
    private BiomePipeline biomePipeline = new BiomePipeline();

    // --- Bioms ---

  /*  [Header("Bioms")]
    [Header("Temperature Settings")]
    [Tooltip("Temperature equator")]
    [SerializeField, Range(0f, 1f)] private float equatorTemperature = 1.0f;

    [Tooltip("Temperature at the poles")]
    [SerializeField, Range(0f, 1f)] private float poleTemperature = 0.0f;

    [Tooltip("Scale of noise.")]
    [SerializeField] private float temperatureNoiseScale = 1.0f;

    [Tooltip("Strength of noise.")]
    [SerializeField] private float temperatureNoiseStrength = 0.2f;*/




    [Header("References")]
  /*  [SerializeField] private BiomeCollectionSO biomeCollection;
    [SerializeField] private BiomeClassifierSO biomeClassifier;*/


    // --- Ocean ---
   // private PlanetMeshSettings planetSettings=null;
    [SerializeField] public SphereMeshSettings waterSettings;

    [SerializeField] private GameObject waterGameObject;
    [SerializeField] private Material waterMaterial;


    [SerializeField] private Material materialDiscreteMax8;
    [SerializeField] private Material materialDiscreteTripling;
    [SerializeField] private Material materialSmoothMax8;
    [SerializeField] private Material materialSmoothTripling;


    public PlanetData planetData = new PlanetData();
    public PlanetData waterSphereData = new PlanetData();


    void Awake()
    {


       // biomePipeline.Initialize(GetComponent<MeshRenderer>(),GetComponent<MeshFilter>(),planetSO.biomeClassifier,planetSO.biomeCollection);
        

        
    }


    void Update()
    {
     //   planetSettings.material.SetVector("_PlanetCenter", transform.position);

        if (currentlyUsedSeed != planetSO.seed)
        {
            UnityEngine.Random.InitState(planetSO.seed);
            currentlyUsedSeed = planetSO.seed;
            // If you want runtime updates on seed change:
            // if (meshDataGenerated) GenerateTerrain();
        }
    }

    [ContextMenu("Generate Planet (Mesh + Terrain)")]
    public void GeneratePlanetAndTerrain()
    {
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
        waterSphereData.gameobject = waterGameObject;
        waterSphereData.meshFilter = waterGameObject.GetComponent<MeshFilter>();

        if (waterGameObject == null)
        {
            // Create new GameObject
            waterGameObject = new GameObject("WaterSphere");

            // Set as child of current GameObject
            waterGameObject.transform.SetParent(transform);
            waterGameObject.transform.localPosition = Vector3.zero;
            waterGameObject.transform.localRotation = Quaternion.identity;
            waterGameObject.transform.localScale = Vector3.one;

            // Add required components
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
        if (GenerateSphereData(waterSettings, waterSphereData))
        {
            GenerateTerrain(waterSettings, waterSphereData);
        }
    }


    private bool GenerateSphereData(SphereMeshSettings settings, PlanetData data)
    {
       // if(sphereMeshGenerator==null)
        data.meshDataGenerated = sphereMeshGenerator.Generate(sphereAlgorithm, settings.resolution);

        if (data.meshDataGenerated)
        {
            data.baseVertices = sphereMeshGenerator.BaseVertices;
            data.numVertices = sphereMeshGenerator.NumVertices;




            // Initialize CPU arrays for final vertices/heights
            data.processedVertices = new Vector3[data.numVertices];
            data.processedHeights = new float[data.numVertices]; // Heights are calculated later

            biomePipeline.UpdateBiomPipeline( data.processedHeights);

            // Initialize the terrain pipeline processor with the correct vertex count
            if (terrainPipelineProcessor == null) terrainPipelineProcessor = new TerrainPipelineProcessor();
            data.pipelineInitialized = terrainPipelineProcessor.Initialize(data.numVertices);
            if (!data.pipelineInitialized)
            {
                Debug.LogError("Failed to initialize terrain pipeline processor.");
                planetData.meshDataGenerated = false; // Cannot proceed without pipeline
                return false;
            }

            // Create or clear the Unity Mesh object
            if (data.generatedMesh == null)
            {
          //      Debug.Log("skibidi radius " + settings.radius);
                data.generatedMesh = new Mesh { name = "Procedural Planet" };
                data.meshFilter.mesh = data.generatedMesh; // Assign only once
            }
            data.generatedMesh.Clear(); // Always clear before adding new data

            data.generatedMesh.indexFormat = data.numVertices > 65535 ?
               UnityEngine.Rendering.IndexFormat.UInt32 :
               UnityEngine.Rendering.IndexFormat.UInt16;

            data.generatedMesh.vertices = sphereMeshGenerator.BaseVertices.ToArray();
            // Set triangles (indices) - this doesn't change with terrain height
            data.generatedMesh.triangles = sphereMeshGenerator.Triangles;

            // Vertices will be set by ApplyDataToMesh
            Debug.Log("Sphere data generated successfully.");
            return true;
        }
        else
        {
            // Generation failed, clear relevant data
            data.baseVertices = null;
            data.processedVertices = null;
            data.processedHeights = null;
       //     normals = null;
            data.numVertices = 0;
            data.pipelineInitialized = false;
            // Optionally clear mesh
            if (data.generatedMesh != null) data.generatedMesh.Clear();
            Debug.LogError("Sphere data generation failed.");
            return false;
        }
    }

    [ContextMenu("Generate Terrain Only")]
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


        // 2. Run the Terrain Pipeline
        float[] finalHeights = terrainPipelineProcessor.ProcessTerrain(settings.terrainLayers, data.baseVertices, planetSO.seed);

        if (finalHeights != null)
        {
            // Store the results if successful
            data.processedHeights = finalHeights;
            biomePipeline.UpdateBiomPipeline(data.processedHeights);
            // 3. Apply results to the mesh
            ApplyDataToMesh(false, settings, data); // Apply calculated heights
        }
        else
        {
            Debug.LogError("Terrain pipeline processing failed. Mesh not updated with terrain.");
            // Optionally revert mesh to base state
            // ApplyDataToMesh(true);
        }
    }

    private void ResetMesh()
    {
        planetData.meshFilter.mesh = null;

 //       MeshFilter meshFilter;

        planetData.generatedMesh=null;
        planetData.baseVertices=null;      // Raw unit sphere vertices
        planetData.processedHeights=null;    // Final height multipliers from GPU
        planetData.processedVertices=null; // Final world-space vertices for mesh
        planetData.numVertices = 0;
        planetData.meshDataGenerated = false; // Tracks if sphere data exists
        planetData.pipelineInitialized = false; // Tracks if processor is ready
    }


    // Renamed from ApplyProcessedDataToMesh for clarity
    private void ApplyDataToMesh(bool useBaseHeights, SphereMeshSettings settings, PlanetData data)
    {
        if (!data.meshDataGenerated || data.generatedMesh == null)
        {
            Debug.LogError("Cannot apply data to mesh, prerequisites not met (mesh data or mesh object missing).");
            return;
        }

        if (useBaseHeights)
        {
            // Calculate base sphere vertices (normalized * radius)
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
        //    normals = new Vector3[numVertices];
            // Calculate final vertices using processed heights
            for (int i = 0; i < data.numVertices; i++)
            {
                // Ensure base vertex is normalized before scaling by height and radius
                
                data.processedVertices[i] = data.baseVertices[i].normalized *(data.processedHeights[i]*settings.radius+settings.radius);//* radius;
            //    normals[i] = processedVertices[i].normalized;

                // Sanity check
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
        data.meshFilter.mesh = data.generatedMesh;
        Debug.Log("Mesh updated.");

        if(planetSO.generateBioms && !settings.isWaterSphere)
        {
            biomePipeline.Initialize(GetComponent<MeshRenderer>(), GetComponent<MeshFilter>(), planetSO.biomeClassifier, planetSO.biomeCollection);

            biomePipeline.UpdateMaterials(materialDiscreteMax8,materialDiscreteTripling,materialSmoothMax8,materialSmoothTripling);
            biomePipeline.UpdateBiomPipelineValues(planetSO.equatorTemperature,planetSO.poleTemperature,planetSO.temperatureNoiseScale,planetSO.temperatureNoiseStrength/*,delta*/);
            biomePipeline.ApplyTexturesToMesh(data.baseVertices, data.generatedMesh.normals, data.generatedMesh.triangles, planetSO.biomBlendType);
        }
        else if(settings.isWaterSphere)
        {
            data.meshRenderer.sharedMaterial = waterMaterial;
        }
    }

    private void ReleaseResources(SphereMeshSettings settings,PlanetData data)
    {
        // Dispose the processor to release its compute buffers
        terrainPipelineProcessor?.Dispose(); // Safely call Dispose if not null
        data.pipelineInitialized = false;

        // Release buffers held by Scriptable Objects
        if (settings.terrainLayers != null)
        {
            foreach (var layer in settings.terrainLayers)
            {
                if (layer is CraterLayerSO craterLayer) // Example check
                {
                    craterLayer.ReleaseBuffers(); // Call the specific release method
                }
                // Add checks for other layer types managing buffers
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
            // Use DestroyImmediate in editor context if needed, otherwise Destroy
            if (Application.isPlaying) Destroy(planetData.generatedMesh);
            else DestroyImmediate(planetData.generatedMesh);
        }
        if (waterSphereData.generatedMesh != null)
        {
            // Use DestroyImmediate in editor context if needed, otherwise Destroy
            if (Application.isPlaying) Destroy(waterSphereData.generatedMesh);
            else DestroyImmediate(waterSphereData.generatedMesh);
        }
    }
}