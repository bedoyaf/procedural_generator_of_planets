using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;
using System.Linq;
using UnityEngine.UIElements;
using System.Drawing;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using Unity.Collections;
using NUnit.Framework;
using Unity.Burst;
using Unity.Jobs;
using NUnit.Framework.Internal;
using Unity.Profiling;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.SceneManagement;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class BiomePipeline
{         
    private const float EQUATORTEMPERATURE = 1.0f;
    private const float POLETEMPERATURE = 0.0f;
    private float temperatureNoiseScale = 1.0f;
    private float temperatureNoiseStrength = 0.2f;
    private float TextureScale = 1;

    private Material materialDiscreteMax8;
    private Material materialDiscreteTripling;
    private Material materialContinuousTripling;

    private Material splatingMaterial;


    private BiomeCollectionSO biomeCollection;
    private BiomeClassifierSO biomeClassifier;

    int[] triangles;

    private float[] heights;

    private MeshFilter meshFilter;

    private MeshRenderer meshRenderer;

    public bool RegeratedMesh { get; private set; } = false;



 
    /// <summary>
    /// Initializes the biompipeline
    /// </summary>
    public void Initialize(MeshRenderer meshRenderer, MeshFilter meshFilter, BiomeClassifierSO biomeClassifier, BiomeCollectionSO biomeCollection)
    {
        this.meshRenderer = meshRenderer;
        this.meshFilter = meshFilter;
        this.biomeClassifier = biomeClassifier;
        this.biomeCollection = biomeCollection;
    }


    public void UpdateBiomPipeline( float[] heights)
    {
        this.heights = heights;
    }



    public void UpdateMaterials(
        Material materialDiscreteMax8,
        Material materialDiscreteTripling,
        Material materialContinuousTripling,
        Material splatingMaterial
    )
    {
        this.materialDiscreteMax8 = materialDiscreteMax8;
        this.materialDiscreteTripling = materialDiscreteTripling;
        this.materialContinuousTripling = materialContinuousTripling;
        this.splatingMaterial = splatingMaterial;
    }

    /// <summary>
    /// Functions for updating the values from the serialized fields in the main script
    /// </summary>
    public void UpdateBiomPipelineValues( 
        float temperatureNoiseScale, 
        float temperatureNoiseStrength,
        float textureScale
        )
    {
        this.temperatureNoiseScale = temperatureNoiseScale;
        this.temperatureNoiseStrength = temperatureNoiseStrength;
        this.TextureScale = textureScale;
    }


    /// <summary>
    /// Main function for mapping bioms onto the planet, by mainly setting up the texture shaders
    /// </summary>
    /// <param name="vertices">vertices of the sphere</param>
    /// <param name="normals">normals of verticies</param>
    /// <param name="triangles">triangles of the planets mesh</param>
    /// <param name="biomeBlendType">enum that determins what algorithm will be used for biomes</param>
    public void ApplyTexturesToMesh(Vector3[] vertices, Vector3[] normals, int[] triangles, BiomeBlendType biomeBlendType)
    {
        if(!BiomeUtils.AreBiomesValid(biomeCollection, biomeClassifier))
        {
            Debug.Log("Biome generation abandoned");
            return;
        }
        if(biomeClassifier == null || biomeCollection==null)
        {
            Debug.Log("Missing BiomeClassifierSO or BiomeCollection");
            Debug.Log("Biome generation abandoned");
            return;
        }

        Texture2DArray biomeTextureArray = BiomeUtils.GenerateBiomeTextureArray(biomeCollection);

        Material material = null;
        bool hasMoreThan8Biomes = false;
        if(biomeBlendType == BiomeBlendType.Discrete)
        {
            if(biomeCollection.biomes.Count>8)
            {
                hasMoreThan8Biomes = true;
                material = materialDiscreteTripling;
            }
            else
            {
                material = materialDiscreteMax8;
            }
        }
        else if(biomeBlendType == BiomeBlendType.Continuous)
        {
            if (biomeCollection.biomes.Count > 8)
            {
                hasMoreThan8Biomes = true;
                material = materialContinuousTripling;
            }
            else
            {
                material = materialDiscreteMax8;
            }
        }
        else if(biomeBlendType == BiomeBlendType.Splat)
        {
            //SetUpSplatMap(vertices,mesh,);
          //  return;
        }
        else
        {
            Debug.LogError("Didnt choose any possible biome blending aproach");
            return;
        }

        if(material == null)
        {
            Debug.LogError("Material is null, aborting");
            return;
        }

        RegeratedMesh = false;

        this.triangles = triangles;

        int numVertices = meshFilter.sharedMesh.vertices.Length;

        DateTime before = DateTime.Now;

        Vector3[] deformedVerticies = meshFilter.sharedMesh.vertices;

        material.SetTexture("_Biomes", biomeTextureArray);


        if (biomeBlendType == BiomeBlendType.Discrete)
        {
            NativeArray<int> biomesPerVertex = GetBiomeForEachVertexParalel(vertices, normals);

            if (hasMoreThan8Biomes)
            {
                RegeratedMesh = true;
                meshFilter.sharedMesh = BuildNewMeshDiscrete(deformedVerticies, normals, biomesPerVertex);
            }
            else
            {
                UpdateCurrentMeshToNewBiomesMax8Discrete(biomesPerVertex);
            }
        }
        else if (biomeBlendType == BiomeBlendType.Continuous)
        {
            var biomIndicesWeightScores = GetTop4BiomForEachVertex(deformedVerticies, normals);
            if(hasMoreThan8Biomes)
            {
                meshFilter.sharedMesh = BuildNewMeshContinuous(deformedVerticies, normals, biomIndicesWeightScores.Item3);
            }
            else
            {
                UpdateCurrentMeshToNewBiomesMax8Continuous(biomIndicesWeightScores.Item1,biomIndicesWeightScores.Item2);
            }

        }

        material.SetFloat("_Scale", TextureScale);

        DateTime after = DateTime.Now;
        TimeSpan duration = after.Subtract(before);
        Debug.Log("Biom creation Duration in milliseconds: " + duration.Milliseconds);

        meshRenderer.sharedMaterial = material;

    }

    private Mesh BuildNewMeshContinuous(Vector3[] vertices, Vector3[] normals, Dictionary<int, float>[] biomIndiciesWeightScores)
    {
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector4> newBiomeIndices = new List<Vector4>();
        List<Vector4> newBiomeWeights = new List<Vector4>();
        List<int> newTriangles = new List<int>();

        for (int tri = 0; tri < triangles.Length; tri += 3)
        {
            int a = triangles[tri];
            int b = triangles[tri + 1];
            int c = triangles[tri + 2];

            var combined = new Dictionary<int, float>();

            foreach (int vert in new[] { a, b, c })
            {
                foreach (var kvp in biomIndiciesWeightScores[vert])
                {
                    if (!combined.ContainsKey(kvp.Key))
                        combined[kvp.Key] = 0f;
                    combined[kvp.Key] += kvp.Value;
                }
            }

            var top4 = combined.OrderByDescending(kvp => kvp.Value)
                               .Take(4)
                               .Select(kvp => kvp.Key)
                               .OrderBy(i => i)
                               .ToList();
            while (top4.Count < 4) top4.Add(0); // fallback

            foreach (int vert in new[] { a, b, c })
            {
                Vector3 position = vertices[vert];
                Vector3 normal = normals[vert];

                Vector4 weights = Vector4.zero;
                var scores = biomIndiciesWeightScores[vert];
                for (int i = 0; i < 4; i++)
                {
                    int biomeIdx = top4[i];
                    weights[i] = scores.TryGetValue(biomeIdx, out float score) ? score : 0f;
                }
                float total = weights.x + weights.y + weights.z + weights.w + 1e-6f;
                weights /= total;

                newVertices.Add(position);
                newNormals.Add(normal);
                newBiomeIndices.Add(new Vector4(top4[0], top4[1], top4[2], top4[3]));
                newBiomeWeights.Add(weights);
                newTriangles.Add(newVertices.Count - 1);
            }

        }

        RegeratedMesh = true;
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = newVertices.Count > 65535 ?
               UnityEngine.Rendering.IndexFormat.UInt32 :
               UnityEngine.Rendering.IndexFormat.UInt16;

        newMesh.SetVertices(newVertices);
        newMesh.SetNormals(newNormals);
        newMesh.SetTriangles(newTriangles, 0);
        newMesh.SetUVs(2, newBiomeIndices.ToList());
        newMesh.SetUVs(3, newBiomeWeights.ToList());
        newMesh.RecalculateBounds();

        return newMesh;
    }

    /// <summary>
    /// Updates the mesh withe the biomes for discrete solution with max of 8 biomes
    /// </summary>
    /// <param name="biomesPerVertex">biome indices for each vertex</param>
    private void UpdateCurrentMeshToNewBiomesMax8Discrete(NativeArray<int> biomesPerVertex)
    {
        Vector4[] biomeWeights0 = new Vector4[biomesPerVertex.Length];
        Vector4[] biomeWeights1 = new Vector4[biomesPerVertex.Length];

        for (int i = 0; i < biomesPerVertex.Length; i++)
        {
            int biom = biomesPerVertex[i]; // délka 8

            biomeWeights0[i] = new Vector4(0, 0, 0, 0);
            biomeWeights1[i] = new Vector4(0, 0, 0, 0);
            if (biom > 3)
            {
                biomeWeights1[i][biom - 4] = 1;
            }
            else
            {
                biomeWeights0[i][biom] = 1;
            }
        }
        meshFilter.sharedMesh.SetUVs(2, biomeWeights0);
        meshFilter.sharedMesh.SetUVs(3, biomeWeights1);

        biomesPerVertex.Dispose();
    }

    /// <summary>
    /// Updates the mesh with the correct weights for the continuous blending max 8 biomes
    /// </summary>
    /// <param name="biomeIndiciesPerVertices">svriptable objects containing the compute shaders that will get executed</param>
    /// <param name="biomeWeightsPerVertices">vertecies in a spherical shape</param>
    private void UpdateCurrentMeshToNewBiomesMax8Continuous(List<Vector4> biomeIndiciesPerVertices, List<Vector4> biomeWeightsPerVertices)
    {
        Vector4[] biomeWeights0 = new Vector4[biomeIndiciesPerVertices.Count];
        Vector4[] biomeWeights1 = new Vector4[biomeIndiciesPerVertices.Count];

        for (int i = 0; i < biomeIndiciesPerVertices.Count; i++)
        {
            biomeWeights0[i] = Vector4.zero;
            biomeWeights1[i] = Vector4.zero;
            for (int j = 0;j<4;j++)
            {
                int biom = (int)biomeIndiciesPerVertices[i][j]; // délka 8
                if (biom == -1) continue;
                float weight = biomeWeightsPerVertices[i][j];

               // if (i%1000==0&&biom == 0) Debug.Log("weight "+weight);

                if (biom > 3)
                {
                    biomeWeights1[i][biom - 4] = weight;
                }
                else
                {
                    biomeWeights0[i][biom] = weight;
                }
            }
        }
        meshFilter.sharedMesh.SetUVs(2, biomeWeights0);
        meshFilter.sharedMesh.SetUVs(3, biomeWeights1);
    }

    /// <summary>
    /// For the discrete implementation of more than 8 biomes it creates the new mesh with triple the verticies in paralel enviroment IJobParallelFor
    /// </summary>
    /// <param name="layers">svriptable objects containing the compute shaders that will get executed</param>
    /// <param name="baseVertices">vertecies in a spherical shape</param>
    /// <param name="seed">the seed for the random generation</param>
    /// <returns>arry of floats representing the new heights</returns>
    private Mesh BuildNewMeshDiscrete(Vector3[] vertices, Vector3[] normals, NativeArray<int> biomesPerVertex)
    {
        int finalVertCount = triangles.Length;
        NativeArray<float3> vertsNA = new NativeArray<float3>(finalVertCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float3> normsNA = new NativeArray<float3>(finalVertCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float4> idxNA = new NativeArray<float4>(finalVertCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float4> wNA = new NativeArray<float4>(finalVertCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<int> trisNA = new NativeArray<int>(finalVertCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        NativeArray<int> trianglesNA = new NativeArray<int>(triangles.Length, Allocator.TempJob);
        for (int i = 0; i < triangles.Length; i++)
        {
            trianglesNA[i] = triangles[i];
        }
        NativeArray<Vector3> normalsNA = new NativeArray<Vector3>(normals.Length, Allocator.TempJob);
        for (int i = 0; i < normals.Length; i++)
        {
            normalsNA[i] = normals[i];
        }
        NativeArray<Vector3> verticesNA = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob);
        for (int i = 0; i < vertices.Length; i++)
        {
            verticesNA[i] = vertices[i];
        }

        var job2 = new CreateNewTrianglesDiscreteJob
        {
            triangles = trianglesNA,
            deformedVertices = verticesNA,
            normals = normalsNA,
            biomesPerVertex = biomesPerVertex,
            outVerts = vertsNA,
            outNormals = normsNA,
            outIndices = idxNA,
            outWeights = wNA,
            outTris = trisNA
        };

        JobHandle handle2 = job2.Schedule(triangles.Length, 64);
        handle2.Complete();

        RegeratedMesh = true;
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = vertsNA.Length > 65535 ?
               UnityEngine.Rendering.IndexFormat.UInt32 :
               UnityEngine.Rendering.IndexFormat.UInt16;

        newMesh.SetVertices(vertsNA);
        newMesh.SetNormals(normsNA);
        newMesh.SetTriangles(trisNA.ToArray(), 0);
        newMesh.SetUVs(2, idxNA);
        newMesh.SetUVs(3, wNA);

        vertsNA.Dispose();
        normsNA.Dispose();
        idxNA.Dispose();
        wNA.Dispose();
        trisNA.Dispose();

        biomesPerVertex.Dispose();
        trianglesNA.Dispose();
        verticesNA.Dispose();
        normalsNA.Dispose();

        return newMesh;
    }

    /// <summary>
    /// Determind the top 4 most influential bioms for each vertex and their weights
    /// </summary>
    /// <param name="vertices">the position of the vertices</param>
    /// <param name="normals">normals of the verticies</param>
    /// <returns>3 structures, list of 4 indicies for the vertices, list of 4 weights for the vertices, and scores for each vertex</returns>
    private (List<Vector4>,List<Vector4>, Dictionary<int, float>[]) GetTop4BiomForEachVertex(Vector3[] vertices, Vector3[] normals)
    {
        var perVertexIndices = new List<Vector4>(vertices.Length);
        var perVertexWeights = new List<Vector4>(vertices.Length);
       
        var vertexBiomeScores = new Dictionary<int, float>[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            float height = heights[i];
            float slope = BiomeUtils.CalculateSlopeFromNormal(normals[i], vertices[i].normalized);
            float temperature = CalculateTemperature(vertices[i]);

            var scores = new Dictionary<int, float>();
            for (int j = 0; j < biomeCollection.biomes.Count; j++)
            {
                var biome = biomeCollection.biomes[j];

                float bestHeightScore = 0;
                foreach(var h in biome.supportedHeights)
                {
                    float heightCenter = biomeClassifier.GetTypeCenter(h);
                    float heightRange = biomeClassifier.GetTypeRange(h);
                    float heightScoreCur = Mathf.Clamp01(1f - Mathf.Abs(height - heightCenter) / (heightRange * (biome.blenddistance ) + 1e-5f));
                    if (heightScoreCur > bestHeightScore) bestHeightScore = heightScoreCur;
                }
                float heightScore = bestHeightScore;


                float bestSlopeScore = 0;
                foreach (var s in biome.supportedSlopes)
                {
                    float Center = biomeClassifier.GetTypeCenter(s);
                    float Range = biomeClassifier.GetTypeRange(s);
                    float slopeScoreCur = Mathf.Clamp01(1f - Mathf.Abs(slope - Center) / (Range * (biome.blenddistance ) + 1e-5f));
                    if (slopeScoreCur> bestSlopeScore) bestSlopeScore = slopeScoreCur;
                }
                // float slopeScore = bestSlopeScore;
                float slopeScore = Mathf.Pow(bestSlopeScore, 2);

                float bestTempScore = 0;
                foreach (var t in biome.supportedTemperatures)
                {
                    float Center = biomeClassifier.GetTypeCenter(t);
                    float Range = biomeClassifier.GetTypeRange(t);
                    float tempScoreCur = Mathf.Clamp01(1f - Mathf.Abs(temperature - Center) / (Range * (biome.blenddistance ) + 1e-5f));
                    if (tempScoreCur > bestTempScore) bestTempScore = tempScoreCur;
                }
                float tempScore = bestTempScore;

                float finalScore = (4*heightScore * biome.heightAffinity + slopeScore * biome.slopeAffinity)*  tempScore;

                if (finalScore > 0)
                {
                    scores[j]=  finalScore;
                }
            }


            vertexBiomeScores[i] = scores;

            var top4 = scores.OrderByDescending(kv => kv.Value).Take(4).Select(kv => (kv.Key, kv.Value)).ToList();

            while (top4.Count < 4) {
                  top4.Add((-1, 0.0f));
            }
            float totalWeight = top4.Sum(s => s.Item2) + 1e-6f;

            perVertexIndices.Add(new Vector4(top4[0].Item1, top4[1].Item1, top4[2].Item1, top4[3].Item1));
            perVertexWeights.Add(new Vector4(top4[0].Item2 / totalWeight, top4[1].Item2 / totalWeight, top4[2].Item2 / totalWeight, top4[3].Item2 / totalWeight));
        }
        return (perVertexIndices, perVertexWeights, vertexBiomeScores);
    }

    /// <summary>
    /// Calculates a biome for each vertex, one per vertex for the discrete implementation using IJobParallelFor so its parallel
    /// </summary>
    /// <param name="vertices">svriptable objects containing the compute shaders that will get executed</param>
    /// <param name="normals">vertecies in a spherical shape</param>
    /// <returns>the indices for each biome, ina nativeArray for future paralel calculations</returns>
    private NativeArray<int> GetBiomeForEachVertexParalel(Vector3[] vertices, Vector3[] normals)
    {
        NativeArray<int> biomesPerVertex = new NativeArray<int>(vertices.Length, Allocator.TempJob);

        NativeArray<float> heightsNA = new NativeArray<float>(heights.Length, Allocator.TempJob);
        for (int i = 0; i < heights.Length; i++)
        {
            heightsNA[i] = heights[i];
        }
        NativeArray<Vector3> normalsNA = new NativeArray<Vector3>(normals.Length, Allocator.TempJob);
        for (int i = 0; i < normals.Length; i++)
        {
            normalsNA[i] = normals[i];
        }
        NativeArray<Vector3> verticesNA = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob);
        for (int i = 0; i < vertices.Length; i++)
        {
            verticesNA[i] = vertices[i];
        }

        BiomeUtils.BiomeClassifierData biomeClassifierData = BiomeUtils.ConvertClassifierToData(biomeClassifier);

        NativeArray<BiomeUtils.BiomeData> biomData = BiomeUtils.PrepareBiomes(biomeCollection, biomeClassifier);

        AssignOneBiomePerVertexJob job = new AssignOneBiomePerVertexJob
        {
            heights = heightsNA,
            baseVertices = verticesNA,
            normals = normalsNA,
            classifierData = biomeClassifierData,
            biomeCollection = biomData,
            equatorTemperature = EQUATORTEMPERATURE,
            poleTemperature = POLETEMPERATURE,
            temperatureNoiseScale = this.temperatureNoiseScale,
            temperatureNoiseStrength = this.temperatureNoiseStrength,
            biomeIndices = biomesPerVertex,
        };

        JobHandle handle = job.Schedule(vertices.Length, 64); // batch size 64
        handle.Complete();

        verticesNA.Dispose();
        normalsNA.Dispose();
        heightsNA.Dispose();
        biomData.Dispose();

        biomeClassifierData.heightRanges.Dispose();
        biomeClassifierData.tempRanges.Dispose();
        biomeClassifierData.slopeRanges.Dispose();


        return biomesPerVertex;
    }

    /// <summary>
    /// Determins the temperature on the vertex based on the distance from the equator
    /// </summary>
    /// <param name="worldPosition">svriptable objects containing the compute shaders that will get executed</param>
    /// <returns>the temprature for the vertex</returns>
    private float CalculateTemperature(Vector3 worldPosition)
    {
        Vector3 normalized = worldPosition.normalized;

        float latitude = normalized.y;

        float baseTemp = 0;

        if (latitude < 0f) baseTemp = Mathf.Lerp(EQUATORTEMPERATURE, POLETEMPERATURE, -latitude);
        else if (latitude >= 0f) baseTemp = Mathf.Lerp(EQUATORTEMPERATURE, POLETEMPERATURE, latitude);

        float longitude = Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI);
        float lat = Mathf.Asin(normalized.y) / Mathf.PI + 0.5f;

        float u = longitude * temperatureNoiseScale;
        float v = lat * temperatureNoiseScale;

        float noise = Mathf.PerlinNoise(u, v);

        float finalTemp = baseTemp + (noise - 0.5f) * 2f * temperatureNoiseStrength;

        return Mathf.Clamp(finalTemp,POLETEMPERATURE, EQUATORTEMPERATURE);
    }


    //splatting
    private void SetUpSplatMap(Vector3[] vertices, Mesh mesh, List<Vector4> perVertexIndices, List<Vector4> perVertexWeights, Material material, Texture2DArray biomeTextureArray)
    {
        Vector2[] uvs1 = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normalized = vertices[i].normalized;
            float u = 0.5f + Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI);
            float v = 0.5f - Mathf.Asin(normalized.y) / Mathf.PI;
            uvs1[i] = new Vector2(u, v);
        }
        mesh.uv = uvs1;



        Vector3[] vertices1 = mesh.vertices;
        Vector3[] normals1 = mesh.normals;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;

        const int mapResolution = 2048;
        var indexMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, false, true); 
        indexMap.filterMode = FilterMode.Point;
        indexMap.wrapMode = TextureWrapMode.Repeat;
        var weightMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, true, true); 
        weightMap.filterMode = FilterMode.Bilinear;
        weightMap.wrapMode = TextureWrapMode.Repeat;
        var indexMapColors = new Color32[mapResolution * mapResolution];
        var weightMapColors = new UnityEngine.Color[mapResolution * mapResolution];

        Action<Vector2, Vector2, Vector2, Vector4, Vector4, Vector4, Vector4, Vector4, Vector4> rasterizeTriangle =
            (uv0, uv1, uv2, i0, i1, i2, w0, w1, w2) =>
            {
                var allBiomes = new Dictionary<int, (float w_v0, float w_v1, float w_v2)>();
                Action<Vector4, Vector4> addBiomesForVertex = (indices, weights) => {
                    for (int i = 0; i < 4; i++)
                    {
                        int index = (int)indices[i];
                        if (weights[i] > 0 && !allBiomes.ContainsKey(index))
                        {
                            allBiomes.Add(index, (0, 0, 0));
                        }
                    }
                };

                addBiomesForVertex(i0, w0);
                addBiomesForVertex(i1, w1);
                addBiomesForVertex(i2, w2);

                var biomeList = allBiomes.Keys.ToList();
                for (int i = 0; i < biomeList.Count; i++)
                {
                    int biomeIndex = biomeList[i];
                    var currentWeights = allBiomes[biomeIndex];
                    for (int j = 0; j < 4; j++)
                    {
                        if ((int)i0[j] == biomeIndex) currentWeights.w_v0 = w0[j];
                        if ((int)i1[j] == biomeIndex) currentWeights.w_v1 = w1[j];
                        if ((int)i2[j] == biomeIndex) currentWeights.w_v2 = w2[j];
                    }
                    allBiomes[biomeIndex] = currentWeights;
                }

                Vector2 p0 = new Vector2(uv0.x * (mapResolution - 1), uv0.y * (mapResolution - 1));
                Vector2 p1 = new Vector2(uv1.x * (mapResolution - 1), uv1.y * (mapResolution - 1));
                Vector2 p2 = new Vector2(uv2.x * (mapResolution - 1), uv2.y * (mapResolution - 1));

                int xMin = (int)Mathf.Floor(Mathf.Min(p0.x, p1.x, p2.x));
                int xMax = (int)Mathf.Ceil(Mathf.Max(p0.x, p1.x, p2.x));
                int yMin = (int)Mathf.Floor(Mathf.Min(p0.y, p1.y, p2.y));
                int yMax = (int)Mathf.Ceil(Mathf.Max(p0.y, p1.y, p2.y));

                var perPixelScores = new List<(int index, float score)>();

                for (int y = yMin; y <= yMax; y++)
                {
                    for (int x = xMin; x <= xMax; x++)
                    {
                        if (x < 0 || x >= mapResolution || y < 0 || y >= mapResolution) continue;

                        Vector2 p = new Vector2(x, y);
                        Vector3 bary = Barycentric(p, p0, p1, p2);

                        if (bary.x >= -0.001f && bary.y >= -0.001f && bary.z >= -0.001f)
                        {
                            perPixelScores.Clear();
                            foreach (var biomeIndex in biomeList)
                            {
                                var vWeights = allBiomes[biomeIndex];
                                float interpolatedWeight = vWeights.w_v0 * bary.x + vWeights.w_v1 * bary.y + vWeights.w_v2 * bary.z;
                                if (interpolatedWeight > 0.0001f)
                                {
                                    perPixelScores.Add((biomeIndex, interpolatedWeight));
                                }
                            }

                            var top4 = perPixelScores.OrderByDescending(s => s.score).Take(4).ToList();
                            while (top4.Count < 4) { top4.Add((0, 0.0f)); }

                            var finalIndices = new Vector4(top4[0].index, top4[1].index, top4[2].index, top4[3].index);
                            var finalWeights = new Vector4(top4[0].score, top4[1].score, top4[2].score, top4[3].score);

                            int pixelIndex = y * mapResolution + x;
                            indexMapColors[pixelIndex] = new Color32((byte)finalIndices.x, (byte)finalIndices.y, (byte)finalIndices.z, (byte)finalIndices.w);
                            weightMapColors[pixelIndex] = new UnityEngine.Color(finalWeights.x, finalWeights.y, finalWeights.z, finalWeights.w);
                        }
                    }
                }
            };

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            Vector2 uv0 = uvs[v0];
            Vector2 uv1 = uvs[v1];
            Vector2 uv2 = uvs[v2];

            if (Mathf.Max(uv0.x, uv1.x, uv2.x) - Mathf.Min(uv0.x, uv1.x, uv2.x) > 0.8f)
            {
                continue;
            }


            rasterizeTriangle(uv0, uv1, uv2,
                    perVertexIndices[v0], perVertexIndices[v1], perVertexIndices[v2],
                    perVertexWeights[v0], perVertexWeights[v1], perVertexWeights[v2]);



            indexMap.SetPixels32(indexMapColors);
            indexMap.Apply(false); 

            weightMap.SetPixels(weightMapColors);
            weightMap.Apply(true); 

            material.SetTexture("_Biomes", biomeTextureArray);
            material.SetTexture("_IndexMap", indexMap);
            material.SetTexture("_WeightMap", weightMap);
            meshRenderer.sharedMaterial = material;
        }
        
    }

    private Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = p - a;

        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;
        if (Mathf.Abs(denom) < 1e-6f)
            return new Vector3(-1, -1, -1); // Degenerate triangle

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        return new Vector3(u, v, w);
    }


}
