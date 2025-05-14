using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "FractalNoiseLayer4", menuName = "Planet Generation/Fractal Noise Layer4", order = 103)]
public class FractalNoiseLayerSO4 : TerrainLayerSO
{
    [Header("Continent (Base) Noise")]
    [Range(0.01f, 10f)][SerializeField] private float baseScale = 1f;
    [Range(1, 8)][SerializeField] private int baseOctaves = 4;
    [Range(1f, 4f)][SerializeField] private float baseLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float basePersistence = 0.5f;
    [Range(0f, 2f)][SerializeField] private float baseMultiplier = 1f;

    [Header("Ridge Noise (Mountains)")]
    [Range(0.01f, 10f)][SerializeField] private float ridgeScale = 1.5f;
    [Range(1, 8)][SerializeField] private int ridgeOctaves = 4;
    [Range(1f, 4f)][SerializeField] private float ridgeLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float ridgePersistence = 0.5f;
    [Range(0f, 2f)][SerializeField] private float ridgeMultiplier = 0.6f;

    [Header("Ridge Masking from Base")]
    [Range(0f, 1f)][SerializeField] private float ridgeMinBase = 0.4f;
    [Range(0.1f, 5f)][SerializeField] private float ridgeStartPower = 2.5f;

    [Header("Oceans")]
    [SerializeField, Range(0f, 1f)]
    private float oceanFloorDepth = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float oceanFloorSmoothing = 0.05f;

    [SerializeField, Range(0f, 2f)]
    private float oceanDepthMultiplier = 0.5f;



    [Header("Final Output")]
    [Range(0f, 10f)][SerializeField] private float heightMultiplier = 1f;


    public override void SetShaderParameters(ComputeShader shader, int kernel, ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices, float radius)
    {
        if (!enabled || shader == null || kernel < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogWarning($"Skipping layer '{this.name}' due to missing requirements.", this);
            return;
        }

        shader.SetBuffer(kernel, "vertices", positionBuffer);
        shader.SetBuffer(kernel, "heights", heightBuffer);
        shader.SetInt("numVertices", numVertices);

        shader.SetFloat("continentScale", baseScale);
        shader.SetInt("continentOctaves", baseOctaves);
        shader.SetFloat("continentLacunarity", baseLacunarity);
        shader.SetFloat("continentPersistence", basePersistence);
        shader.SetFloat("continentStrength", baseMultiplier);

        shader.SetFloat("mountainScale", ridgeScale);
        shader.SetInt("mountainOctaves", ridgeOctaves);
        shader.SetFloat("mountainLacunarity", ridgeLacunarity);
        shader.SetFloat("mountainPersistence", ridgePersistence);
        shader.SetFloat("mountainStrength", ridgeMultiplier);

        shader.SetFloat("mountainMaskMin", ridgeMinBase);
        shader.SetFloat("mountainMaskPower", ridgeStartPower);

        shader.SetFloat("oceanFloorDepth", oceanFloorDepth);
        shader.SetFloat("oceanFloorSmoothing", oceanFloorSmoothing);
        shader.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);


        shader.SetFloat("heightMultiplier", heightMultiplier);

        // Offset
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        shader.SetVector("noiseOffset", randomOffset);
    }


}