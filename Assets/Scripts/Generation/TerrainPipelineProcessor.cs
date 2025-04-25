using UnityEngine;
using System.Collections.Generic;
using System; // Needed for IDisposable

// Doesn't need to be a MonoBehaviour
// Implement IDisposable for proper buffer cleanup
public class TerrainPipelineProcessor : IDisposable
{
    private ComputeBuffer positionBuffer;
    private ComputeBuffer heightBuffer;
    // Add other buffers if your pipeline needs them (e.g., normalBuffer)

    private float[] currentHeights; // Internal storage for initial/final heights
    private int numVertices;

    public bool Initialize(int vertexCount)
    {
        ReleaseBuffers(); // Release any existing buffers first

        if (vertexCount <= 0)
        {
            Debug.LogError("Cannot initialize TerrainPipelineProcessor with zero or negative vertices.");
            return false;
        }

        numVertices = vertexCount;
        Debug.Log($"Initializing Compute Buffers for {numVertices} vertices.");

        try
        {
            positionBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3); // Stride for Vector3
            heightBuffer = new ComputeBuffer(numVertices, sizeof(float));     // Stride for float
                                                                              // Initialize other buffers...

            // Prepare initial height data (all 1.0f)
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

    public float[] ProcessTerrain(List<TerrainLayerSO> layers, Vector3[] baseVertices, float radius, int seed)
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
            // Return a copy of the initial heights
            float[] initialHeightsCopy = new float[numVertices];
            Array.Copy(currentHeights, initialHeightsCopy, numVertices);
            return initialHeightsCopy;
        }


        Debug.Log("Starting Terrain Generation Pipeline...");
        UnityEngine.Random.InitState(seed); // Ensure consistent randomness for layers

        try
        {
            currentHeights = new float[numVertices]; //skibidien
            // Upload initial data
            positionBuffer.SetData(baseVertices);   // Unit sphere positions
            heightBuffer.SetData(currentHeights);  // Initial heights (usually 1.0)

            // Run the pipeline
            foreach (TerrainLayerSO layer in layers)
            {
                if (layer != null && layer.enabled && layer.computeShader != null)
                {
                    Console.WriteLine("name"+layer.kernelName);
                    if (layer.kernelHandle < 0) layer.FindKernel(); // Ensure kernel is found
                    if (layer.kernelHandle < 0)
                    {
                        Debug.LogWarning($"Skipping layer '{layer.name}' due to invalid kernel.", layer);
                        continue;
                    }

                    // Set params & dispatch
                    layer.SetShaderParameters(layer.computeShader, layer.kernelHandle, positionBuffer, heightBuffer, numVertices, radius);
                    Debug.Log($"Dispatching Layer: {layer.name}");
                    layer.Dispatch(layer.computeShader, layer.kernelHandle, numVertices);
                }
                else if (layer != null && !layer.enabled) { /* Log skip */ }
                else if (layer != null) { Debug.LogWarning($"Skipping layer '{layer.name}' - Compute Shader is null.", layer); }
            }

            // Get results back
            heightBuffer.GetData(currentHeights); // Read final heights back into our array


       /*   for (int i = 0; i < Mathf.Min(20, currentHeights.Length); i++)
            {
                Debug.Log($"Vertex {i}: FinalHeight = {currentHeights[i]:F4}");
            }*/
            Debug.Log("Terrain Generation Pipeline Finished.");
            return currentHeights; // Return the processed height data
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during terrain pipeline processing: {e.Message}");
            return null; // Return null on error
        }
    }

    public void ReleaseBuffers()
    {
        positionBuffer?.Release();
        positionBuffer = null;
        heightBuffer?.Release();
        heightBuffer = null;
        // Release other buffers...

        // Release buffers held by SOs (important!)
        // This part is tricky as the processor doesn't own the layers list.
        // The PlanetGenerator should perhaps iterate its layers and call release on them.
        // Or, the SOs themselves could be made IDisposable, but that's less common.
        // For now, let's assume PlanetGenerator handles SO buffer release.

        numVertices = 0;
        currentHeights = null;
        // Debug.Log("Compute buffers released by TerrainPipelineProcessor."); // Less verbose logging
    }

    // Implement IDisposable
    public void Dispose()
    {
        ReleaseBuffers();
        // Suppress finalization because we've handled cleanup
        GC.SuppressFinalize(this);
    }

    // Optional: Finalizer as a backup (not always necessary if Dispose is called correctly)
    ~TerrainPipelineProcessor()
    {
        // This finalizer will be called by the GC if Dispose() was not called.
        // It's a safety net but can have performance implications.
        // Debug.LogWarning("TerrainPipelineProcessor finalizer called - Dispose() was missed?");
        ReleaseBuffers();
    }
}