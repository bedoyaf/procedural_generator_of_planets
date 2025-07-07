using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static BiomePipeline;

[BurstCompile]
struct AssignOneBiomePerVertexJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> heights;
    [ReadOnly] public NativeArray<Vector3> baseVertices;
    [ReadOnly] public NativeArray<Vector3> normals;
    [ReadOnly] public BiomeClassifierData classifierData;
    [ReadOnly] public NativeArray<BiomeData> biomeCollection;
    public float temperatureNoiseScale;
    public float temperatureNoiseStrength;
    public float equatorTemperature;
    public float poleTemperature;

    [WriteOnly] public NativeArray<int> biomeIndices;

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
            BiomeData bd = biomeCollection[b];
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
        for (int r = 0; r < ranges.Length; r++)
            if (v >= ranges[r].min && v <= ranges[r].max) return r;
        return 0;
    }

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