using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Main script for running terrain generation, goes through compute shaders
/// </summary>
/// <remarks>
/// Runs each stores layer with a compute shader, has IDisposable to release all relevant buffer when necessary
/// </remarks>
public class TerrainPipelineProcessor : IDisposable
{
    private ComputeBuffer positionBuffer;
    private ComputeBuffer heightBuffer;

    private float[] currentHeights; 
    private int numVertices;

    /// <summary>
    /// Sets up the pipeline by initializing buffers
    /// </summary>
    /// <param name="vertexCount"></param>
    /// <returns>returns success</returns>
    public bool Initialize(int vertexCount)
    {
        ReleaseBuffers(); 

        if (vertexCount <= 0)
        {
            Debug.LogError("Cannot initialize TerrainPipelineProcessor with zero or negative vertices.");
            return false;
        }

        numVertices = vertexCount;
        Debug.Log($"Initializing Compute Buffers for {numVertices} vertices.");

        try
        {
            positionBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3); 
            heightBuffer = new ComputeBuffer(numVertices, sizeof(float));     
                                                                             
            currentHeights = new float[numVertices];
            for (int i = 0; i < numVertices; ++i) currentHeights[i] = 1.0f;

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize compute buffers: {e.Message}");
            ReleaseBuffers(); // Ensure cleanup on failure
            return false;
        }
    }

    /// <summary>
    /// Runs the lists of TerrainLayerSO, running each compute shader and storing the new heights
    /// </summary>
    /// <param name="layers">svriptable objects containing the compute shaders that will get executed</param>
    /// <param name="baseVertices">vertecies in a spherical shape</param>
    /// <param name="seed">the seed for the random generation</param>
    /// <returns>arry of floats representing the new heights</returns>
    public float[] ProcessTerrain(List<TerrainLayerSO> layers, Vector3[] baseVertices, int seed)
    {
        if (positionBuffer == null || heightBuffer == null || !positionBuffer.IsValid() || !heightBuffer.IsValid())
        {
            Debug.LogError("Compute buffers are not initialized or invalid. Cannot process terrain.");
            return null;
        }

        if (baseVertices == null || baseVertices.Length != numVertices)
        {
            Debug.LogError("Base vertices data is null or does not match buffer size.");
            return null;
        }

        if (layers == null || layers.Count == 0)
        {
            Debug.LogWarning("No terrain layers provided. Returning initial heights.");

            float[] initialHeightsCopy = new float[numVertices];
            Array.Copy(currentHeights, initialHeightsCopy, numVertices);
            return initialHeightsCopy;
        }


        Debug.Log("Starting Terrain Generation Pipeline...");
        UnityEngine.Random.InitState(seed); 

        try
        {
            currentHeights = new float[numVertices]; 

            positionBuffer.SetData(baseVertices); 
            heightBuffer.SetData(currentHeights); 

            foreach (TerrainLayerSO layer in layers)
            {
                if (layer != null && layer.layerEnabled && layer.computeShader != null)
                {
                    Debug.Log("name "+layer.kernelHandle);
                    if (layer.kernelHandle < 0) layer.FindKernel(); 
                    if (layer.kernelHandle < 0)
                    {
                        Debug.LogWarning($"Skipping layer '{layer.name}' due to invalid kernel.", layer);
                        continue;
                    }

                    layer.SetShaderParameters( positionBuffer, heightBuffer, numVertices);
                    Debug.Log($"Dispatching Layer: {layer.name}");
                    layer.Dispatch( numVertices);
                }
                else if (layer == null) { Debug.LogWarning($"Skipping layer '{layer.name}' - Compute Shader is null.", layer); }
            }

            heightBuffer.GetData(currentHeights); 

            Debug.Log("Terrain Generation Pipeline Finished.");
            return currentHeights; 
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during terrain pipeline processing: {e.Message}");
            return null; 
        }
    }

    public void ReleaseBuffers()
    {
        positionBuffer?.Release();
        positionBuffer = null;
        heightBuffer?.Release();
        heightBuffer = null;

        numVertices = 0;
        currentHeights = null;
    }

    public void Dispose()
    {
        ReleaseBuffers();

        GC.SuppressFinalize(this);
    }
}