using UnityEngine;


/// <summary>
/// Class for rangers in biome classifier
/// </summary>
[System.Serializable]
public struct FloatRange
{
    public float min, max;

    public bool Contains(float value) => value >= min && value <= max;

}

