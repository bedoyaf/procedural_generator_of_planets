using UnityEngine;

[CreateAssetMenu(fileName = "SimpleNoiseLayer", menuName = "Planet Generation/Simple Noise Layer", order = 102)]
public class SimpleNoiseLayerSO : TerrainLayerSO
{
    [Header("Simple Noise Settings")]
    public float noiseScale = 1.0f;
    public float heightMultiplier = 1.0f;

    // IMPORTANT: Set this in the Inspector to "GenerateSphereNoise"
    // Or uncomment the Reset method below to set it by default.
    // [SerializeField] protected new string kernelName = "GenerateSphereNoise";

    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        // Basic validation
        if (!layerEnabled || computeShader == null || kernelHandle < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogError($"Skipping layer '{this.name}' due to missing requirements (enabled={layerEnabled}, computeShader={(computeShader != null)}, kernel={kernelHandle}, posBuffer={(positionBuffer != null)}, heightBuffer={(heightBuffer != null)})", this);
            return;
        }

        // Set common buffers using standardized names
        computeShader.SetBuffer(kernelHandle, "_Positions", positionBuffer); // Expects unit sphere positions
        computeShader.SetBuffer(kernelHandle, "_Heights", heightBuffer);     // Expects current height multipliers (read/write)
        computeShader.SetInt("_NumVertices", numVertices);
        computeShader.SetFloat("_Radius", radius);                   // Pass the main planet radius


        computeShader.SetFloat("_NoiseScale", noiseScale);
        computeShader.SetFloat("_HeightMultiplier", heightMultiplier);
    }
    /*
    // Optional: Automatically set the kernel name when the asset is created/reset
    protected override void Reset()
    {
        kernelName = "GenerateSphereNoise"; // Default kernel for this type
        base.Reset(); // Call base Reset to find the kernel handle
    }*/
}