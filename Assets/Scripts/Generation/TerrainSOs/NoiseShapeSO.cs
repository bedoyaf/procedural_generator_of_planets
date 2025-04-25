using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "NoiseLayer", menuName = "Planet Generation/Noise Layer", order = 100)]
public class NoiseLayerSO : TerrainLayerSO
{
    [Header("Noise Settings")]
    public float noiseScale = 3f;
    public float amplitude = 0.3f;
    public float power = 1.2f;
    public float baseHeightContribution = 1.0f; // How much base sphere radius contributes
    public bool additive = false; // If true, adds noise; if false, sets height based on noise

    // Override kernel name if different from "CSMain"
    // [SerializeField] protected new string kernelName = "NoiseTerrain"; // Example if needed

    public override void SetShaderParameters(ComputeShader shader, int kernel, ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices, float radius)
    {
        if (!enabled || shader == null || kernel < 0) return;

        shader.SetBuffer(kernel, "vertices", positionBuffer); // Assuming shader uses _Positions
        shader.SetBuffer(kernel, "heights", heightBuffer);     // Assuming shader uses _Heights
        shader.SetInt("numVertices", numVertices);
   //     shader.SetFloat("sphereRadius", radius);

        shader.SetFloat("noiseScale", noiseScale);
        shader.SetFloat("amplitude", amplitude);
        shader.SetFloat("power", power);
        shader.SetFloat("baseHeight", baseHeightContribution);

        //   shader.SetBool("additive", additive);
    }
    /*
    // Optional: Override Reset/OnValidate if kernel name needs specific handling
    protected override void Reset()
    {
        kernelName = "NoiseTerrain"; // Set default kernel name for this type
        base.Reset();
    }*/
}