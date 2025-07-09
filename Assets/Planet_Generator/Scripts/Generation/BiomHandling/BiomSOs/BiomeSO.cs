using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that represents and stores the biom data, mainly the bioms requirements on position and texture
/// </summary>
[CreateAssetMenu(fileName = "NewBiome", menuName = "Planet Generation/Biome")]
public class BiomeSO : ScriptableObject
{
//    public string biomeName;
    //public Texture2D biomeTexture;
//    [ColorUsage(true, true)]
//    public Color biomeColor;

    public Texture2D biomeTexture;

    public List<BiomeAttributeHeight> supportedHeights;
    public List<BiomeAttributeTemp> supportedTemperatures;
    public List<BiomeAttributeSlope> supportedSlopes;

    // Optional weights, for more nuanced matching
    [Range(0f, 1f)] public float heightAffinity = 1f;
    [Range(0f, 1f)] public float slopeAffinity = 1f;
    [Range(0f, 1f)] public float temperatureAffinity = 1f;

    [Range(1f, 2f)] public float blenddistance = 1f;

 //   public int priority = 1;


    [HideInInspector] public ushort heightMask;
    [HideInInspector] public ushort tempMask;
    [HideInInspector] public ushort slopeMask;


}