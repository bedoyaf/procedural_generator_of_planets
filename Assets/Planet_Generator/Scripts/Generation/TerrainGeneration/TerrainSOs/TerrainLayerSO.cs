using System;
using UnityEngine;


/// <summary>
/// Runs the compute shaders for planet terrain
/// </summary>
/// <remarks>
/// Just sets up the shaders and dispatches them
/// </remarks>
public abstract class TerrainLayerSO : ScriptableObject
{
    [SerializeField] public bool layerEnabled = true; // Allows easily toggling layers
    [SerializeField] public ComputeShader computeShader;
    [HideInInspector] public string kernelName = "CSMain"; 

    [HideInInspector] public int kernelHandle = -1;

    /// <summary>
    /// Searches for the kernelHandle for shader setup
    /// </summary>
    public void FindKernel()
    {
        if (computeShader != null && !string.IsNullOrEmpty(kernelName))
        {
            kernelHandle = computeShader.FindKernel(kernelName);
            if (kernelHandle < 0)
            {
                Debug.LogError($"Kernel '{kernelName}' not found in Compute Shader '{computeShader.name}' for layer '{this.name}'.", this);
            }
        }
        else
        {
            kernelHandle = -1; // Mark as invalid
            if (computeShader == null) Debug.LogError($"Compute Shader is null for layer '{this.name}'.", this);
            if (string.IsNullOrEmpty(kernelName)) Debug.LogError($"Kernel Name is empty for layer '{this.name}'.", this);
        }
    }


    /// <summary>
    /// Sets up the shader with the buffers and serialized fields
    /// </summary>
    /// <param name="positionBuffer">buffer of the mesh positions</param>
    /// <param name="heightBuffer">buffer of the heights(start at 0)</param>
    /// <param name="numVertices">the number of vertices</param>
    public abstract void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices);

    /// <summary>
    /// Dispathes the shader to make it run
    /// </summary>
    /// <param name="kernel"></param>
    public void Dispatch( int numVertices)
    {
        if (!layerEnabled || computeShader == null || kernelHandle < 0) return;

        int threadGroups = Mathf.Max(1, Mathf.CeilToInt(numVertices / 512.0f));
        computeShader.Dispatch(kernelHandle, threadGroups, 1, 1);
    }
    /// <summary>
    /// Abstract function to release any buffers created for the specific implementation
    /// </summary>
    public abstract void ReleaseAnySpecificBuffers();
}