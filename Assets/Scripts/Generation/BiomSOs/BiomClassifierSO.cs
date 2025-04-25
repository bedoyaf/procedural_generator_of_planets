using UnityEngine;

[CreateAssetMenu(fileName = "BiomeClassifier", menuName = "Planet Generation/Biome Classifier")]
public class BiomeClassifierSO : ScriptableObject
{
    public FloatRange lowHeight;
    public FloatRange mediumHeight;
    public FloatRange highHeight;
    public FloatRange mountainHeight;

    public FloatRange coldTemp;
    public FloatRange temperateTemp;
    public FloatRange hotTemp;

    public HeightType GetHeightType(float height)
    {
        if (mountainHeight.Contains(height)) return HeightType.Mountain;
        if (highHeight.Contains(height)) return HeightType.High;
        if (mediumHeight.Contains(height)) return HeightType.Medium;
        return HeightType.Low;
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
