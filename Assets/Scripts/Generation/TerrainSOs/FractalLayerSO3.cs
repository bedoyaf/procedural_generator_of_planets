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
    [Range(0f, 2f)][SerializeField] private float ridgeMultiplier = 0.6f;
    [Range(1, 8)][SerializeField] private int ridgeOctaves = 4;
    [Range(1f, 4f)][SerializeField] private float ridgeLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float ridgePersistence = 0.5f;


    // Seeded offset (or randomized)
    //  [SerializeField] private Vector3 noiseOffset = Vector3.zero;


    public override void SetShaderParameters(ComputeShader shader, int kernel, ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices, float radius)
    {
        if (!enabled || shader == null || kernel < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogWarning($"Skipping layer '{this.name}' due to missing requirements.", this);
            return;
        }

        // Required buffers
        shader.SetBuffer(kernel, "vertices", positionBuffer);
        shader.SetBuffer(kernel, "heights", heightBuffer);

        // General
        shader.SetInt("numVertices", numVertices);

        // Base layer noise
        shader.SetFloat("baseNoiseScale", baseNoiseScale); // optional, not used in shader right now
        shader.SetFloat("baseScale", baseScale);
        shader.SetInt("baseOctaves", baseOctaves);
        shader.SetFloat("baseLacunarity", baseLacunarity);
        shader.SetFloat("basePersistence", basePersistence);
        shader.SetFloat("baseMultiplier", baseMultiplier);

        // Detail layer noise
        shader.SetFloat("detailNoiseScale", detailNoiseScale); // optional, not used in shader right now
        shader.SetFloat("detailScale", detailScale);
        shader.SetInt("detailOctaves", detailOctaves);
        shader.SetFloat("detailLacunarity", detailLacunarity);
        shader.SetFloat("detailPersistence", detailPersistence);
        shader.SetFloat("detailMultiplier", detailMultiplier);

        // Height multiplier
        shader.SetFloat("heightMultiplier", heightMultiplier);

        shader.SetFloat("ridgeScale", ridgeScale); // Tune as needed
        shader.SetFloat("ridgeMultiplier", ridgeMultiplier);
        shader.SetInt("ridgeOctaves", ridgeOctaves);
        shader.SetFloat("ridgeLacunarity", ridgeLacunarity);
        shader.SetFloat("ridgePersistence", ridgePersistence);

        // Noise offset (random or seeded)
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        shader.SetVector("noiseOffset", randomOffset); // assume set in the inspector or by code
    }


}