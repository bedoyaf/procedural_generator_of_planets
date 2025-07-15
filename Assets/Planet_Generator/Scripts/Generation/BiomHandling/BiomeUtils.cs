using Unity.Collections;
using UnityEngine;
using Unity.Mathematics.Geometry;
using Unity.Mathematics;

/// <summary>
/// Helper static class containing helper functions
/// </summary>
public static class BiomeUtils
{
    /// <summary>
    /// Calculates the angle of the triangle
    /// </summary>
    /// <param name="normal">normal of the vertex</param>
    /// <param name="localUP">the vector from center to the current vertex representing the up on the planet</param>
    /// <returns>the angle</returns>
    public static float CalculateSlopeFromNormal(Vector3 normal, Vector3 localUP)
    {
        float slopeCos = math.dot(normal, localUP);
        float slopeDeg = math.acos(slopeCos) * Mathf.Rad2Deg;
        return slopeDeg;
    }

    /// <summary>
    /// Generates a texture array to be used in the material
    /// </summary>
    /// <param name="biomeCollection">the biomes to be used</param>
    /// <returns>the texture2Darray</returns>
    public static Texture2DArray GenerateBiomeTextureArray(BiomeCollectionSO biomeCollection)
    {
        int textureSize = 512; // adjust based on your source textures
        Texture2DArray texArray = new Texture2DArray(textureSize, textureSize, biomeCollection.biomes.Count, TextureFormat.RGBA32, true);
        texArray.wrapMode = TextureWrapMode.Repeat;
        texArray.filterMode = FilterMode.Bilinear;

        for (int i = 0; i < biomeCollection.biomes.Count; i++)
        {
            Texture2D source = biomeCollection.biomes[i].biomeTexture;

            RenderTexture rt = RenderTexture.GetTemporary(textureSize, textureSize);
            Graphics.Blit(source, rt);

            RenderTexture.active = rt;
            Texture2D readableTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            readableTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            texArray.SetPixels(readableTex.GetPixels(), i);
        }

        texArray.Apply();
        return texArray;
    }

    /// <summary>
    /// creates a mask representing the types, each type is signified as a 1 or 0 in the number, then we can just use logic to determin the calculations
    /// </summary>
    /// <param name="biome">the biome to be used</param>
    /// <param name="biomeClassifier">biomeclassifier to get the values</param>
    /// <returns>the bit mask</returns>
    private static ushort CreateBiomesAttributeMaskHeight(BiomeSO biome, BiomeClassifierSO biomeClassifier)
    {
        ushort mask = 0;
        foreach (var attribute in biome.supportedHeights)
        {
            int index = biomeClassifier.GetAttributeIndex(attribute);
            mask |= (ushort)(1 << index);
        }
        return mask;
    }

    /// <summary>
    /// creates a mask representing the types, each type is signified as a 1 or 0 in the number, then we can just use logic to determin the calculations
    /// </summary>
    /// <param name="biome">the biome to be used</param>
    /// <param name="biomeClassifier">biomeclassifier to get the values</param>
    /// <returns>the bit mask</returns>
    private static  ushort CreateBiomesAttributeMaskTemperature(BiomeSO biome, BiomeClassifierSO biomeClassifier)
    {
        ushort mask = 0;
        foreach (var attribute in biome.supportedTemperatures)
        {
            int index = biomeClassifier.GetAttributeIndex(attribute);
            mask |= (ushort)(1 << index);
        }
        return mask;
    }

    /// <summary>
    /// creates a mask representing the types, each type is signified as a 1 or 0 in the number, then we can just use logic to determin the calculations
    /// </summary>
    /// <param name="biome">the biome to be used</param>
    /// <param name="biomeClassifier">biomeclassifier to get the values</param>
    /// <returns>the bit mask</returns>
    private static uint CreateBiomesAttributeMaskSlope(BiomeSO biome, BiomeClassifierSO biomeClassifier)
    {
        uint mask = 0;
        foreach (var attribute in biome.supportedSlopes)
        {
            int index = biomeClassifier.GetAttributeIndex(attribute);
            mask |= (uint)(1 << index);
        }
        return mask;
    }

