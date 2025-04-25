using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;
using System.Linq;

public class BiomPipeline : MonoBehaviour
{
    private ComputeBuffer biomeIndexBuffer;
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
    [SerializeField] private Material material;

    [SerializeField] private BiomeCollectionSO biomeCollection;
    [SerializeField] private BiomeClassifierSO biomeClassifier;

    private Vector3[] baseVertices;

    private float[] heights;

    private float sphereRadius;

    private Mesh mesh;

    private MeshFilter meshFilter;

    private MeshRenderer meshRenderer;



    public void Initialize(MeshRenderer meshRenderer, MeshFilter meshFilter)
    {
        meshRenderer.material = material;
        mesh = meshFilter.mesh;
        this.meshRenderer = meshRenderer;
        this.meshFilter = meshFilter;
    }

    public void UpdateBiomPipeline(float radius, float[] heights)
    {
        this.sphereRadius = radius;
        this.heights = heights;
    }

    public void UpdateBiomPipelineMesh(Mesh mesh)
    {
        this.mesh = mesh;
    }

    private Texture2DArray GenerateBiomeTextureArray(BiomeCollectionSO biomeCollection)
    {
        int textureSize = 512; // adjust based on your source textures
        Texture2DArray texArray = new Texture2DArray(textureSize, textureSize, biomeCollection.biomes.Count, TextureFormat.RGBA32, true);

        for (int i = 0; i < biomeCollection.biomes.Count; i++)
        {
            Texture2D source = biomeCollection.biomes[i].biomeTexture;

            // Create resized readable texture
            RenderTexture rt = RenderTexture.GetTemporary(textureSize, textureSize);
            Graphics.Blit(source, rt);

            RenderTexture.active = rt;
            Texture2D readableTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            readableTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            // Copy into the texture array
            texArray.SetPixels(readableTex.GetPixels(), i);
        }

        texArray.Apply();
        return texArray;
    }


    public void ApplyTexturesToMesh(Vector3[] vertices, Vector3[] normals)
    {
        baseVertices = vertices;

        int numVertices = baseVertices.Length;
        int[] biomeIndices = new int[numVertices];


        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;


        for (int i = 0; i < heights.Length; i++)
        {
            float h = heights[i];
            if (h < minHeight) minHeight = h;
            if (h > maxHeight) maxHeight = h;
        }
        Debug.Log("highest point is " + maxHeight);
        Debug.Log("lowest point is " + minHeight);


        List<Vector4> biomeUVs = new List<Vector4>(numVertices);
        for (int i = 0; i < numVertices; i++)
        {
            float temp = CalculateTemperature(baseVertices[i]);
            biomeIndices[i] = FindBiomeIndex(heights[i], temp, normals[i], vertices[i]);

            biomeUVs.Add(new Vector4((float)biomeIndices[i], 0f, 0f, 0f));
        }

        mesh.SetUVs(2, biomeUVs);
        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_BiomeTexArray", biomTexArray);

        meshRenderer.sharedMaterial = material;
        Debug.Log("Assigned texture array with " + biomTexArray.depth + " layers to material.");
    }


    private float CalculateTemperature(Vector3 worldPosition)
    {
        // Normalize to unit sphere
        Vector3 normalized = worldPosition.normalized;

        // Latitude: -1 at south pole, 0 at equator, 1 at north pole
        float latitude = normalized.y;

        //Debug.Log(latitude);

        // Map to 0..1: poles = poleTemperature, equator = equatorTemperature
        float baseTemp = 0;

        if (latitude < 0f) baseTemp = Mathf.Lerp(equatorTemperature, poleTemperature, -latitude);
        else if (latitude >= 0f) baseTemp = Mathf.Lerp(equatorTemperature, poleTemperature, latitude);

        // Longitude/Latitude for noise coordinates
        float longitude = Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI); // -0.5 to 0.5
        float lat = Mathf.Asin(normalized.y) / Mathf.PI + 0.5f; // 0 to 1

        // Scale and wrap
        float u = longitude * temperatureNoiseScale;
        float v = lat * temperatureNoiseScale;

        // Use spherical UVs as noise coordinates
        float noise = Mathf.PerlinNoise(u, v);

        float finalTemp = baseTemp + (noise - 0.5f) * 2f * temperatureNoiseStrength;

        return Mathf.Clamp01(finalTemp);
        //    return baseTemp;
    }

    private int FindBiomeIndex(float height, float temperature, Vector3 normal, Vector3 vertex)
    {
        float slope = CalculateSlopeFromNormal(normal, vertex);

        if (slope > slopeThreshold)
            return 0;

        var heightType = biomeClassifier.GetHeightType(height);
        var tempType = biomeClassifier.GetTemperatureType(temperature);

        // Find index directly using FindIndex for elegance
        var possibleBiomes = biomeCollection.biomes
            .Select((b, index) => new { Biome = b, Index = index })
            .Where(x =>
                x.Biome.supportedHeights.Contains(heightType) &&
                x.Biome.supportedTemperatures.Contains(tempType)
            )
            .OrderByDescending(x => x.Biome.priority) // highest priority wins
            .ToList();

        // Fallback to first biome if none match
        return possibleBiomes.Count > 0 ? possibleBiomes[0].Index : 0;
    }

    float CalculateSlopeFromNormal(Vector3 normal, Vector3 localUP)
    {
        // The slope angle in radians (dot product between the normal and the up direction)
        float slopeAngle = Vector3.Angle(normal, localUP);

        // You can then convert it to a range of your choice, for example, degrees or as a factor
        return slopeAngle; // Or transform it to a desired scale
    }


}