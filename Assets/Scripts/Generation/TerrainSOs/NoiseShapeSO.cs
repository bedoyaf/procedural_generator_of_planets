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

    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        if (!layerEnabled || computeShader == null || kernelHandle < 0) return;

        computeShader.SetBuffer(kernelHandle, "vertices", positionBuffer); // Assuming computeShader uses _Positions
        computeShader.SetBuffer(kernelHandle, "heights", heightBuffer);     // Assuming computeShader uses _Heights
        computeShader.SetInt("numVertices", numVertices);
   //     computeShader.SetFloat("sphereRadius", radius);

        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("amplitude", amplitude);
        computeShader.SetFloat("power", power);
        computeShader.SetFloat("baseHeight", baseHeightContribution);

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