    /// <summary>
    /// Converts BiomeClassifierSO to struct so its not a reference type, to be used in the paralel calculations
    /// </summary>
    /// <param name="classifier">the classifier</param>
    /// <returns>value type of the BiomeClassifierSO</returns>
    public static BiomeClassifierData ConvertClassifierToData(BiomeClassifierSO classifier)
    {
        var data = new BiomeClassifierData
        {
            heightRanges = new NativeArray<FloatRange>(classifier.heightRanges.ToArray(), Allocator.TempJob),
            tempRanges = new NativeArray<FloatRange>(classifier.temperaturesRanges.ToArray(), Allocator.TempJob),
            slopeRanges = new NativeArray<FloatRange>(classifier.slopeRanges.ToArray(), Allocator.TempJob),
        };
        return data;
    }

    /// <summary>
    /// Value type to store the BiomeClassifier to be used in the paralel calculations
    /// </summary>
    public struct BiomeClassifierData
    {
        public NativeArray<FloatRange> heightRanges;
        public NativeArray<FloatRange> tempRanges;
        public NativeArray<FloatRange> slopeRanges;
    }

    /// <summary>
    /// Biome as a value type to be used in the paralel calculations
    /// </summary>
    public struct BiomeData
    {
        public uint heightMask;  
        public uint tempMask;    
        public uint slopeMask;   
    }

    /// <summary>
    /// Takes all the biomes and converts(just stores relevant values) them into a value type to be used in paralel calculations
    /// </summary>
    /// <param name="biomeCollection">list of biomes to be stored</param>
    /// <param name="biomeClassifier">the classifier to help</param>
    /// <returns>biome data in value type so can be used in paralel calculations</returns>
    public static NativeArray<BiomeData> PrepareBiomes(BiomeCollectionSO biomeCollection, BiomeClassifierSO biomeClassifier)
    {
        NativeArray<BiomeData> arr = new NativeArray<BiomeData>(biomeCollection.biomes.Count, Allocator.TempJob);
        for (int i = 0; i < biomeCollection.biomes.Count; i++)
        {
            var biome = biomeCollection.biomes[i];
            arr[i] = new BiomeData
            {
                heightMask = CreateBiomesAttributeMaskHeight(biome,biomeClassifier),
                tempMask = CreateBiomesAttributeMaskTemperature(biome,biomeClassifier),
                slopeMask = CreateBiomesAttributeMaskSlope(biome, biomeClassifier)
            };
        }
        return arr;
    }


    public static bool AreBiomesValid(BiomeCollectionSO biomeCollectionSO, BiomeClassifierSO biomeClassifier)
    {
        if (!AreBiomeTexturesSet(biomeCollectionSO)) return false;
        if (!AreSupportedBiomeAttributesValid(biomeCollectionSO, biomeClassifier)) return false;
        return true;
    }

    private static bool AreSupportedBiomeAttributesValid(BiomeCollectionSO biomeCollectionSO, BiomeClassifierSO biomeClassifier)
    {
        foreach (var biome in biomeCollectionSO.biomes)
        {
            foreach (var att in biome.supportedHeights)
            {
                if (!biomeClassifier.heights.Contains(att))
                {
                    Debug.Log($"Invald height attribute {att} in biome {biome}");
                    return false;
                }
            }
            foreach (var att in biome.supportedSlopes)
            {
                if (!biomeClassifier.slopes.Contains(att))
                {
                    Debug.Log($"Invald slope attribute {att} in biome {biome}");
                    return false;
                }
            }
            foreach (var att in biome.supportedTemperatures)
            {
                if (!biomeClassifier.temperatures.Contains(att))
                {
                    Debug.Log($"Invald temperature attribute {att} in biome {biome}");
                    return false;
                }
            }
        }
        return true;
    }

    private static bool AreBiomeTexturesSet(BiomeCollectionSO biomeCollection)
    {
        foreach(var biome in biomeCollection.biomes)
        {
            if(biome == null)
            {
                Debug.Log($"Biom missing in collection");
                return false;
            }
            if (biome.biomeTexture == null)
            {
                Debug.Log($"Biome {biome} missing texture" );
                return false;
            }
        }
        return true;
    }
}
