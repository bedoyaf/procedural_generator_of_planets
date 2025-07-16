using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic; 
using System.Runtime.InteropServices;

/// <summary>
/// Layer from TerrainLayerSO, focused on creating craters on the planet
/// </summary>
[CreateAssetMenu(fileName = "CraterLayer", menuName = "Planet Generation/Crater Layer", order = 101)]
public class CraterLayerSO : TerrainLayerSO
{
    private struct CraterData 
    {
        public float3 center;
        public float radius;
        public float depth;
    }

    [Header("Crater Settings")]
    [Range(1,100)] public ushort userOffset = 0;
    public int numCraters = 10;
    public Vector2 craterRadiusRange = new Vector2(0.05f, 0.2f);
    public Vector2 floorHeightRange = new Vector2(-0.5f, -0.2f); 
    public Vector2 rimWidthRange = new Vector2(0.2f, 0.4f);
    public Vector2 rimSteepnessRange = new Vector2(1f, 3f);
    public float smoothness = 0.1f;

    private ComputeBuffer craterBuffer;
    private List<CraterData> craterList = new List<CraterData>();


    /// <summary>
    /// Sets up the shader with the buffers and serialized fields
    /// </summary>
    /// <param name="positionBuffer">buffer of the mesh positions</param>
    /// <param name="heightBuffer">buffer of the heights(start at 0)</param>
    /// <param name="numVertices">the number of vertices in the mesh</param>
    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        ReleaseBuffers();
        if (!layerEnabled || computeShader == null || kernelHandle < 0) return;

        if (craterBuffer == null || craterList.Count != numCraters)
        {
            Vector3[] originalPositions = new Vector3[numVertices];
            positionBuffer.GetData(originalPositions); 
            if (originalPositions != null)
            {
                GenerateCraterData(originalPositions, numVertices);
            }
            else
            {
                Debug.LogError("Could not get original positions for crater generation.", this);
                return;
            }
        }

        if (craterBuffer == null || craterList.Count == 0)
        {
            Debug.LogWarning($"Crater buffer not ready or empty for layer '{this.name}'. Skipping.", this);
            return; 
        }

        computeShader.SetBuffer(kernelHandle, "vertices", positionBuffer);
        computeShader.SetBuffer(kernelHandle, "heights", heightBuffer);
        computeShader.SetInt("numVertices", numVertices);

        computeShader.SetBuffer(kernelHandle, "craters", craterBuffer);
        computeShader.SetInt("numCraters", craterList.Count);
        computeShader.SetFloat("smoothness", smoothness);

        computeShader.SetFloat("rimSteepness", UnityEngine.Random.Range(rimSteepnessRange.x, rimSteepnessRange.y));
        computeShader.SetFloat("floorHeight", UnityEngine.Random.Range(floorHeightRange.x, floorHeightRange.y));
        computeShader.SetFloat("rimWidth", UnityEngine.Random.Range(rimWidthRange.x, rimWidthRange.y));

    }

    /// <summary>
    /// it creates the craters for the shader by generating the struct for it
    /// </summary>
    /// <param name="originalVertices">the mesh vertices</param>
    /// <param name="numVertices">number of vertices in the mesh</param>
    private void GenerateCraterData(Vector3[] originalVertices, int numVertices)
    {
        if (originalVertices == null || originalVertices.Length != numVertices)
        {
            Debug.LogError("Invalid original vertices for crater generation.", this);
            return;
        }

        uint combinedSeed = (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue) * userOffset;
        var random = new Unity.Mathematics.Random(combinedSeed);

        craterList.Clear();
        for (int i = 0; i < numCraters; i++)
        {
            int randomIndex = random.NextInt(0, numVertices);
            Vector3 randomCenter = originalVertices[randomIndex].normalized;

            float randomRadius = random.NextFloat(craterRadiusRange.x, craterRadiusRange.y);

            craterList.Add(new CraterData
            {
                center = randomCenter,
                radius = randomRadius,
            });
        }

        if (craterBuffer != null)
        {
            craterBuffer.Release();
            craterBuffer = null;
        }

        if (craterList.Count > 0)
        {
            craterBuffer = new ComputeBuffer(craterList.Count, Marshal.SizeOf(typeof(CraterData)));
            craterBuffer.SetData(craterList);
        }
        else
        {
            craterBuffer = null;
        }
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    /// <summary>
    /// Crater buffer release
    /// </summary>
    public void ReleaseBuffers()
    {
        if (craterBuffer != null)
        {
            craterBuffer?.Release();
            craterBuffer = null; 
        //    Debug.Log($"Released crater buffer for layer '{this.name}'.");
        }
    }

    public override void ReleaseAnySpecificBuffers()
    {
        ReleaseBuffers();
    }
}