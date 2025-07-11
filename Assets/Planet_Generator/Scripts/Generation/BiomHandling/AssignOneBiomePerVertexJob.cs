using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static BiomePipeline;

/// <summary>
/// Paralel struct that find a biom for each vertex thanks to IJOBParalellelFOr
/// </summary>
[BurstCompile]
struct AssignOneBiomePerVertexJob : IJobParallelFor
{
    //All non reference data to use in the assigment
    [ReadOnly] public NativeArray<float> heights;
    [ReadOnly] public NativeArray<Vector3> baseVertices;
    [ReadOnly] public NativeArray<Vector3> normals;
    [ReadOnly] public BiomeUtils.BiomeClassifierData classifierData;
    [ReadOnly] public NativeArray<BiomeUtils.BiomeData> biomeCollection;
    public float temperatureNoiseScale;
    public float temperatureNoiseStrength;
    public float equatorTemperature;
    public float poleTemperature;
    //output
    [WriteOnly] public NativeArray<int> biomeIndices;


    /// <summary>
    /// Goes through each vertex and finds the biom
    /// </summary>
    /// <param name="i">biome index</param>
    public void Execute(int i)
    {
        float height = heights[i];
        Vector3 worldPos = baseVertices[i];
        Vector3 normal = normals[i];

        float temperature = CalculateTemperature(worldPos);
        float slope = Vector3.Angle(normal, worldPos.normalized);

        var heightType = GetTypeIndex(height, classifierData.heightRanges);
        var tempType = GetTypeIndex(temperature, classifierData.tempRanges);
        var slopeType = GetTypeIndex(slope, classifierData.slopeRanges);

        uint hMask = 1u << heightType;
        uint tMask = 1u << tempType;
        uint sMask = 1u << slopeType;

        int chosenBiom = 0;
        for (int b = 0; b < biomeCollection.Length; b++)
        {
            BiomeUtils.BiomeData bd = biomeCollection[b];
            if ((bd.heightMask & hMask) != 0 &&
                 (bd.tempMask & tMask) != 0 &&
                 (bd.slopeMask & sMask) != 0)
            {
                chosenBiom = b;
                break;
            }
        }

        biomeIndices[i] = chosenBiom;
    }

    int GetTypeIndex(float v, NativeArray<FloatRange> ranges)
    {
        for (int r = 0; r < ranges.Length; r++) if (ranges[r].Contains(v)) return r;
        return 0;
    }

    /// <summary>
    /// Same as in the main biome script calulates temperature based on distance from the equator.
    /// </summary>
    /// <param name="worldPosition">the vertex position</param>
    /// <returns>The temperature value</returns>
    float CalculateTemperature(Vector3 worldPosition)
    {
        Vector3 normalized = worldPosition.normalized;
        float latitude = normalized.y;

        float baseTemp = latitude < 0f ?
            Mathf.Lerp(equatorTemperature, poleTemperature, -latitude) :
            Mathf.Lerp(equatorTemperature, poleTemperature, latitude);

        float longitude = Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI);
        float lat = Mathf.Asin(normalized.y) / Mathf.PI + 0.5f;

        float u = longitude * temperatureNoiseScale;
        float v = lat * temperatureNoiseScale;

        float noise = Mathf.PerlinNoise(u, v);
        return Mathf.Clamp01(baseTemp + (noise - 0.5f) * 2f * temperatureNoiseStrength);
    }
}