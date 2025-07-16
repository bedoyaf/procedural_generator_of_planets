using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that represents and stores the biom data, mainly the bioms requirements on position and texture
/// </summary>
[CreateAssetMenu(fileName = "NewBiome", menuName = "Planet Generation/Biome")]
public class BiomeSO : ScriptableObject
{
    public Texture2D biomeTexture;

    public List<BiomeAttributeHeightSO> supportedHeights;
    public List<BiomeAttributeTemperatures> supportedTemperatures;
    public List<BiomeAttributeSlopeSO> supportedSlopes;

    [Range(1f, 2f)] public float blendDistance = 1.1f;

    //represent the Attributes for faster calculations
    [HideInInspector] public ushort heightMask;
    [HideInInspector] public ushort tempMask;
    [HideInInspector] public ushort slopeMask;


}