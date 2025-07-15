using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
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
    [SerializeField] private GameObject waterGameObject;
    private Material waterMaterial;


    private Material materialMax8;
    private Material materialDiscreteTripling;
    private Material materialContinuousTripling;

    private PlanetData planetData = new PlanetData();
    private PlanetData waterSphereData = new PlanetData();

    //paths for materials, so they dont have to be setup manualy through inspektor
    private const string MATERIALFOLDER = "Assets/Planet_Generator/Materials";
    private const string MATERIALMAX8NAME = "materialBiomeMax8";
    private const string MATERIALDISCRETETRIPLING = "materialDiscreteTripling";
    private const string MATERIALSMOOTHTRIPLINGNAME = "materialContinuousTripling";
    private const string MATERIALWATERNAME = "WATER";


    /// <summary>
    /// Sets up all the relevant properties for the generation of the planet
    /// </summary>
    private void UpdateAllRelevantProperties()
    {
        UpdateSeed();
        AssignMaterial(ref materialMax8, MATERIALMAX8NAME);
        AssignMaterial(ref materialDiscreteTripling, MATERIALDISCRETETRIPLING);
        AssignMaterial(ref materialContinuousTripling, MATERIALSMOOTHTRIPLINGNAME);
        AssignMaterial(ref waterMaterial, MATERIALWATERNAME);

        if(planetData.meshRenderer==null) planetData.meshRenderer = GetComponent<MeshRenderer>();
        if(planetData.meshFilter==null) planetData.meshFilter = GetComponent<MeshFilter>();
    }

    private void UpdateSeed()
    {
        if (currentlyUsedSeed != planetSO.seed)
        {
            UnityEngine.Random.InitState(planetSO.seed);
            currentlyUsedSeed = planetSO.seed;
        }
    }

    /// <summary>
    /// The main function for generating the planet, accessible through a button
    /// </summary>
    [ContextMenu("Generate Planet")]
    public void GeneratePlanet()
    {
        UpdateAllRelevantProperties();

        if (biomePipeline.RegeratedMesh) ResetMesh();

        if (GenerateSphereData(planetSO.meshSettings, planetData))
        {
            GenerateTerrain(planetSO.meshSettings,planetData);
        }
        if (planetSO.hasWater) GenerateWaterSphere();
        else DestroyImmediate(waterGameObject);
        BakeAndSave();
    }

    /// <summary>
    /// Sets up and generates the WaterSphere, similar to planet generation
    /// </summary>
    private void GenerateWaterSphere()
    {
        planetSO.waterSettings.isWaterSphere = true;
        if (waterGameObject == gameObject) waterGameObject = null;
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

        if (GenerateSphereData(planetSO.waterSettings, waterSphereData))
        {
            GenerateTerrain(planetSO.waterSettings, waterSphereData);
        }
    }

    /// <summary>
    /// Generates the base mesh, and sets up the rest
    /// </summary>
    /// <param name="settings">users settings for the mesh</param>
    /// <param name="data">the stored data for that mesh</param>
    /// <returns>generation success</returns>
    private bool GenerateSphereData(SphereMeshSettings settings, PlanetData data)
    {
        data.meshDataGenerated = sphereMeshGenerator.Generate( settings.resolution);

        if (data.meshDataGenerated)
        {
            data.baseVertices = sphereMeshGenerator.BaseVertices;
            data.numVertices = sphereMeshGenerator.NumVertices;

            data.processedVertices = new Vector3[data.numVertices];
            data.processedHeights = new float[data.numVertices]; 

            biomePipeline.UpdateBiomPipeline( data.processedHeights);

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
                data.generatedMesh = new Mesh { name = "Procedural Planet" };
                data.meshFilter.sharedMesh = data.generatedMesh; 
            }
            data.generatedMesh.Clear(); 

            //standart Unity meshes have limited indices
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

    /// <summary>
    /// Generates the new vertex heights by calling the terrainPipeline
    /// </summary>
    /// <param name="settings">users settings for the mesh</param>
    /// <param name="data">the stored data for that mesh</param>
    public void GenerateTerrain(SphereMeshSettings settings,PlanetData data)
    {
        if (!data.meshDataGenerated)
        {
            Debug.LogWarning("Sphere mesh data not generated yet. Generating data first.");
            if (!GenerateSphereData(settings,data)) 
            {
                Debug.LogError("Cannot generate terrain because sphere data generation failed.");
                return;
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

            ApplyDataToMesh( settings, data); 
        }
        else
        {
            Debug.LogError("Terrain pipeline processing failed. Mesh not updated with terrain.");
            return;
        }
    }

    private void ResetMesh()
    {
        if(planetData.meshFilter!=null)planetData.meshFilter.sharedMesh = null;

        planetData.generatedMesh=null;
        planetData.baseVertices=null;     
        planetData.processedHeights=null;   
        planetData.processedVertices=null;
        planetData.numVertices = 0;
        planetData.meshDataGenerated = false;
        planetData.pipelineInitialized = false;
    }

    /// <summary>
    /// Calculates the new data for the mesh based on the settings and data, including calling the setup of biomes or water
    /// </summary>
    /// <param name="settings">users settings for the mesh</param>
    /// <param name="data">the stored data for that mesh</param>
    private void ApplyDataToMesh( SphereMeshSettings settings, PlanetData data)
    {
        if (!data.meshDataGenerated || data.generatedMesh == null)
        {
            Debug.LogError("Cannot apply data to mesh, prerequisites not met (mesh data or mesh object missing).");
            return;
        }

        if (data.processedHeights == null || data.processedHeights.Length != data.numVertices)
        {
            Debug.LogError("Cannot apply terrain heights, processedHeights data is invalid.");
            return;
        }
        for (int i = 0; i < data.numVertices; i++)
        {                
            data.processedVertices[i] = data.baseVertices[i].normalized *(data.processedHeights[i]*settings.radius+settings.radius);

            if (float.IsNaN(data.processedVertices[i].x) || float.IsInfinity(data.processedVertices[i].x))
            {
                Debug.LogWarning($"Invalid vertex position calculated at index {i}. Clamping height.");
                data.processedVertices[i] = data.baseVertices[i].normalized * settings.radius; 
            }
        }
        //update mesh
        data.generatedMesh.vertices = data.processedVertices;
        if (data.generatedMesh.GetTopology(0) != MeshTopology.Triangles)
        {
            data.generatedMesh.triangles = sphereMeshGenerator.Triangles;
        }
        data.generatedMesh.RecalculateNormals();

        data.generatedMesh.RecalculateBounds(); 
        data.meshFilter.sharedMesh = data.generatedMesh;
        Debug.Log("Mesh updated.");

        if( !settings.isWaterSphere)
        {
            biomePipeline.Initialize(data.meshRenderer, data.meshFilter, planetSO.biomeClassifier, planetSO.biomeCollection);

            biomePipeline.UpdateMaterials(materialMax8,materialDiscreteTripling,materialContinuousTripling, null);
            biomePipeline.UpdateBiomPipelineValues(planetSO.temperatureNoiseScale,planetSO.temperatureNoiseStrength, planetSO.TextureScale);
            biomePipeline.ApplyTexturesToMesh(data.baseVertices, data.generatedMesh.normals, data.generatedMesh.triangles, planetSO.biomeBlendType);
        }
        else if(settings.isWaterSphere)
        {
            data.meshRenderer.sharedMaterial = waterMaterial;
            waterMaterial.SetFloat("_heightStart", planetSO.waterIceLineStart);
            waterMaterial.SetFloat("_heightEnd", planetSO.waterIceLineEnd);
            waterMaterial.SetColor("_Wat_Color", planetSO.waterColor);
            waterMaterial.SetFloat("_WaterAlpha", planetSO.waterColor.a / 255f);
            waterMaterial.SetColor("_IceColor", planetSO.IceColor);
            waterMaterial.SetFloat("_IceAlpha", planetSO.IceColor.a/255f);
            waterMaterial.SetFloat("_radius", planetSO.waterSettings.radius);
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

    /// <summary>
    /// Just makes sure everything is disabled safely
    /// </summary>
    void OnDisable()
    {
        ReleaseResources(planetSO.meshSettings, planetData);
        ReleaseResources(planetSO.waterSettings, waterSphereData);
    }

    /// <summary>
    /// Just makes sure everything is destroyed safely
    /// </summary>
    void OnDestroy()
    {
        ReleaseResources(planetSO.meshSettings, planetData);
        ReleaseResources(planetSO.waterSettings, waterSphereData);
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

    /// <summary>
    /// Resets all values, for emergancy purposes for user, accessible through a button
    /// </summary>
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



    [ContextMenu("Bake Biome Texture Array And Save Material")]
    void BakeAndSave()
    {
#if UNITY_EDITOR
        if (planetSO == null || planetSO.biomeCollection == null)
        {
            Debug.LogWarning("Missing PlanetSO or BiomeCollection. Cannot bake.", this);
            return;
        }

        // 1) Vygeneruj Text2DArray z biome kolekce
        var texArr = BiomeUtils.GenerateBiomeTextureArray(planetSO.biomeCollection);
        if (texArr == null)
        {
            Debug.LogError("Texture2DArray generation failed.", this);
            return;
        }

        // 2) Zajisti složku pro uložení assetù
        string folder = "Assets/Planet_Generator/Baked";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Planet_Generator", "Baked");
        }

        // 3) Cesty k assetùm
        string baseName = name.Replace(" ", "_");
        string texturePath = Path.Combine(folder, $"{baseName}_biomes.asset");
        string materialPath = Path.Combine(folder, $"{baseName}_material.mat");

        // 4) Ulož nebo aktualizuj Texture2DArray asset
        texArr.name = Path.GetFileNameWithoutExtension(texturePath);
        var existingTexArr = AssetDatabase.LoadAssetAtPath<Texture2DArray>(texturePath);
        if (existingTexArr == null)
        {
            AssetDatabase.CreateAsset(texArr, texturePath);
            AssetDatabase.ImportAsset(texturePath);
        }
        else
        {
            EditorUtility.CopySerialized(texArr, existingTexArr);
            texArr = existingTexArr;
        }
        EditorUtility.SetDirty(texArr);

        // 5) Vytvoø instanci materiálu a pøiøaï texturové pole
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError("Missing MeshRenderer on this GameObject.", this);
            return;
        }

        Material newMat = new Material(renderer.sharedMaterial);
        newMat.name = $"{baseName}_Material";
        newMat.SetTexture("_Biomes", texArr);

        // 6) Ulož materiál jako asset
        AssetDatabase.CreateAsset(newMat, materialPath);
        AssetDatabase.ImportAsset(materialPath);
        EditorUtility.SetDirty(newMat);

        // 7) Aplikuj nový materiál na renderer
        renderer.sharedMaterial = newMat;

        // 8) Oznaè scénu jako modifikovanou a ulož
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log($"Baked Texture2DArray saved to '{texturePath}', material saved to '{materialPath}', and applied to '{renderer.gameObject.name}'", this);
#endif
    }

    private void AssignMaterial(ref Material field, string name)
    {
#if UNITY_EDITOR
        if (field != null) return;

        var guids = AssetDatabase.FindAssets($"t:Material {name}", new[] { MATERIALFOLDER });
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            field = AssetDatabase.LoadAssetAtPath<Material>(path);
            EditorUtility.SetDirty(this);
            if(field==null)
            {
                Debug.LogError($"[PlanetGenerator] Material '{name}' got loaded as null", this);
            }

        }
        else
        {
            Debug.LogWarning($"[PlanetGenerator] Material '{name}' not found in project.", this);
        }
#endif
    }

}