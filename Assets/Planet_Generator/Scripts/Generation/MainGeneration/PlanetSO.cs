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

    [Header("Bioms")]
    [SerializeField] public BiomeBlendType biomeBlendType = BiomeBlendType.Discrete;
    [SerializeField] public float TextureScale = 1.0f;
    [Header("Temperature Settings")]
    [Tooltip("Scale of noise.")]
    [SerializeField] public float temperatureNoiseScale = 1.0f;

    [Tooltip("Strength of noise.")]
    [SerializeField] public float temperatureNoiseStrength = 0.2f;

    [SerializeField] public bool hasWater;

    [SerializeField] public SphereMeshSettings waterSettings;

    [Range(0, 1)] public float waterIceLineStart = 0.82f;
    [Range(0, 1)] public float waterIceLineEnd = 0.87f;
    [SerializeField] public Color32 waterColor = new Color32(144,255,255,153);
    [SerializeField] public Color32 IceColor = new Color32(255, 255, 255, 255); 
}
