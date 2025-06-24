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
    [SerializeField] public string kernelName = "CSMain"; 

    [SerializeField] protected int threadGroupSize = 512;

    protected float radius;

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
    /// Sets up the shader params
    /// </summary>
    public abstract void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices);

    /// <summary>
    /// Dispathes the shader to make it run
    /// </summary>
    /// <param name="kernel"></param>
    public void Dispatch( int numVertices)
    {
        if (!layerEnabled || computeShader == null || kernelHandle < 0) return;

        int threadGroups = Mathf.Max(1, Mathf.CeilToInt(numVertices / threadGroupSize));
        computeShader.Dispatch(kernelHandle, threadGroups, 1, 1);
    }
}