using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "ContinentsMountainsMaskedLayer", menuName = "Planet Generation/Continents Mountains Masked Layer", order = 103)]
public class ContinentsMountainsMaskedLayer : TerrainLayerSO
{
    [Header("Continent (Base) Noise")]
    [SerializeField] Vector3 baseNoiseOffset = Vector3.zero;
    [Range(0.01f, 10f)][SerializeField] private float baseScale = 1f;
    [Range(1, 8)][SerializeField] private int baseOctaves = 4;
    [Range(1f, 4f)][SerializeField] private float baseLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float basePersistence = 0.5f;
    [Range(0f, 2f)][SerializeField] private float baseMultiplier = 1f;

    [Header("Ridge Noise (Mountains)")]
    [SerializeField] Vector3 ridgeNoiseOffset = Vector3.zero;
    [Range(0.01f, 10f)][SerializeField] private float ridgeScale = 1.5f;
    [Range(1, 8)][SerializeField] private int ridgeOctaves = 4;
    [Range(1f, 4f)][SerializeField] private float ridgeLacunarity = 2f;
    [Range(0f, 1f)][SerializeField] private float ridgePersistence = 0.5f;
    [Range(0f, 2f)][SerializeField] private float ridgeMultiplier = 0.6f;

    [Header("Ridge Masking")]
    [SerializeField] Vector3 ridgeMaskingNoiseOffset = Vector3.zero;
    [Range(0.01f, 3f)][SerializeField] float ridgeMaskScale = 0.3f;
    [Range(1, 6)][SerializeField] int ridgeMaskOctaves = 4;
    [Range(1.5f, 3f)][SerializeField] float ridgeMaskLacunarity = 2.0f;
    [Range(0.3f, 0.8f)][SerializeField] float ridgeMaskPersistence = 0.5f;
    [Range(0.2f, 0.8f)][SerializeField] float ridgeMaskThreshold = 0.4f;     // e.g. 0.4
    [Range(0.01f, 0.2f)][SerializeField] float ridgeMaskFalloff = 0.1f;
    [Range(0, 10f), SerializeField] private float ridgeMaskMultiplier = 1;

    [Header("Oceans")]
    [SerializeField, Range(0f, 1f)]
    private float oceanFloorDepth = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float oceanFloorSmoothing = 0.05f;

    [SerializeField, Range(0f, 2f)]
    private float oceanDepthMultiplier = 0.5f;



    [Header("Final Output")]
    [Range(0f, 10f)][SerializeField] private float heightMultiplier = 1f;


    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        if (!layerEnabled || computeShader == null || kernelHandle < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogWarning($"Skipping layer '{this.name}' due to missing requirements.", this);
            return;
        }

        computeShader.SetBuffer(kernelHandle, "vertices", positionBuffer);
        computeShader.SetBuffer(kernelHandle, "heights", heightBuffer);
        computeShader.SetInt("numVertices", numVertices);

        computeShader.SetFloat("continentScale", baseScale);
        computeShader.SetInt("continentOctaves", baseOctaves);
        computeShader.SetFloat("continentLacunarity", baseLacunarity);
        computeShader.SetFloat("continentPersistence", basePersistence);
        computeShader.SetFloat("continentStrength", baseMultiplier);

        computeShader.SetFloat("mountainScale", ridgeScale);
        computeShader.SetInt("mountainOctaves", ridgeOctaves);
        computeShader.SetFloat("mountainLacunarity", ridgeLacunarity);
        computeShader.SetFloat("mountainPersistence", ridgePersistence);
        computeShader.SetFloat("mountainStrength", ridgeMultiplier);

        //computeShader.SetFloat("mountainMaskMin", ridgeMinBase);
        //computeShader.SetFloat("mountainMaskPower", ridgeStartPower);

        computeShader.SetFloat("oceanFloorDepth", oceanFloorDepth);
        computeShader.SetFloat("oceanFloorSmoothing", oceanFloorSmoothing);
        computeShader.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);


        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        // Offset
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("baseNoiseOffset", (randomOffset+baseNoiseOffset));
        Vector3 randomOffset2 = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("ridgeNoiseOffset", (randomOffset+ ridgeNoiseOffset));
        
        Vector3 randomOffset3 = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("maskNoiseOffset", (randomOffset+ ridgeMaskingNoiseOffset));

        computeShader.SetFloat("ridgeMaskMultiplier", ridgeMaskMultiplier);
        computeShader.SetFloat("ridgeMaskScale", ridgeMaskScale);
        computeShader.SetInt("ridgeMaskOctaves", ridgeMaskOctaves);
        computeShader.SetFloat("ridgeMaskLacunarity", ridgeMaskLacunarity);
        computeShader.SetFloat("ridgeMaskPersistence", ridgeMaskPersistence);
        computeShader.SetFloat("ridgeMaskThreshold", ridgeMaskThreshold);
        computeShader.SetFloat("ridgeMaskFalloff", ridgeMaskFalloff);



    }

    public override void ReleaseAnySpecificBuffers()
    {

    }
}