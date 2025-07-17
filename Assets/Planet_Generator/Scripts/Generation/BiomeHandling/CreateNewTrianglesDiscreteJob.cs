using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static BiomePipeline;

[BurstCompile]
public struct CreateNewTrianglesDiscreteJob : IJobParallelFor
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