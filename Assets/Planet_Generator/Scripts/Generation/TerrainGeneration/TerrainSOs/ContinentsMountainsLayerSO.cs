using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Layer from TerrainLayerSO, focused on creating realisting earthlike terrain
/// </summary>
[CreateAssetMenu(fileName = "ContinentsMountainsLayer", menuName = "Planet Generation/Continents Mountains Layer", order = 103)]
public class ContinentsMountainsLayer : TerrainLayerSO
{
    [Header("Fractal Noise Settings")]
    [Range(0f, 10f)][SerializeField] private float heightMultiplier = 1f;

    [Header("Noise offsets")]
    [SerializeField] Vector3 baseNoiseOffset = Vector3.zero;
    [SerializeField] Vector3 ridgeNoiseOffset = Vector3.zero;
    [SerializeField] Vector3 ridgeAttunationNoiseOffset = Vector3.zero;


    [Header("Base")]
    [Range(0.01f, 10f)][SerializeField] private float baseScale = 1f;
    [Range(1, 8)][SerializeField] private int baseOctaves = 8;
    [Range(1f, 4f)][SerializeField] private float baseLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float basePersistence = 0.5f;
    [Range(0f, 2f)][SerializeField] private float baseMultiplier = 1f;

    [Header("Detail")]
    [Range(0.01f, 50f)][SerializeField] private float detailScale = 20f;
    [Range(1, 8)][SerializeField] private int detailOctaves = 1;
    [Range(1f, 4f)][SerializeField] private float detailLacunarity = 3.87f;
    [Range(0f, 1f)][SerializeField] private float detailPersistence = 0.169f;
    [Range(0f, 2f)][SerializeField] private float detailMultiplier = 0.01f;

    [Header("Ridge")]
    [Range(0.01f, 10f)][SerializeField] private float ridgeScale = 3.1f;
    [Range(0f, 10f)][SerializeField] private float ridgeMultiplier = 1.92f;
    [Range(1, 8)][SerializeField] private int ridgeOctaves = 8;
    [Range(1f, 4f)][SerializeField] private float ridgeLacunarity = 2.59f;
    [Range(0f, 1f)][SerializeField] private float ridgePersistence = 0.147f;
    [Header("Ridge Attenuation")]
    [SerializeField, Range(-10f, 10f)] private float ridgeMinBase = 6.8f; 

    [SerializeField, Range(0.1f, 5f)] private float ridgeStartPower = 0.53f; 

    [SerializeField, Range(0f, 10f)]
    private float ridgeAttenuationScale = 1f;

    [SerializeField, Range(0f, 10f)]
    private float ridgeAttenuationFrequency = 1f;

    [SerializeField, Range(1, 10)]
    private int ridgeAttenuationOctaves = 3;

    [SerializeField, Range(0f, 1f)]
    private float ridgeAttenuationPersistence = 0.5f;


    /// <summary>
    /// Sets up the shader with the buffers and serialized fields
    /// </summary>
    /// <param name="positionBuffer">buffer of the mesh positions</param>
    /// <param name="heightBuffer">buffer of the heights(start at 0)</param>
    /// <param name="numVertices">the number of vertices in the mesh</param>
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
        computeShader.SetFloat("baseScale", baseScale);
        computeShader.SetInt("baseOctaves", baseOctaves);
        computeShader.SetFloat("baseLacunarity", baseLacunarity);
        computeShader.SetFloat("basePersistence", basePersistence);
        computeShader.SetFloat("baseMultiplier", baseMultiplier);

        // Detail layer noise
        computeShader.SetFloat("detailScale", detailScale);
        computeShader.SetInt("detailOctaves", detailOctaves);
        computeShader.SetFloat("detailLacunarity", detailLacunarity);
        computeShader.SetFloat("detailPersistence", detailPersistence);
        computeShader.SetFloat("detailMultiplier", detailMultiplier);

        // Height multiplier
        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        computeShader.SetFloat("ridgeScale", ridgeScale);
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

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("baseNoiseOffset", randomOffset + baseNoiseOffset);
        Vector3 randomOffset2 = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("ridgeNoiseOffset", randomOffset2 + ridgeNoiseOffset);
        Vector3 randomOffset3 = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("ridgeAttenuationNoiseOffset", randomOffset3+ ridgeAttunationNoiseOffset);
    }

    public override void ReleaseAnySpecificBuffers(){}
}