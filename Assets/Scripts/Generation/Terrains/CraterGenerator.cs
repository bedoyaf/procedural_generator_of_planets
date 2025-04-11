using NUnit.Framework.Internal;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static PlanetGenerator;

public class CraterGenerator : ShaderControllerAbstract
{
    public bool settedUp = false;
    public bool running = false;


    [SerializeField] ComputeShader computeShaderCrater;

    List<Crater> craterList = new List<Crater>();

    List<int> randomSpotsForCraters = new List<int>();

    int numCraters=1;


    public void SetupTerrainGenerator(MeshFilter meshFilter, Vector3[] originalVertices, int numCraters)
    {
        _filter = meshFilter;
        this.originalVertices = originalVertices;
        numVertices = originalVertices.Length;

        Debug.Log("num craters" + numCraters);
        this.numCraters = numCraters;

        Debug.Log("num craters" + numCraters);
        verticesBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3);
        heightsBuffer = new ComputeBuffer(numVertices, sizeof(float));
        deformedVertices = originalVertices;
        heights = new float[numVertices];
        settedUp = true;
        Debug.Log("kraterovac inicializovan");
      //  RunComputeShader();
    }

    [SerializeField] float smoothness = 0f;
    [SerializeField] float rimSteepness = 0.5f;
    [SerializeField] float floorHeight = -1f;
    //[SerializeField] float craterDepth = 1f;
    [SerializeField] float rimWidth = 0.7f;

    //   [SerializeField] float depthCrater = -0.2f;
    [SerializeField] float radiusCrater = 0.5f;//0.17f;

    [ContextMenu("Run Compute Shader")]
    public override void RunComputeShader()
    {
        //  if (originalVertices == null || _filter.mesh == null) GeneratePlanet();
        Debug.Log("Generating Craters");

        running = true;

        int kernel = 0;
        ComputeShader computeShader;

        kernel = computeShaderCrater.FindKernel("ComputeCrater");
        computeShader = computeShaderCrater;
        SetupCraters();

        if (craterBuffer == null)
        {
            Debug.LogError("craterBuffer is null! Crater buffer must be initialized before running the compute shader.");
            return;
        }

        Debug.Log($"numVertices: {numVertices}, numCraters: {craterList.Count}");
        Debug.Log($"First Crater: {craterList[0].center}, Radius: {craterList[0].radius}");
        Debug.Log($"craterBuffer size: {craterBuffer.count}");

        computeShader.SetBuffer(kernel, "craters", craterBuffer);
        computeShader.SetInt("numCraters", craterList.Count);
        computeShader.SetFloat("smoothness", smoothness);
        computeShader.SetFloat("rimSteepness", rimSteepness);
        computeShader.SetFloat("floorHeight", floorHeight);
      //  computeShader.SetFloat("craterDepth", craterDepth);
        computeShader.SetFloat("rimWidth", rimWidth);
        computeShader.SetInt("numVertices", numVertices);




        // Reset deformedVertices to original shape before applying modifications
        deformedVertices = (Vector3[])originalVertices.Clone();

        if (heightsBuffer == null || verticesBuffer == null)
        {
            Debug.LogError("Compute buffers are not initialized!");
            return;
        }
        if (originalVertices == null || originalVertices.Length != numVertices)
            Debug.LogError("originalVertices is not properly initialized!");

        verticesBuffer.SetData(originalVertices);
        Debug.Log(originalVertices.Length);
        heightsBuffer.SetData(heights);

        // Choose the correct kernel

        computeShader.SetBuffer(kernel, "vertices", verticesBuffer);
        computeShader.SetBuffer(kernel, "heights", heightsBuffer);
        computeShader.SetInt("numVertices", numVertices);

        int threadGroups = Mathf.Max(1, Mathf.CeilToInt(numVertices / 512.0f));
        computeShader.Dispatch(kernel, threadGroups, 1, 1);

        // Get modified heights
        heightsBuffer.GetData(heights);
        Debug.Log($"Computed Heights{heights.Length}: {heights[0]}, {heights[1]}, {heights[2]}");
        for (int i = 0; i < numVertices; i++)
        {
            if (i < 5) // Limit the output to avoid too many logs
                Debug.Log($"Height at index {i}: {heights[i]}");
        }
        for (int i = 0; i < numVertices; i++)
        {
            if (float.IsNaN(heights[i]) || float.IsInfinity(heights[i]))
            {
                //Debug.LogError($"Invalid height at index {i}: {heights[i]}");
                heights[i] = 1.0f;  // Default to 1 to prevent mesh issues
            }
        }

        // Apply height-based modification
        for (int i = 0; i < numVertices; i++)
        {
            deformedVertices[i] = /*originalVertices[i] +*/ originalVertices[i] * heights[i];
        }

        // Update mesh
        Mesh mesh = _filter.mesh;
        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Debug.Log("Done generating");
        running = false;
    }


    struct Crater
    {
        public float3 center;
        public float radius;
        public float depth;
        //     public float rimWidth;
        //     public float rimSteepness;
    }//amogus



    private void SetupCraters()
    {
        craterList.Clear(); // Clear any previous data


        // Example craters (you can UnityEngine.Random.ize this)
        //    craterList.Add(new Crater { center = new Vector3(0, 0, 0), radius = radiusCrater, depth = depth});
        for (int i = 0; i < numCraters; i++)
        {
            int randomIndex;
            Vector3 randomCenter;

            if (randomSpotsForCraters.Count <= i)
            {
                randomIndex = UnityEngine.Random.Range(0, originalVertices.Length);
                randomSpotsForCraters.Add(randomIndex);
                randomCenter = originalVertices[randomIndex];
            }
            else
            {
                randomCenter = originalVertices[randomSpotsForCraters[i]];
            }

            float randomRadius = radiusCrater;//UnityEngine.Random.Range(minCraterRadius, maxCraterRadius);
                                              //     float randomDepth = depthCrater;// UnityEngine.Random.Range(minCraterDepth, maxCraterDepth);
                                              //   float randomRimWidth =// UnityEngine.Random.Range(0.05f, 0.2f);
                                              //    float randomRimSteepness =// UnityEngine.Random.Range(0.5f, 2.0f);

            craterList.Add(new Crater
            {
                center = randomCenter,
                radius = randomRadius,
                depth = 0 //randomDepth, 
                          //      rimWidth = randomRimWidth,
                          //   rimSteepness = randomRimSteepness
            });
        }

        // Create or update crater buffer
        if (craterBuffer != null)
            craterBuffer.Release();
        craterBuffer = new ComputeBuffer(craterList.Count, 20);
        craterBuffer.SetData(craterList.ToArray());

    }


}
