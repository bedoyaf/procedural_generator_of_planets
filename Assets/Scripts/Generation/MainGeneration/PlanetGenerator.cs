using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(MeshFilter))]
public class PlanetGenerator : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] int seed = 0;
    private int currentlyUsedSeed = -1;

    [SerializeField] SphereMeshGenerator.SphereAlgorithm sphereAlgorithm = SphereMeshGenerator.SphereAlgorithm.Optimal;

    [Header("Sphere Mesh Settings")]

    [Header("Generation Pipeline")]
    private SphereMeshGenerator sphereMeshGenerator;
    private TerrainPipelineProcessor terrainPipelineProcessor;

    [Header("BiomStuff")]
    public bool generateBioms = true;



    // [SerializeField] public BiomPipeline biomPipeline;
    private BiomPipeline biomPipeline = new BiomPipeline();

    // --- Bioms ---

    [Header("Bioms")]
    [Header("Slopes")]
    [SerializeField, Range(0f, 90f)] private float slopeThreshold = 30f;
    [Header("Temperature Settings")]
    [Tooltip("Temperature equator")]
    [SerializeField, Range(0f, 1f)] private float equatorTemperature = 1.0f;

    [Tooltip("Temperature at the poles")]
    [SerializeField, Range(0f, 1f)] private float poleTemperature = 0.0f;

    [Tooltip("Scale of noise.")]
    [SerializeField] private float temperatureNoiseScale = 1.0f;

    [Tooltip("Strength of noise.")]
    [SerializeField] private float temperatureNoiseStrength = 0.2f;




    [Header("References")]
  //  [SerializeField] private Material material;

    [SerializeField] private BiomeCollectionSO biomeCollection;
    [SerializeField] private BiomeClassifierSO biomeClassifier;


    // --- Ocean ---
    [SerializeField] public PlanetMeshSettings planetSettings;
    [SerializeField] public PlanetMeshSettings waterSettings;

    [SerializeField] private GameObject waterGameObject;

    public PlanetData planetData = new PlanetData();
    public PlanetData waterSphereData = new PlanetData();


    void Awake()
    {
        planetData.meshFilter = GetComponent<MeshFilter>();
        waterSphereData.gameobject = waterGameObject;
        waterSphereData.meshFilter = waterGameObject.GetComponent<MeshFilter>();

        sphereMeshGenerator = new SphereMeshGenerator();
        terrainPipelineProcessor = new TerrainPipelineProcessor();
        if (biomPipeline == null) Debug.LogError("BiomPipeLine missing");
        biomPipeline.Initialize(GetComponent<MeshRenderer>(),GetComponent<MeshFilter>(),biomeClassifier,biomeCollection);
    }


    void Update()
    {
        planetSettings.material.SetVector("_PlanetCenter", transform.position);

        if (currentlyUsedSeed != seed)
        {
            UnityEngine.Random.InitState(seed);
            currentlyUsedSeed = seed;
            // If you want runtime updates on seed change:
            // if (meshDataGenerated) GenerateTerrain();
        }
    }

   /* void OnValidate()
    {
        // Clamp values
        resolution = Mathf.Max(0, resolution);
        radius = Mathf.Max(0.1f, radius);
        biomPipeline.UpdateBiomPipeline(radius, processedHeights);

        // Re-find kernels for all layers if shaders/names changed in SOs
        if (terrainLayers != null)
        {
            foreach (var layer in terrainLayers)
            {
                if (layer != null) layer.FindKernel();
            }
        }
    }*/

    [ContextMenu("Generate Planet (Mesh + Terrain)")]
    public void GeneratePlanetAndTerrain()
    {
        // Generate sphere data first
        if (GenerateSphereData(planetSettings, planetData))
        {
            GenerateTerrain(planetSettings,planetData);
        }
    }

    [ContextMenu("Generate Planet with water (Mesh + Terrain)")]
    public void GeneratePlanetAndTerrainWater()
    {
        // Generate sphere data first
        if (GenerateSphereData(planetSettings,planetData))
        {
            GenerateTerrain(planetSettings,planetData);
        }
        GenerateWaterSphere();
    }

    [ContextMenu("Generate Sphere Mesh Only")]
    public void GenerateSphereMesh()
    {
        if (GenerateSphereData(planetSettings,planetData))
        {
            ApplyDataToMesh(true,planetSettings,planetData); 
        }
    }

    public void GenerateWaterSphere()
    {
        if (GenerateSphereData(waterSettings, waterSphereData))
        {
            GenerateTerrain(waterSettings, waterSphereData);
        }
    }


    private bool GenerateSphereData(PlanetMeshSettings settings, PlanetData data)
    {
        data.meshDataGenerated = sphereMeshGenerator.Generate(sphereAlgorithm, settings.resolution);

        if (data.meshDataGenerated)
        {
            data.baseVertices = sphereMeshGenerator.BaseVertices;
            data.numVertices = sphereMeshGenerator.NumVertices;




            // Initialize CPU arrays for final vertices/heights
            data.processedVertices = new Vector3[data.numVertices];
            data.processedHeights = new float[data.numVertices]; // Heights are calculated later

            biomPipeline.UpdateBiomPipeline(settings.radius, data.processedHeights);

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

            biomPipeline.UpdateBiomPipelineMesh(data.generatedMesh);

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
    public void GenerateTerrain(PlanetMeshSettings settings,PlanetData data)
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
        float[] finalHeights = terrainPipelineProcessor.ProcessTerrain(settings.terrainLayers, data.baseVertices, seed);

        if (finalHeights != null)
        {
            // Store the results if successful
            data.processedHeights = finalHeights;
            biomPipeline.UpdateBiomPipeline(settings.radius, data.processedHeights);
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


    // Renamed from ApplyProcessedDataToMesh for clarity
    private void ApplyDataToMesh(bool useBaseHeights, PlanetMeshSettings settings, PlanetData data)
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
        Debug.Log("Mesh updated.");

        biomPipeline.UpdateBiomPipelineMesh(data.generatedMesh);

        if(generateBioms && settings.hasBioms)
        {
            biomPipeline.UpdateBiomPipelineValues(slopeThreshold,equatorTemperature,poleTemperature,temperatureNoiseScale,temperatureNoiseStrength,transform/*,delta*/);
            biomPipeline.ApplyTexturesToMesh(settings.material,data.baseVertices, data.generatedMesh.normals);
        }
    }

    private void ReleaseResources(PlanetMeshSettings settings,PlanetData data)
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
}