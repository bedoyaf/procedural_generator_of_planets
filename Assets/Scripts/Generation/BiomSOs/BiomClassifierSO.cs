using UnityEngine;

[CreateAssetMenu(fileName = "BiomeClassifier", menuName = "Planet Generation/Biome Classifier")]
public class BiomeClassifierSO : ScriptableObject
{
    [Header("Height Types")]
    public FloatRange ocean;
    public FloatRange lowHeight;
    public FloatRange mediumHeight;
    public FloatRange highHeight;
    public FloatRange mountainHeight;

    [Header("Temperature Types")]
    public FloatRange coldTemp;
    public FloatRange temperateTemp;
    public FloatRange hotTemp;


    [Header("Slope Types")]
    public FloatRange steepSlope;
    public FloatRange mildlySteepSlope;
    public FloatRange flatSlope;

    public HeightType GetHeightType(float height)
    {
        if (mountainHeight.Contains(height))
        {
            return HeightType.Mountain;
        }
        if (highHeight.Contains(height)) { 
            return HeightType.High;
        }
        if (mediumHeight.Contains(height)) return HeightType.Medium;
        if (lowHeight.Contains(height)) return HeightType.Low;
        return HeightType.Mountain;
    }

    public (float,float) GetSlopeValues(SlopeType slopeType)
    {
        if (slopeType == SlopeType.Steep)
        {
            return (steepSlope.min,steepSlope.max);
        }
        if (slopeType == SlopeType.Flat)
        {
            return (flatSlope.min, flatSlope.max);
        }
        if (slopeType == SlopeType.MildlySteep)
        {
            return (mediumHeight.min, mildlySteepSlope.max);
        }
        return (-1, -1);
    }

    public TemperatureType GetTemperatureType(float temp)
    {
        if (coldTemp.Contains(temp)) return TemperatureType.Cold;
        if (temperateTemp.Contains(temp)) return TemperatureType.Temperate;
        return TemperatureType.Hot;
    }

    public float GetTypeCenter(HeightType type)
    {
        return type switch
        {
            HeightType.Ocean => (ocean.min + ocean.max) * 0.5f,
            HeightType.Low => (lowHeight.min + lowHeight.max) * 0.5f,
            HeightType.Medium => (mediumHeight.min + mediumHeight.max) * 0.5f,
            HeightType.High => (highHeight.min + highHeight.max) * 0.5f,
            HeightType.Mountain => (mountainHeight.min + mountainHeight.max) * 0.5f,
            _ => 0f
        };
    }

    public float GetTypeCenter(TemperatureType type)
    {
        return type switch
        {
            TemperatureType.Cold => (coldTemp.min + coldTemp.max) * 0.5f,
            TemperatureType.Temperate => (temperateTemp.min + temperateTemp.max) * 0.5f,
            TemperatureType.Hot => (hotTemp.min + hotTemp.max) * 0.5f,
            _ => 0f
        };
    }

    public float GetTypeRange(HeightType type)
    {
        return type switch
        {
            HeightType.Ocean => (ocean.max - ocean.min) * 0.5f,
            HeightType.Low => (lowHeight.max - lowHeight.min) * 0.5f,
            HeightType.Medium => (mediumHeight.max - mediumHeight.min) * 0.5f,
            HeightType.High => (highHeight.max - highHeight.min) * 0.5f,
            HeightType.Mountain => (mountainHeight.max - mountainHeight.min) * 0.5f,
            _ => 1f
        };
    }

    public float GetTypeRange(TemperatureType type)
    {
        return type switch
        {
            TemperatureType.Cold => (coldTemp.max - coldTemp.min) * 0.5f,
            TemperatureType.Temperate => (temperateTemp.max - temperateTemp.min) * 0.5f,
            TemperatureType.Hot => (hotTemp.max - hotTemp.min) * 0.5f,
            _ => 1f
        };
    }
}

[System.Serializable]
public struct FloatRange
{
    public float min, max;

    public bool Contains(float value) => value >= min && value <= max;

}
