using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class NoiseShaderController : ShaderControllerAbstract
{
    [Header("Noise Settings")]
    public float noiseScale = 3f;
    public float amplitude = 0.3f;
    public float power = 1.2f;
    public float baseHeight = 1.0f;


    public override void SetupTerrainGenerator(MeshFilter meshFilter, Vector3[] originalVertices, float sphereRadius)
    {
        _filter = meshFilter;
        this.originalVertices = originalVertices;
        numVertices = originalVertices.Length;

        verticesBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3);
        heightsBuffer = new ComputeBuffer(numVertices, sizeof(float));
        deformedVertices = originalVertices;
        heights = new float[numVertices];
        this.sphereRadius = sphereRadius;
        Debug.Log("kraterovac inicializovan");
        //  RunComputeShader();
    }

    [ContextMenu("Run Compute Shader")]
    public override void RunComputeShader()
    {
        Debug.Log("Generating Noise Terrain");

        Mesh mesh = _filter.sharedMesh;
        originalVertices = mesh.vertices;
        numVertices = originalVertices.Length;

        if (originalVertices == null || numVertices == 0)
        {
            Debug.LogError("Mesh has no vertices!");
            return;
        }

        if (verticesBuffer != null) verticesBuffer.Release();
        if (heightsBuffer != null) heightsBuffer.Release();

        verticesBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3);
        heightsBuffer = new ComputeBuffer(numVertices, sizeof(float));

        verticesBuffer.SetData(originalVertices);
        heights = new float[numVertices];
        heightsBuffer.SetData(heights);

        int kernel = computeShader.FindKernel("NoiseTerrain");

        Debug.Log("Kernel found! Index: " + kernel);


        computeShader.SetBuffer(kernel, "vertices", verticesBuffer);
        computeShader.SetBuffer(kernel, "heights", heightsBuffer);
        computeShader.SetInt("numVertices", numVertices);
        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("amplitude", amplitude);
        computeShader.SetFloat("power", power);
        computeShader.SetFloat("baseHeight", baseHeight);

        int threadGroups = Mathf.CeilToInt(numVertices / 512f);
        computeShader.Dispatch(kernel, threadGroups, 1, 1);

        heightsBuffer.GetData(heights);

        deformedVertices = new Vector3[numVertices];
        for (int i = 0; i < numVertices; i++)
        {
            Vector3 direction = originalVertices[i].normalized;
            deformedVertices[i] = direction * heights[i];
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Debug.Log("Noise terrain generated");
    }

    private void OnDestroy()
    {
        verticesBuffer?.Release();
        heightsBuffer?.Release();
    }


}
