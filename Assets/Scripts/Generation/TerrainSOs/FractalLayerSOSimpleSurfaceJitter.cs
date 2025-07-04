using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "SimpleNoise", menuName = "Planet Generation/Simple Noise", order = 103)]
public class SimpleNoiseLayerSO2 : TerrainLayerSO
{
    [Header("Fractal Noise Settings")]
    // This seems specific to the shader's internal calculations, potentially different from the main planet radius.
    // Keep it, but be aware of potential confusion if the shader could just use the passed '_Radius'.
   // public float shaderBaseRadius = 10f;
    public float noiseScale = 1.0f;
    public float heightMultiplier = 1.0f;


    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        // Basic validation
        if (!layerEnabled || computeShader == null || kernelHandle < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogError($"Skipping layer '{this.name}' due to missing requirements (enabled={layerEnabled}, computeShader={(computeShader != null)}, kernel={kernelHandle}, posBuffer={(positionBuffer != null)}, heightBuffer={(heightBuffer != null)})", this);
            return;
        }

        // Set common buffers using standardized names
        computeShader.SetBuffer(kernelHandle, "vertices", positionBuffer); // Unit sphere positions
        computeShader.SetBuffer(kernelHandle, "heights", heightBuffer);     // Current height multipliers (read/write)
        computeShader.SetInt("numVertices", numVertices);


        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );



        // Note: Similar assumption as above. This shader likely reads "_Positions",
        // calculates complex noise, potentially using "_BaseRadius" and other params,
        // and writes a final height to "_Heights". Adjust shader if additive behaviour needed.
    }
    /*
    // Optional: Automatically set the kernel name
    protected override void Reset()
    {
        kernelName = "GenerateSphereNoiseTrippy"; // Default kernel for this type
        base.Reset(); // Call base Reset to find the kernel handle
    }*/
}