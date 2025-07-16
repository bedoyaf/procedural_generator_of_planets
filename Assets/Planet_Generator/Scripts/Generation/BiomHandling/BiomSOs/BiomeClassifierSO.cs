using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// Scriptable object that defines values for Biome parameters
/// </summary>
[CreateAssetMenu(fileName = "BiomeClassifier", menuName = "Planet Generation/Biome Classifier")]
public class BiomeClassifierSO : ScriptableObject
{
    [Header("Height Types")]

   [SerializeField] public List<BiomeAttributeHeightSO> heights;
    [SerializeField] public List<BiomeAttributeTemperatures>  temperatures;
    [SerializeField] public List<BiomeAttributeSlopeSO> slopes;

    [SerializeField] public List<FloatRange> heightRanges = new();
    [SerializeField] public List<FloatRange> temperaturesRanges = new();
    [SerializeField] public List<FloatRange> slopeRanges = new();


    /// <summary>
    /// Simple function to  get the types center
    /// </summary>
    /// <param name="type">the type to identify the center</param>
    /// <returns>center value of the current attribute</returns>
    public float GetTypeCenter(BiomeAttributeHeightSO type)
    {
        int index = heights.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }    
        return (heightRanges[index].max + heightRanges[index].min) * 0.5f;
    }

    /// <summary>
    /// Simple function to  get the types center
    /// </summary>
    /// <param name="type">the type to identify the center</param>
    /// <returns>center value of the current attribute</returns>
    public float GetTypeCenter(BiomeAttributeTemperatures type)
    {
        int index = temperatures.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (temperaturesRanges[index].max + temperaturesRanges[index].min) * 0.5f;
    }

    /// <summary>
    /// Simple function to  get the types center
    /// </summary>
    /// <param name="type">the type to identify the center</param>
    /// <returns>center value of the current attribute</returns>
    public float GetTypeCenter(BiomeAttributeSlopeSO type)
    {
        int index = slopes.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (slopeRanges[index].max + slopeRanges[index].min) * 0.5f;
    }

    /// <summary>
    /// Simple function to  get the types range
    /// </summary>
    /// <param name="type">the type to identify the range</param>
    /// <returns>range value of the current attribute</returns>
    public float GetTypeRange(BiomeAttributeHeightSO type)
    {
        int index = heights.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (heightRanges[index].max - heightRanges[index].min) * 0.5f;
    }

    /// <summary>
    /// Simple function to  get the types range
    /// </summary>
    /// <param name="type">the type to identify the range</param>
    /// <returns>range value of the current attribute</returns>
    public float GetTypeRange(BiomeAttributeTemperatures type)
    {
        int index = temperatures.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (temperaturesRanges[index].max - temperaturesRanges[index].min) * 0.5f;
    }

    /// <summary>
    /// Simple function to  get the types range
    /// </summary>
    /// <param name="type">the type to identify the range</param>
    /// <returns>range value of the current attribute</returns>
    public float GetTypeRange(BiomeAttributeSlopeSO type)
    {
        int index = slopes.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (slopeRanges[index].max - slopeRanges[index].min) * 0.5f;
    }

    /// <summary>
    /// checks in what interval lies the value that corresponds to the correct type
    /// </summary>
    /// <param name="height">the value that we check</param>
    /// <returns>the corresponding type</returns>
    public BiomeAttributeHeightSO GetHeightType(float height)
    {
        for (int i = 0;i< heightRanges.Count;i++)
        {
            if (heightRanges[i].Contains(height)) return heights[i];
        }
        return heights[0];
    }

    /// <summary>
    /// checks in what interval lies the value that corresponds to the correct type
    /// </summary>
    /// <param name="temp">the value that we check</param>
    /// <returns>the corresponding type</returns>
    public BiomeAttributeTemperatures GetTempType(float temp)
    {
        for (int i = 0; i < temperaturesRanges.Count;i++)
        {
            if (temperaturesRanges[i].Contains(temp)) return temperatures[i];
        }
        return temperatures[0];
    }

    /// <summary>
    /// checks in what interval lies the value that corresponds to the correct type
    /// </summary>
    /// <param name="slope">the value that we check</param>
    /// <returns>the corresponding type</returns>
    public BiomeAttributeSlopeSO GetSlopeType(float slope)
    {
        for (int i = 0; i < slopeRanges.Count;i++)
        {
            if (slopeRanges[i].Contains(slope)) return slopes[i];
        }
        return slopes[0];
    }

    public int GetAttributeIndex(BiomeAttributeHeightSO attribute)
    {
        return heights.IndexOf(attribute);
    }

    public int GetAttributeIndex(BiomeAttributeTemperatures attribute)
    {
        return temperatures.IndexOf(attribute);
    }

    public int GetAttributeIndex(BiomeAttributeSlopeSO attribute)
    {
        return slopes.IndexOf(attribute);
    }


}
