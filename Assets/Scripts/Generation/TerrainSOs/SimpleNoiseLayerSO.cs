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

    public override void SetShaderParameters(ComputeShader shader, int kernel, ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices, float radius)
    {
        // Basic validation
        if (!enabled || shader == null || kernel < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogError($"Skipping layer '{this.name}' due to missing requirements (enabled={enabled}, shader={(shader != null)}, kernel={kernel}, posBuffer={(positionBuffer != null)}, heightBuffer={(heightBuffer != null)})", this);
            return;
        }

        // Set common buffers using standardized names
        shader.SetBuffer(kernel, "_Positions", positionBuffer); // Expects unit sphere positions
        shader.SetBuffer(kernel, "_Heights", heightBuffer);     // Expects current height multipliers (read/write)
        shader.SetInt("_NumVertices", numVertices);
        shader.SetFloat("_Radius", radius);                   // Pass the main planet radius


        shader.SetFloat("_NoiseScale", noiseScale);
        shader.SetFloat("_HeightMultiplier", heightMultiplier);
    }
    /*
    // Optional: Automatically set the kernel name when the asset is created/reset
    protected override void Reset()
    {
        kernelName = "GenerateSphereNoise"; // Default kernel for this type
        base.Reset(); // Call base Reset to find the kernel handle
    }*/
}