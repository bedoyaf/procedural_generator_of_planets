using System.Collections.Generic;
using UnityEngine;


public enum HeightType { Ocean, Low, Medium, High, Mountain }
public enum TemperatureType { Cold, Temperate, Hot }

public enum SlopeType { Flat, Steep, MildlySteep }

[CreateAssetMenu(fileName = "NewBiome", menuName = "Planet Generation/Biome")]
public class BiomeSO : ScriptableObject
{
    public string biomeName;
    //public Texture2D biomeTexture;
    [ColorUsage(true, true)]
    public Color biomeColor;

    public Texture2D biomeTexture;


    //   public Color tint;
    public List<HeightType> supportedHeights;
    public List<TemperatureType> supportedTemperatures;
    public List<SlopeType> supportedSlopes;

    // Optional weights, for more nuanced matching
    [Range(0f, 1f)] public float heightAffinity = 1f;
    [Range(0f, 1f)] public float slopeAffinity = 1f;
    [Range(0f, 1f)] public float temperatureAffinity = 1f;

    [Range(1f, 2f)] public float blenddistance = 1f;

    public int priority = 1;
}
