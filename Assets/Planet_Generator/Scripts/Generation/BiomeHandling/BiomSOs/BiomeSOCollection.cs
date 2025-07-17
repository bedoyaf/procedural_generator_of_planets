using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SO that stores all the biomes to be used in the generation
/// </summary>
[CreateAssetMenu(fileName = "NewBiomeCollection", menuName = "Planet Generation/Bime Collection")]
public class BiomeCollectionSO : ScriptableObject
{
    public List<BiomeSO> biomes;

}
