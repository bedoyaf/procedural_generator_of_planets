using System.Collections.Generic;
using UnityEngine;


public enum HeightType { Low, Medium, High, Mountain }
public enum TemperatureType { Cold, Temperate, Hot }


[CreateAssetMenu(fileName = "NewBiome", menuName = "Planet Generation/Biome")]
public class BiomeSO : ScriptableObject
{
    public string biomeName;
    public Texture2D biomeTexture;


    //   public Color tint;
    public List<HeightType> supportedHeights;
    public List<TemperatureType> supportedTemperatures;

    // Optional weights, for more nuanced matching
    [Range(0f, 1f)] public float heightAffinity = 1f;
    [Range(0f, 1f)] public float temperatureAffinity = 1f;

    public int priority = 1;
}
