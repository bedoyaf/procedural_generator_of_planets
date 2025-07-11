using UnityEngine;

/// <summary>
/// Class that represents all the relevant value in generation of the planet, allows to store planet templates
/// </summary>
[CreateAssetMenu(fileName = "PlanetSO", menuName = "Planet Generation/PlanetSO", order = 0)]
public class PlanetSO : ScriptableObject
{
    public int seed=0;
    public SphereMeshSettings meshSettings;
    public BiomeCollectionSO biomeCollection;
    public BiomeClassifierSO biomeClassifier;

    public bool generateBioms = true;

    [Header("Bioms")]
    [SerializeField] public BiomeBlendType biomBlendType = BiomeBlendType.Discrete;
    [Header("Temperature Settings")]
    [Tooltip("Temperature equator")]
    [SerializeField, Range(0f, 1f)] public float equatorTemperature = 1.0f;

    [Tooltip("Temperature at the poles")]
    [SerializeField, Range(0f, 1f)] public float poleTemperature = 0.0f;

    [Tooltip("Scale of noise.")]
    [SerializeField] public float temperatureNoiseScale = 1.0f;

    [Tooltip("Strength of noise.")]
    [SerializeField] public float temperatureNoiseStrength = 0.2f;
}
