using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static BiomePipeline;

[BurstCompile]
struct AssignBiomeJob : IJobParallelFor
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

public struct FlattenTrianglesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> triangles;
    [ReadOnly] public NativeArray<Vector3> deformedVertices;
    [ReadOnly] public NativeArray<Vector3> normals;
    [ReadOnly] public NativeArray<int> biomesPerVertex;

    [WriteOnly] public NativeArray<float3> outVerts;
    [WriteOnly] public NativeArray<float3> outNormals;
    [WriteOnly] public NativeArray<float4> outIndices;
    [WriteOnly] public NativeArray<float4> outWeights;
    [WriteOnly] public NativeArray<int> outTris;

    public void Execute(int i)
    {
        int tri = i / 3;
        int local = i % 3;

        int a = triangles[tri * 3];
        int b = triangles[tri * 3 + 1];
        int c = triangles[tri * 3 + 2];

        int srcVert = local == 0 ? a : (local == 1 ? b : c);
        float4 indices = new float4(biomesPerVertex[a], biomesPerVertex[b], biomesPerVertex[c], 0);

        outVerts[i] = deformedVertices[srcVert];
        outNormals[i] = normals[srcVert];
        outIndices[i] = indices;

        float4 w = float4.zero;
        w[local] = 1f;
        outWeights[i] = w;

        outTris[i] = i;
    }
}