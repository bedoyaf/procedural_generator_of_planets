using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "FractalNoiseLayer2", menuName = "Planet Generation/Fractal Noise Layer2", order = 103)]
public class FractalNoiseLayerSO2 : TerrainLayerSO
{
    [Header("Fractal Noise Settings")]
    // This seems specific to the shader's internal calculations, potentially different from the main planet radius.
    // Keep it, but be aware of potential confusion if the shader could just use the passed '_Radius'.
   // public float shaderBaseRadius = 10f;
    public float noiseScale = 1.0f;
    public float heightMultiplier = 1.0f;
   // public Vector3 noiseOffset = Vector3.zero;

    [Range(1, 12)] public int octaves = 6;
    [Range(0.1f, 1.0f)] public float persistence = 0.5f;
    [Range(1.0f, 4.0f)] public float lacunarity = 2.0f;

    // These seem like specific noise shaping parameters
  //  [Range(0.0f, 1.0f)] public float ridgeFactor = 0.0f; // 0 = standard FBM, 1 = ridged noise
  //  [Range(0.1f, 5.0f)] public float powerExponent = 1.0f; // Apply power to final noise output
//
    // IMPORTANT: Set this in the Inspector to "GenerateSphereNoiseTrippy"
    // Or uncomment the Reset method below.
    // [SerializeField] protected new string kernelName = "GenerateSphereNoiseTrippy";


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
      //  shader.SetFloat("sphereRadius", radius);                   // Main planet radius

        // Set layer-specific parameters (use _ convention)
       // shader.SetFloat("baseRadius", radius); // Pass the shader-specific radius
        shader.SetFloat("noiseScale", noiseScale);
        shader.SetFloat("heightMultiplier", heightMultiplier);

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );


        shader.SetVector("noiseOffset", randomOffset);
        shader.SetInt("octaves", octaves);
        shader.SetFloat("persistence", persistence);
        shader.SetFloat("lacunarity", lacunarity);
    //    shader.SetFloat("ridgeFactor", ridgeFactor);
    //    shader.SetFloat("powerExponent", powerExponent);

    //    uint numVertices;
  //      float noiseScale;
   //     float heightMultiplier;
 //       int octaves;
   //     float lacunarity;
   //     float persistence;


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