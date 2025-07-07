using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "BiomeClassifier", menuName = "Planet Generation/Biome Classifier")]
public class BiomeClassifierSO : ScriptableObject
{
    [Header("Height Types")]

   [SerializeField] public List<BiomAttributeHeight> heights;
    [SerializeField] public List<BiomAttributeTemp>  temperatures;
    [SerializeField] public List<BiomAttributeSlope> slopes;

 //   public Dictionary<BiomAttributeHeight, FloatRange> heightDict;
 //   public Dictionary<BiomAttributeSlope, FloatRange> slopeDict;
 //   public Dictionary<BiomAttributeTemp, FloatRange> tempDict;


    [SerializeField] public List<FloatRange> heightRanges = new();
    [SerializeField] public List<FloatRange> temperaturesRanges = new();
    [SerializeField] public List<FloatRange> slopeRanges = new();


    public void Awake()
    {
        /*  heightDict = new Dictionary< BiomAttributeHeight, FloatRange>();
          foreach (var attr in heights)
          {
              if (!heightDict.ContainsKey(attr))
                  heightDict.Add(attr, new FloatRange());
              else Debug.LogError($"Biom attribute name {attr.name} already stored");
          }

          tempDict = new Dictionary<BiomAttributeTemp, FloatRange>();
          foreach (var attr in temperatures)
          {
              if (!tempDict.ContainsKey(attr))
                  tempDict.Add(attr, new FloatRange());
              else Debug.LogError($"Biom attribute name {attr.name} already stored");
          }

          slopeDict = new Dictionary< BiomAttributeSlope, FloatRange>();
          foreach (var attr in slopes)
          {
              if (!slopeDict.ContainsKey(attr))
                  slopeDict.Add(attr, new FloatRange());
              else Debug.LogError($"Biom attribute name {attr.name} already stored");
          }*/
        if (heights.Count > 0) PopulateLists(heights, heightRanges, "height");
        if (temperatures.Count > 0) PopulateLists(temperatures, temperaturesRanges, "temperature");
        if (slopes.Count > 0) PopulateLists(slopes, slopeRanges, "slope");
    }

    private void OnValidate()
    {
        PopulateLists(heights, heightRanges, "height");
        PopulateLists(temperatures, temperaturesRanges, "temperature");
        PopulateLists(slopes, slopeRanges, "slope");
    }

    private void PopulateLists<TAttribute>(List<TAttribute> primaryList, List<FloatRange> rangeList, string typeName) where TAttribute : BiomAtrributeAbstract
    {


        for (int i = 0; i < primaryList.Count; i++)
        {
            if (primaryList[i] == null)
            {
                Debug.LogWarning($"Null {typeName} attribute at index {i}. Skipping.");
                continue;
            }

            FloatRange range = (i < rangeList.Count) ? rangeList[i] : new FloatRange();

        }
    }


    public float GetTypeCenter(BiomAttributeHeight type)
    {
        int index = heights.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }    
        return (heightRanges[index].max + heightRanges[index].min) * 0.5f;
    }

    public float GetTypeCenter(BiomAttributeTemp type)
    {
        int index = temperatures.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (temperaturesRanges[index].max + temperaturesRanges[index].min) * 0.5f;
    }
    public float GetTypeCenter(BiomAttributeSlope type)
    {
        int index = slopes.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (slopeRanges[index].max + slopeRanges[index].min) * 0.5f;
    }

    public float GetTypeRange(BiomAttributeHeight type)
    {
        int index = heights.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (heightRanges[index].max - heightRanges[index].min) * 0.5f;
    }

    public float GetTypeRange(BiomAttributeTemp type)
    {
        int index = temperatures.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (temperaturesRanges[index].max - temperaturesRanges[index].min) * 0.5f;
    }
    public float GetTypeRange(BiomAttributeSlope type)
    {
        int index = slopes.IndexOf(type);
        if (index == -1)
        {
            Debug.LogError("Invalid BiomAttribute in biome");
        }
        return (slopeRanges[index].max - slopeRanges[index].min) * 0.5f;
    }

    public BiomAttributeHeight GetHeightType(float height)
    {
        for (int i = 0;i< heightRanges.Count;i++)
        {
            if (heightRanges[i].max > height && heightRanges[i].min < height) return heights[i];
        }
        return heights[0];
    }

    public BiomAttributeTemp GetTempType(float temp)
    {
        for (int i = 0; i < temperaturesRanges.Count;i++)
        {
            if (temperaturesRanges[i].max > temp && heightRanges[i].min < temp) return temperatures[i];
        }
        return temperatures[0];
    }

    public BiomAttributeSlope GetSlopeType(float slope)
    {
        for (int i = 0; i < slopeRanges.Count;i++)
        {
            if (slopeRanges[i].max > slope && slopeRanges[i].min < slope) return slopes[i];
        }
        return slopes[0];
    }

    public int GetAttributeIndex(BiomAttributeHeight attribute)
    {
        return heights.IndexOf(attribute);
    }

    public int GetAttributeIndex(BiomAttributeTemp attribute)
    {
        return temperatures.IndexOf(attribute);
    }

    public int GetAttributeIndex(BiomAttributeSlope attribute)
    {
        return slopes.IndexOf(attribute);
    }


}

[System.Serializable]
public struct FloatRange
{
    public float min, max;

    public bool Contains(float value) => value >= min && value <= max;

}

