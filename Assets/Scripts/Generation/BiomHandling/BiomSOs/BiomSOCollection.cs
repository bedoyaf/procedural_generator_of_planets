using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewBiomeCollection", menuName = "Planet Generation/Bime Collection")]
public class BiomeCollectionSO : ScriptableObject
{
    public List<BiomeSO> biomes;

}
