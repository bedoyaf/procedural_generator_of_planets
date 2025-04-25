using System;
using UnityEngine;

// Base class for all terrain generation layers/steps
public abstract class TerrainLayerSO : ScriptableObject
{
    public bool enabled = true; // Allows easily toggling layers
    [SerializeField] public ComputeShader computeShader;
    [SerializeField] public string kernelName = "CSMain"; // Default, override if needed

    protected float radius;

    public int kernelHandle = -1;

    // Called once to find the kernel
    public virtual void FindKernel()
    {
        if (computeShader != null && !string.IsNullOrEmpty(kernelName))
        {
            Console.WriteLine("nvm jak jsem se sem dostal"+kernelName);
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

    // Abstract method for derived classes to set their specific shader parameters
    // Takes the shared buffers as arguments.
    public abstract void SetShaderParameters(ComputeShader shader, int kernel, ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices, float radius);

    // Method to dispatch the shader for this layer
    public virtual void Dispatch(ComputeShader shader, int kernel, int numVertices)
    {
        if (!enabled || shader == null || kernel < 0) return;

        // Sensible default thread group calculation, might need adjustment
        int threadGroups = Mathf.Max(1, Mathf.CeilToInt(numVertices / 512.0f));
        shader.Dispatch(kernel, threadGroups, 1, 1);
    }


    /*
    // Optional: Reset method called when script is added or reset in inspector
    protected virtual void Reset()
    {
        FindKernel(); // Attempt to find kernel on reset
    }

    // Optional: Validate method called when values change in inspector
    protected virtual void OnValidate()
    {
        FindKernel(); // Re-check kernel if shader/name changes
    }*/
}