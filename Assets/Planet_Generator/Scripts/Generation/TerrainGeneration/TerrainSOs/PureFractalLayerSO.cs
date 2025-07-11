using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "PureFractalLayer", menuName = "Planet Generation/Pure Fractal Layer", order = 103)]
public class PureFractalLayerSO : TerrainLayerSO
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
      //  computeShader.SetFloat("sphereRadius", radius);                   // Main planet radius

        // Set layer-specific parameters (use _ convention)
       // computeShader.SetFloat("baseRadius", radius); // Pass the computeShader-specific radius
        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );


        computeShader.SetVector("noiseOffset", randomOffset);
        computeShader.SetInt("octaves", octaves);
        computeShader.SetFloat("persistence", persistence);
        computeShader.SetFloat("lacunarity", lacunarity);
    //    computeShader.SetFloat("ridgeFactor", ridgeFactor);
    //    computeShader.SetFloat("powerExponent", powerExponent);

    //    uint numVertices;
  //      float noiseScale;
   //     float heightMultiplier;
 //       int octaves;
   //     float lacunarity;
   //     float persistence;


        // Note: Similar assumption as above. This computeShader likely reads "_Positions",
        // calculates complex noise, potentially using "_BaseRadius" and other params,
        // and writes a final height to "_Heights". Adjust computeShader if additive behaviour needed.
    }
    /*
    // Optional: Automatically set the kernel name
    protected override void Reset()
    {
        kernelName = "GenerateSphereNoiseTrippy"; // Default kernel for this type
        base.Reset(); // Call base Reset to find the kernel handle
    }*/
}