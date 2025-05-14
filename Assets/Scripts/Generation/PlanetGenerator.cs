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
    // Use the enum defined in SphereMeshGenerator for clarity
    [SerializeField] SphereMeshGenerator.SphereAlgorithm sphereAlgorithm = SphereMeshGenerator.SphereAlgorithm.Optimal;

    [Header("Sphere Mesh Settings")]
    [Range(0, 300)] public int resolution = 10;
    [Range(0.1f, 100)] public float radius = 1;
    [SerializeField] bool renderWireframe = false;

    [Header("Generation Pipeline")]
    [SerializeField] public  List<TerrainLayerSO> terrainLayers  = new List<TerrainLayerSO>();

    // --- Dependencies ---
    private MeshFilter meshFilter;
    private SphereMeshGenerator sphereMeshGenerator;
    private TerrainPipelineProcessor terrainPipelineProcessor;

    [Header("BiomStuff")]
    public bool generateBioms = true;
    public bool showBiomeSettings = true;


    [SerializeField] public BiomPipeline biomPipeline;

    // --- State Data ---
    private Mesh generatedMesh;
    private Vector3[] baseVertices;      // Raw unit sphere vertices
    private float[] processedHeights;    // Final height multipliers from GPU
    private Vector3[] processedVertices; // Final world-space vertices for mesh
    private int numVertices;
    private bool meshDataGenerated = false; // Tracks if sphere data exists
    private bool pipelineInitialized = false; // Tracks if processor is ready

  //  private Vector3[] normals;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        sphereMeshGenerator = new SphereMeshGenerator();
        terrainPipelineProcessor = new TerrainPipelineProcessor();
        if (biomPipeline == null) Debug.LogError("BiomPipeLine missing");
        biomPipeline.Initialize(GetComponent<MeshRenderer>(),GetComponent<MeshFilter>());
    }


    void Update()
    {
        if (currentlyUsedSeed != seed)
        {
            UnityEngine.Random.InitState(seed);
            currentlyUsedSeed = seed;
            // If you want runtime updates on seed change:
            // if (meshDataGenerated) GenerateTerrain();
        }
    }

    void OnValidate()
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
    }

    [ContextMenu("Generate Planet (Mesh + Terrain)")]
    public void GeneratePlanetAndTerrain()
    {
        // Generate sphere data first
        if (GenerateSphereData())
        {
            // If successful, generate terrain using that data
            GenerateTerrain();
        }
    }

    [ContextMenu("Generate Sphere Mesh Only")]
    public void GenerateSphereMesh()
    {
        if (GenerateSphereData())
        {
            // Only apply the base mesh if data generation was successful
            ApplyDataToMesh(true); // Apply base heights
        }
    }

    // Renamed from GenerateSphereMesh to avoid conflict and clarify purpose
    private bool GenerateSphereData()
    {
        // Use the dedicated generator class
        meshDataGenerated = sphereMeshGenerator.Generate(sphereAlgorithm, resolution, radius);

        if (meshDataGenerated)
        {
            // Store results locally
            baseVertices = sphereMeshGenerator.BaseVertices;
            numVertices = sphereMeshGenerator.NumVertices;




            // Initialize CPU arrays for final vertices/heights
            processedVertices = new Vector3[numVertices];
            processedHeights = new float[numVertices]; // Heights are calculated later

            biomPipeline.UpdateBiomPipeline(radius, processedHeights);

            // Initialize the terrain pipeline processor with the correct vertex count
            if (terrainPipelineProcessor == null) terrainPipelineProcessor = new TerrainPipelineProcessor();
            pipelineInitialized = terrainPipelineProcessor.Initialize(numVertices);
            if (!pipelineInitialized)
            {
                Debug.LogError("Failed to initialize terrain pipeline processor.");
                meshDataGenerated = false; // Cannot proceed without pipeline
                return false;
            }

            // Create or clear the Unity Mesh object
            if (generatedMesh == null)
            {
                generatedMesh = new Mesh { name = "Procedural Planet" };
                meshFilter.mesh = generatedMesh; // Assign only once
            }
            generatedMesh.Clear(); // Always clear before adding new data

            generatedMesh.indexFormat = numVertices > 65535 ?
               UnityEngine.Rendering.IndexFormat.UInt32 :
               UnityEngine.Rendering.IndexFormat.UInt16;

            generatedMesh.vertices = sphereMeshGenerator.BaseVertices.ToArray();
            // Set triangles (indices) - this doesn't change with terrain height
            generatedMesh.triangles = sphereMeshGenerator.Triangles;

            biomPipeline.UpdateBiomPipelineMesh(generatedMesh);

            // Vertices will be set by ApplyDataToMesh
            Debug.Log("Sphere data generated successfully.");
            return true;
        }
        else
        {
            // Generation failed, clear relevant data
            baseVertices = null;
            processedVertices = null;
            processedHeights = null;
       //     normals = null;
            numVertices = 0;
            pipelineInitialized = false;
            // Optionally clear mesh
            if (generatedMesh != null) generatedMesh.Clear();
            Debug.LogError("Sphere data generation failed.");
            return false;
        }
    }

    [ContextMenu("Generate Terrain Only")]
    public void GenerateTerrain()
    {
        // 1. Check Prerequisites
        if (!meshDataGenerated)
        {
            Debug.LogWarning("Sphere mesh data not generated yet. Generating data first.");
            if (!GenerateSphereData()) // Attempt to generate data
            {
                Debug.LogError("Cannot generate terrain because sphere data generation failed.");
                return; // Stop if data generation failed
            }
        }
        if (!pipelineInitialized)
        {
            Debug.LogError("Cannot generate terrain, pipeline processor not initialized.");
            return;
        }


        // 2. Run the Terrain Pipeline
        float[] finalHeights = terrainPipelineProcessor.ProcessTerrain(terrainLayers, baseVertices, radius, seed);

        if (finalHeights != null)
        {
            // Store the results if successful
            processedHeights = finalHeights;
            biomPipeline.UpdateBiomPipeline(radius, processedHeights);
            // 3. Apply results to the mesh
            ApplyDataToMesh(false); // Apply calculated heights
        }
        else
        {
            Debug.LogError("Terrain pipeline processing failed. Mesh not updated with terrain.");
            // Optionally revert mesh to base state
            // ApplyDataToMesh(true);
        }
    }


    // Renamed from ApplyProcessedDataToMesh for clarity
    private void ApplyDataToMesh(bool useBaseHeights)
    {
        if (!meshDataGenerated || generatedMesh == null)
        {
            Debug.LogError("Cannot apply data to mesh, prerequisites not met (mesh data or mesh object missing).");
            return;
        }

        if (useBaseHeights)
        {
            // Calculate base sphere vertices (normalized * radius)
            for (int i = 0; i < numVertices; i++)
            {
                processedVertices[i] = baseVertices[i].normalized * radius;
            }
            Debug.Log("Applying base sphere shape to mesh.");
        }
        else
        {
            if (processedHeights == null || processedHeights.Length != numVertices)
            {
                Debug.LogError("Cannot apply terrain heights, processedHeights data is invalid.");
                return;
            }
        //    normals = new Vector3[numVertices];
            // Calculate final vertices using processed heights
            for (int i = 0; i < numVertices; i++)
            {
                // Ensure base vertex is normalized before scaling by height and radius

                processedVertices[i] = baseVertices[i].normalized *(processedHeights[i]*radius+radius);//* radius;
            //    normals[i] = processedVertices[i].normalized;

                // Sanity check
                if (float.IsNaN(processedVertices[i].x) || float.IsInfinity(processedVertices[i].x))
                {
                    Debug.LogWarning($"Invalid vertex position calculated at index {i}. Clamping height.");
                    processedVertices[i] = baseVertices[i].normalized * radius; // Reset to base
                }
            }
            Debug.Log("Applying terrain heights to mesh.");
        }

        // --- Update Mesh ---
        generatedMesh.vertices = processedVertices;

      //  Vector3[] accurateNormals;

        if (renderWireframe && sphereMeshGenerator.EdgeIndices != null)
        {
            // Use stored edge indices if available and wireframe is enabled
            generatedMesh.SetIndices(sphereMeshGenerator.EdgeIndices.ToArray(), MeshTopology.Lines, 0);
            // No need to RecalculateNormals for wireframe
        }
        else
        {
            // Set triangles if not in wireframe mode (or if wireframe failed)
            // Ensure triangles are set if switching from wireframe
            if (generatedMesh.GetTopology(0) != MeshTopology.Triangles)
            {
                generatedMesh.triangles = sphereMeshGenerator.Triangles;
            }
            generatedMesh.RecalculateNormals();
            // Optional: generatedMesh.RecalculateTangents();
        }

        generatedMesh.RecalculateBounds(); // Crucial!
        Debug.Log("Mesh updated.");

        biomPipeline.UpdateBiomPipelineMesh(generatedMesh);

        if(generateBioms) biomPipeline.ApplyTexturesToMesh(baseVertices, generatedMesh.normals);
    }

    private void ReleaseResources()
    {
        // Dispose the processor to release its compute buffers
        terrainPipelineProcessor?.Dispose(); // Safely call Dispose if not null
        pipelineInitialized = false;

        // Release buffers held by Scriptable Objects
        if (terrainLayers != null)
        {
            foreach (var layer in terrainLayers)
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
        ReleaseResources();
    }

    void OnDestroy()
    {
        ReleaseResources();
        // Destroy the mesh asset if it's not needed elsewhere
        if (generatedMesh != null)
        {
            // Use DestroyImmediate in editor context if needed, otherwise Destroy
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
    }
}