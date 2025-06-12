using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "SimpleNoiseLayer2", menuName = "Planet Generation/SimpleNoiseLayer2", order = 103)]
public class FractalNoiseLayerSOSurfaceRoughness : TerrainLayerSO
{
    [Header("Fractal Noise Settings")]
    // This seems specific to the shader's internal calculations, potentially different from the main planet radius.
    // Keep it, but be aware of potential confusion if the shader could just use the passed '_Radius'.
   // public float shaderBaseRadius = 10f;
    public float noiseScale = 1.0f;
    public float heightMultiplier = 1.0f;


    public override void SetShaderParameters(ComputeShader shader, int kernel, ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices, float radius)
    {
        // Basic validation
        if (!enabled || shader == null || kernel < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogError($"Skipping layer '{this.name}' due to missing requirements (enabled={enabled}, shader={(shader != null)}, kernel={kernel}, posBuffer={(positionBuffer != null)}, heightBuffer={(heightBuffer != null)})", this);
            return;
        }

        // Set common buffers using standardized names
        shader.SetBuffer(kernel, "vertices", positionBuffer); // Unit sphere positions
        shader.SetBuffer(kernel, "heights", heightBuffer);     // Current height multipliers (read/write)
        shader.SetInt("numVertices", numVertices);


        shader.SetFloat("noiseScale", noiseScale);
        shader.SetFloat("heightMultiplier", heightMultiplier);

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