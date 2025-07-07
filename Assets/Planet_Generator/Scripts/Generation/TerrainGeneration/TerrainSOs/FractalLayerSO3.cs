using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "FractalNoiseLayer3", menuName = "Planet Generation/Fractal Noise Layer3", order = 103)]
public class FractalNoiseLayerSO3 : TerrainLayerSO
{
    [Header("Fractal Noise Settings")]
    [Range(0f, 10f)][SerializeField] private float heightMultiplier = 1f;

    [Header("Base")]
    [Range(0.01f, 10f)][SerializeField] private float baseScale = 1f;
    [Range(1, 8)][SerializeField] private int baseOctaves = 4;
    [Range(1f, 4f)][SerializeField] private float baseLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float basePersistence = 0.5f;
    [Range(0f, 2f)][SerializeField] private float baseMultiplier = 1f;
    [Range(0f, 10f)][SerializeField] private float baseNoiseScale = 1f;

    [Header("Detail")]
    [Range(0.01f, 50f)][SerializeField] private float detailScale = 10f;
    [Range(1, 8)][SerializeField] private int detailOctaves = 6;
    [Range(1f, 4f)][SerializeField] private float detailLacunarity = 2.2f;
    [Range(0f, 1f)][SerializeField] private float detailPersistence = 0.55f;
    [Range(0f, 2f)][SerializeField] private float detailMultiplier = 0.3f;
    [Range(0f, 10f)][SerializeField] private float detailNoiseScale = 1f;

    [Header("Ridge")]
    [Range(0.01f, 10f)][SerializeField] private float ridgeScale = 1.5f;
    [Range(0f, 10f)][SerializeField] private float ridgeMultiplier = 0.6f;
    [Range(1, 8)][SerializeField] private int ridgeOctaves = 4;
    [Range(1f, 4f)][SerializeField] private float ridgeLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float ridgePersistence = 0.5f;
    [Header("Ridge Attenuation")]
    [SerializeField, Range(-10f, 10f)] private float ridgeMinBase = 0.4f; // Ridges start appearing above this base noise value

    [SerializeField, Range(0.1f, 5f)] private float ridgeStartPower = 2.5f; // Controls how sharply ridges fade in

    [SerializeField, Range(0f, 10f)]
    private float ridgeAttenuationScale = 1f;

    [SerializeField, Range(0f, 10f)]
    private float ridgeAttenuationFrequency = 1f;

    [SerializeField, Range(1, 10)]
    private int ridgeAttenuationOctaves = 3;

    [SerializeField, Range(0f, 1f)]
    private float ridgeAttenuationPersistence = 0.5f;


    // Seeded offset (or randomized)
    //  [SerializeField] private Vector3 noiseOffset = Vector3.zero;


    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        if (!layerEnabled || computeShader == null || kernelHandle < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogWarning($"Skipping layer '{this.name}' due to missing requirements.", this);
            return;
        }

        // Required buffers
        computeShader.SetBuffer(kernelHandle, "vertices", positionBuffer);
        computeShader.SetBuffer(kernelHandle, "heights", heightBuffer);

        // General
        computeShader.SetInt("numVertices", numVertices);

        // Base layer noise
        computeShader.SetFloat("baseNoiseScale", baseNoiseScale); // optional, not used in computeShader right now
        computeShader.SetFloat("baseScale", baseScale);
        computeShader.SetInt("baseOctaves", baseOctaves);
        computeShader.SetFloat("baseLacunarity", baseLacunarity);
        computeShader.SetFloat("basePersistence", basePersistence);
        computeShader.SetFloat("baseMultiplier", baseMultiplier);

        // Detail layer noise
        computeShader.SetFloat("detailNoiseScale", detailNoiseScale); // optional, not used in computeShader right now
        computeShader.SetFloat("detailScale", detailScale);
        computeShader.SetInt("detailOctaves", detailOctaves);
        computeShader.SetFloat("detailLacunarity", detailLacunarity);
        computeShader.SetFloat("detailPersistence", detailPersistence);
        computeShader.SetFloat("detailMultiplier", detailMultiplier);

        // Height multiplier
        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        computeShader.SetFloat("ridgeScale", ridgeScale); // Tune as needed
        computeShader.SetFloat("ridgeMultiplier", ridgeMultiplier);
        computeShader.SetInt("ridgeOctaves", ridgeOctaves);
        computeShader.SetFloat("ridgeLacunarity", ridgeLacunarity);
        computeShader.SetFloat("ridgePersistence", ridgePersistence);

        computeShader.SetFloat("ridgeStartPower", ridgeStartPower);
        computeShader.SetFloat("ridgeMinBase", ridgeMinBase);
        computeShader.SetFloat("ridgeAttenuationScale", ridgeAttenuationScale);
        computeShader.SetFloat("ridgeAttenuationFrequency", ridgeAttenuationFrequency);
        computeShader.SetFloat("ridgeAttenuationOctaves", ridgeAttenuationOctaves);
        computeShader.SetFloat("ridgeAttenuationPersistence", ridgeAttenuationPersistence);


        // Noise offset (random or seeded)
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("noiseOffset", randomOffset); // assume set in the inspector or by code
    }


}