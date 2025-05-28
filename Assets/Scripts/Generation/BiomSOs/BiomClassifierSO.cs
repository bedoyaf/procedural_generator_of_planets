using UnityEngine;

[CreateAssetMenu(fileName = "BiomeClassifier", menuName = "Planet Generation/Biome Classifier")]
public class BiomeClassifierSO : ScriptableObject
{
    public FloatRange ocean;
    public FloatRange lowHeight;
    public FloatRange mediumHeight;
    public FloatRange highHeight;
    public FloatRange mountainHeight;

    public FloatRange coldTemp;
    public FloatRange temperateTemp;
    public FloatRange hotTemp;

    public FloatRange steepSlope;
    public FloatRange flatSlope;
    public FloatRange mildlySteepSlope;

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
}

[System.Serializable]
public struct FloatRange
{
    public float min, max;

    public bool Contains(float value) => value >= min && value <= max;

}
