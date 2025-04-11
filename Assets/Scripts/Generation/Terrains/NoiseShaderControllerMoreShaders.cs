using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class NoiseShaderControllerMoreShaders : ShaderControllerAbstract
{
    [Header("Noise Settings")]
    [SerializeField] private float noiseScale = 1.0f;
    [SerializeField] private float heightMultiplier = 1.0f;




    public void SetupTerrainGenerator(MeshFilter meshFilter, Vector3[] originalVertices)
    {
        _filter = meshFilter;
        this.originalVertices = originalVertices;
        numVertices = originalVertices.Length;

        verticesBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3);
        heightsBuffer = new ComputeBuffer(numVertices, sizeof(float));
        deformedVertices = originalVertices;
        heights = new float[numVertices];
    }


    [ContextMenu("Run Compute Shader")]
    public override void RunComputeShader()
    {
        if (computeShader == null)
        {
            Debug.LogError("ComputeShader not assigned!");
            return;
        }

        if (_filter == null || _filter.mesh == null)
        {
            Debug.LogError("MeshFilter or mesh not found!");
            return;
        }

        Mesh mesh = _filter.mesh;
        //originalVertices = mesh.vertices;
        numVertices = originalVertices.Length;
        deformedVertices = new Vector3[numVertices];
        heights = new float[numVertices];

        // Initialize buffers
        if (verticesBuffer != null) verticesBuffer.Release();
        if (heightsBuffer != null) heightsBuffer.Release();

        verticesBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3);
        heightsBuffer = new ComputeBuffer(numVertices, sizeof(float));

        verticesBuffer.SetData(originalVertices);
        heightsBuffer.SetData(heights);

        int kernel = computeShader.FindKernel("GenerateSphereNoise");

        Debug.Log("Kernel found! Index: " + kernel);

        computeShader.SetBuffer(kernel, "vertices", verticesBuffer);
        computeShader.SetBuffer(kernel, "heights", heightsBuffer);
        computeShader.SetInt("numVertices", numVertices);
        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        int threadGroups = Mathf.CeilToInt(numVertices / 512f);
        computeShader.Dispatch(kernel, threadGroups, 1, 1);

        heightsBuffer.GetData(heights);

        // Apply deformation
        for (int i = 0; i < numVertices; i++)
        {
            deformedVertices[i] = originalVertices[i].normalized * heights[i];
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        verticesBuffer?.Release();
        heightsBuffer?.Release();
    }

}
