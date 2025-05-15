using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;
using System.Linq;
using UnityEngine.UIElements;
using System.Drawing;
using Unity.Mathematics.Geometry;

public class BiomPipeline : MonoBehaviour
{
    private ComputeBuffer biomeIndexBuffer;
    [Header("Slopes")]
    [SerializeField, Range(0f, 90f)] private float slopeThreshold = 30f;
    [Header("Temperature Settings")]
    [Tooltip("Temperature equator")]
    [SerializeField, Range(0f, 1f)] private float equatorTemperature = 1.0f;

    [Tooltip("Temperature at the poles")]
    [SerializeField, Range(0f, 1f)] private float poleTemperature = 0.0f;

    [Tooltip("Scale of noise.")]
    [SerializeField] private float temperatureNoiseScale = 1.0f;

    [Tooltip("Strength of noise.")]
    [SerializeField] private float temperatureNoiseStrength = 0.2f;



    public float falloff = 1f;

    public float smoothnessExponent = 1;
    public float blenddistance = 1;


    [Header("References")]
    [SerializeField] private Material material;

    [SerializeField] private BiomeCollectionSO biomeCollection;
    [SerializeField] private BiomeClassifierSO biomeClassifier;

   // [SerializeField] private float minHeight=0f;
  //  [SerializeField] private float maxHeight=1f;

    private Vector3[] baseVertices;

    private float[] heights;

    private float sphereRadius;

    private Mesh mesh;

    private MeshFilter meshFilter;

    private MeshRenderer meshRenderer;



    public void Initialize(MeshRenderer meshRenderer, MeshFilter meshFilter)
    {
        meshRenderer.material = material;
        mesh = meshFilter.mesh;
        this.meshRenderer = meshRenderer;
        this.meshFilter = meshFilter;


    }


    public void UpdateBiomPipeline(float radius, float[] heights)
    {
        this.sphereRadius = radius;
        this.heights = heights;
    }

    public void UpdateBiomPipelineMesh(Mesh mesh)
    {
        this.mesh = mesh;
    }

    private ComputeBuffer heightBuffer;
   // private ComputeBuffer positionBuffer;

    public void ApplyTexturesToMesh(Vector3[] vertices, Vector3[] normals)
    {
     //   positionBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3); // Stride for Vector3
  /*      ComputeBuffer heightBuffer = new ComputeBuffer(vertices.Length, sizeof(float));
        heightBuffer.SetData(heights);
        material.SetBuffer("_VertexHeights", heightBuffer);
        material.SetInt("_VertexCount", vertices.Length);*/

       // material.SetBuffer("vertices", positionBuffer);
      //  material.SetBuffer( "heights", heightBuffer);
     //   material.SetInt("numVertices", vertices.Length);

      /*  Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(i, 0); // index encoded in .x
        }
        mesh.uv = uvs;


        Texture2D gradientTex = GenerateGradientTexture(biomeGradient, 300); //resolution idk
        gradientTex.wrapMode = TextureWrapMode.Clamp;
        gradientTex.filterMode = FilterMode.Bilinear;

     //   material.SetFloat("_GradientMin", minHeight);
    //    material.SetFloat("_GradientMax", maxHeight);
        material.SetTexture("_BiomeGradient", gradientTex);*/
   



        baseVertices = vertices;

         int numVertices = baseVertices.Length;
        // int[] biomeIndices = new int[numVertices];


         float minHeight2 = float.MaxValue;
         float maxHeight2 = float.MinValue;

         for (int i = 0; i < heights.Length; i++)
         {
             float h = heights[i];

         //    if(i%1000==0)Debug.Log(heights[i]);

             if (h < minHeight2) minHeight2 = h;
             if (h > maxHeight2) maxHeight2 = h;
         }
         Debug.Log("highest point is " + maxHeight2);
         Debug.Log("lowest point is " + minHeight2);

        /*  ComputeBuffer heightBuffer = new ComputeBuffer(vertices.Length, sizeof(float));
          material.SetInt("_VertexCount", heights.Length);
          heightBuffer.SetData(heights);
          material.SetBuffer("_VertexHeights", heightBuffer);*/

        //         material.SetFloat("_BaseRadius", minHeight2);
        //       material.SetFloat("_MaxHeight", maxHeight2);





        //   Color[] vertexColors = new Color[numVertices];
        /*    List<Vector4> biomeUVs = new List<Vector4>(numVertices);
            for (int i = 0; i < numVertices; i++)
            {


                 float temp = CalculateTemperature(baseVertices[i]);
                 biomeIndices[i] = FindBiomeIndex(heights[i], temp, normals[i], vertices[i]);

                 int biomeIndex = biomeIndices[i]; // already assigned per vertex
                                                   //   BiomeSO biome = biomeCollection.biomes[biomeIndex];
                                                   //  vertexColors[i] = biome.biomeColor;
                biomeUVs.Add(new Vector4((float)biomeIndices[i], 0f, 0f, 0f));
            }
            //   mesh.colors = vertexColors;

            mesh.SetUVs(2, biomeUVs);
            Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
            material.SetTexture("_BiomeTexArray", biomTexArray);

            */

        /*
        List<Vector4> biomeDataUVs = new(numVertices);

        for (int i = 0; i < numVertices; i++)
        {
            float temp = CalculateTemperature(baseVertices[i]);
            float height = heights[i];
            Vector3 normal = normals[i];

            BiomeData data = FindBiomeData(height, temp, vertices[i], normals[i]); // You implement this

            if (i % 1000 == 0) Debug.Log("p: "+data.primaryBiome+" s: "+ data.secondaryBiome +" blend: "+data.blendFactor);

            float primary = data.primaryBiome;
            float secondary = (data.secondaryBiome >= 0) ? data.secondaryBiome : -1f;
            float blend = Mathf.Clamp01(data.blendFactor);


            biomeDataUVs.Add(new Vector4(primary, -1f, 0f, 0f));
            //biomeDataUVs.Add(new Vector4(primary, secondary, blend, 0f)); // packed into UV2
        }

        mesh.SetUVs(2, biomeDataUVs);
        material.SetTexture("_BiomeTexArray", GenerateBiomeTextureArray(biomeCollection));
        material.SetInt("_BiomeTexArray_Depth", biomeCollection.biomes.Count);
        */

        /*     List<Vector4> biomeUVs = new List<Vector4>(numVertices);

             for (int i = 0; i < numVertices; i++)
             {
                 float height = heights[i];
                 float temp = CalculateTemperature(baseVertices[i]);
                 Vector3 normal = normals[i];
                 Vector3 vertex = vertices[i];

                 int primary = FindBiomeIndex(height, temp, normal, vertex);
                 int secondary = primary;
                 float blendScore = 0f;

                 // Sample offsets in a spherical pattern
                 Vector3[] offsets = new Vector3[]
                 {
                     new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
                     new Vector3(0, 1, 0), new Vector3(0, -1, 0),
                     new Vector3(0, 0, 1), new Vector3(0, 0, -1)
                 };

                 float[] scales = new float[] { 0.01f, 0.007f, 0.004f }; // decreasing deltas

                 foreach (float scale in scales)
                 {
                         float height2 = height + scale; // you might want to sample from height map if available
                         Vector3 approxNormal = normal; // could estimate from nearby noise if needed

                         int checkBiome = FindBiomeIndex(height2, temp, approxNormal, vertex);

                         if (checkBiome != primary)
                         {
                             secondary = checkBiome;
                             blendScore += 1f;
                         }
                 }

                 float blend = Mathf.Clamp01(blendScore / (offsets.Length * scales.Length));
           //      if (i % 1000 == 0 && blend > 0) Debug.Log($"Blend at {i}: {blend}");

                 biomeUVs.Add(new Vector4(primary, secondary, blend, 0f));
             }

             mesh.SetUVs(2, biomeUVs);

             Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
             material.SetTexture("_BiomeTexArray", biomTexArray);
        */

        /*   List<Vector4> biomeUVs = new List<Vector4>(numVertices);

           for (int i = 0; i < numVertices; i++)
           {
               float height = heights[i];
               float temp = CalculateTemperature(baseVertices[i]);
               Vector3 normal = normals[i];
               Vector3 vertex = vertices[i];

               int primary = FindBiomeIndex(height, temp, normal, vertex);
               int secondary = primary;
               float blendScore = 0f;

               // Sample offsets in a spherical pattern
               Vector3[] offsets = new Vector3[]
               {
                        new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
                        new Vector3(0, 1, 0), new Vector3(0, -1, 0),
                        new Vector3(0, 0, 1), new Vector3(0, 0, -1)
               };

               float[] scales = new float[] { 0.01f, 0.007f,0.004f, 0.001f, 0.0007f, 0.0001f }; // decreasing deltas

               foreach (float scale in scales)
               {
                   float height2 = height + scale; // you might want to sample from height map if available
                   Vector3 approxNormal = normal; // could estimate from nearby noise if needed

                   int checkBiome = FindBiomeIndex(height2, temp, approxNormal, vertex);

                   if (checkBiome != primary)
                   {
                       secondary = checkBiome;
                       blendScore += 1f;
                   }
               }

               float blend = Mathf.Clamp01(blendScore / (scales.Length));
               //      if (i % 1000 == 0 && blend > 0) Debug.Log($"Blend at {i}: {blend}");

               biomeUVs.Add(new Vector4(primary, secondary, blend, 0f));
           }

           mesh.SetUVs(2, biomeUVs);

           UnityEngine.Color[] biomeColors = biomeCollection.biomes.Select(b => b.biomeColor).ToArray();

           ComputeBuffer colorBuffer = new ComputeBuffer(biomeColors.Length, sizeof(float) * 4);
           colorBuffer.SetData(biomeColors);


           material.SetBuffer("_BiomeColors", colorBuffer);

           */

        var biomeIndices = new List<Vector4>(vertices.Length);
        var biomeWeights = new List<Vector4>(vertices.Length);

        for (int i = 0; i < vertices.Length; i++)
        {
            float height = heights[i];
          //  float temp = temperatures[i];

            HeightType heightType = biomeClassifier.GetHeightType(height);
         //   TemperatureType tempType = biomeClassifier.GetTemperatureType(temp);

            var scores = new List<(int biomeIndex, float score)>();

            for (int j = 0; j < biomeCollection.biomes.Count; j++)
            {
                var biome = biomeCollection.biomes[j];

                bool supportsHeight = biome.supportedHeights.Contains(heightType);
                //   bool supportsTemp = biome.supportedTemperatures.Contains(tempType);

                //      if (!supportsHeight /*|| !supportsTemp*/)
                //       continue;

                float center = GetTypeCenter(biome.supportedHeights[0]);
                float range = GetTypeRange(biome.supportedHeights[0]);
                float dist = Mathf.Abs(height - center);

                // Falloff edge sharpness (controls blend zone smoothness)
                float t = Mathf.Clamp01(dist / (range * blenddistance));
                float score = 1f - t * t * (3f - 2f * t);

                scores.Add((j, score));
            }

            scores.Sort((a, b) => b.score.CompareTo(a.score));

            int indexA = scores.Count > 0 ? scores[0].biomeIndex : 0;
            int indexB = scores.Count > 1 ? scores[1].biomeIndex : indexA;

            float weightA = scores.Count > 0 ? scores[0].score : 1f;
            float weightB = scores.Count > 1 ? scores[1].score : 0f;

            float total = weightA + weightB + 1e-5f;
            //    if (i % 1000 == 0) Debug.Log("skibidi: "+weightA);

            if (i % 1000 == 1) Debug.Log("index: " + indexA + " weight " + weightA + " indexB: " + indexB + "weight " + weightB);

            biomeIndices.Add(new Vector4(indexA, indexB, 0, 0));
            biomeWeights.Add(new Vector4(weightA / total, weightB / total, 0, 0));
        }
        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomTexArray);
        mesh.SetUVs(2, biomeIndices);
        mesh.SetUVs(3, biomeWeights);


        meshRenderer.sharedMaterial = material;
    }

    public float delta = 0.005f;


    private float CalculateTemperature(Vector3 worldPosition)
    {
        // Normalize to unit sphere
        Vector3 normalized = worldPosition.normalized;

        // Latitude: -1 at south pole, 0 at equator, 1 at north pole
        float latitude = normalized.y;

        //Debug.Log(latitude);

        // Map to 0..1: poles = poleTemperature, equator = equatorTemperature
        float baseTemp = 0;

        if (latitude < 0f) baseTemp = Mathf.Lerp(equatorTemperature, poleTemperature, -latitude);
        else if (latitude >= 0f) baseTemp = Mathf.Lerp(equatorTemperature, poleTemperature, latitude);

        // Longitude/Latitude for noise coordinates
        float longitude = Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI); // -0.5 to 0.5
        float lat = Mathf.Asin(normalized.y) / Mathf.PI + 0.5f; // 0 to 1

        // Scale and wrap
        float u = longitude * temperatureNoiseScale;
        float v = lat * temperatureNoiseScale;

        // Use spherical UVs as noise coordinates
        float noise = Mathf.PerlinNoise(u, v);

        float finalTemp = baseTemp + (noise - 0.5f) * 2f * temperatureNoiseStrength;

        return Mathf.Clamp01(finalTemp);
        //    return baseTemp;
    }

    private int FindBiomeIndex(float height, float temperature, Vector3 normal, Vector3 vertex)
    {
        float slope = CalculateSlopeFromNormal(normal, vertex);

        if (slope > slopeThreshold)
            return 0;




        var heightType = biomeClassifier.GetHeightType(height);//pridal sphereRadius 
        var tempType = biomeClassifier.GetTemperatureType(temperature);

        // Find index directly using FindIndex for elegance
        var possibleBiomes = biomeCollection.biomes
            .Select((b, index) => new { Biome = b, Index = index })
            .Where(x =>
                x.Biome.supportedHeights.Contains(heightType) &&
                x.Biome.supportedTemperatures.Contains(tempType)
            )
            .OrderByDescending(x => x.Biome.priority) // highest priority wins
            .ToList();

        // Fallback to first biome if none match
        return possibleBiomes.Count > 0 ? possibleBiomes[0].Index : 0;
    }

    float CalculateSlopeFromNormal(Vector3 normal, Vector3 localUP)
    {
        // The slope angle in radians (dot product between the normal and the up direction)
        float slopeAngle = Vector3.Angle(normal, localUP);

        // You can then convert it to a range of your choice, for example, degrees or as a factor
        return slopeAngle; // Or transform it to a desired scale
    }



    [SerializeField] private Gradient biomeGradient;
 //   [SerializeField, Range(16, 1024)] private int resolution = 256;


    private Texture2D GenerateGradientTexture(Gradient gradient, int width)
    {
        Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            tex.SetPixel(x, 0, gradient.Evaluate(t));
        }

        tex.Apply();
        return tex;
    }



    private Texture2DArray GenerateBiomeTextureArray(BiomeCollectionSO biomeCollection)
    {
        int textureSize = 512; // adjust based on your source textures
        Texture2DArray texArray = new Texture2DArray(textureSize, textureSize, biomeCollection.biomes.Count, TextureFormat.RGBA32, true);
        texArray.wrapMode = TextureWrapMode.Repeat;
        texArray.filterMode = FilterMode.Bilinear;

        for (int i = 0; i < biomeCollection.biomes.Count; i++)
        {
            Texture2D source = biomeCollection.biomes[i].biomeTexture;

            // Create resized readable texture
            RenderTexture rt = RenderTexture.GetTemporary(textureSize, textureSize);
            Graphics.Blit(source, rt);

            RenderTexture.active = rt;
            Texture2D readableTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            readableTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            // Copy into the texture array
            texArray.SetPixels(readableTex.GetPixels(), i);
        }

        texArray.Apply();
        return texArray;
    }




    public struct BiomeData
    {
        public int primaryBiome;
        public int secondaryBiome; // -1 if no blend
        public float blendFactor;  // 0 = hard, 1 = full blend (only near edges)
    }
    public BiomeData GetBlendedBiome(
    float height,
    float temp)
    {
        BiomeSO primary = FindBiome(height, temp);

        // Check in 4 directions: height+/-, temp+/-
        BiomeSO heightUp = FindBiome(height + delta, temp);
        BiomeSO heightDown = FindBiome(height - delta, temp);
 //       BiomeSO tempUp = FindBiome(height, temp + delta);
 //       BiomeSO tempDown = FindBiome(height, temp - delta);

        BiomeSO[] neighbors = { heightUp, heightDown/*, tempUp, tempDown*/ };

        foreach (var neighbor in neighbors)
        {
            if (neighbor != null && neighbor != primary)
            {
                float blendFactor = 1.0f; // or use distance to center of threshold if you want smoothing
                return new BiomeData
                {
                    primaryBiome = biomeCollection.biomes.IndexOf(primary),
                    secondaryBiome = biomeCollection.biomes.IndexOf(neighbor),
                    blendFactor = blendFactor
                };
            }
        }

        return new BiomeData
        {
            primaryBiome = biomeCollection.biomes.IndexOf(primary),
            secondaryBiome = -1,
            blendFactor = 0f
        };
    }

    private BiomeSO FindBiome(float height, float temp)
    {
        HeightType hType = biomeClassifier.GetHeightType(height);
        TemperatureType tType = biomeClassifier.GetTemperatureType(temp);

        foreach (var biome in biomeCollection.biomes)
        {
            if (biome.supportedHeights.Contains(hType) && biome.supportedTemperatures.Contains(tType))
                return biome;
        }

        return null;
    }
    
    BiomeData FindBiomeData(float height, float temperature, Vector3 vertex, Vector3 normal)
    {
        int primary = FindBiomeIndex(height, temperature,vertex, normal); // your logic
        int secondary = -1;
        float blend = 0f;


        int low = FindBiomeIndex(height - delta, temperature,vertex, normal);
        int high = FindBiomeIndex(height + delta, temperature,vertex,normal);

        if (low != primary)
        {
            Debug.Log("lower");
            secondary = low;
            blend = 1f - Mathf.InverseLerp(height - delta, height, height);
        }
        else if (high != primary)
        {
            secondary = high;
            blend = Mathf.InverseLerp(height, height + delta, height);
        }

        return new BiomeData
        {
            primaryBiome = primary,
            secondaryBiome = secondary,
            blendFactor = blend
        };
    }


    private float GetTypeCenter(HeightType type)
    {
        return type switch
        {
            HeightType.Ocean => (biomeClassifier.ocean.min + biomeClassifier.ocean.max) * 0.5f,
            HeightType.Low => (biomeClassifier.lowHeight.min + biomeClassifier.lowHeight.max) * 0.5f,
            HeightType.Medium => (biomeClassifier.mediumHeight.min + biomeClassifier.mediumHeight.max) * 0.5f,
            HeightType.High => (biomeClassifier.highHeight.min + biomeClassifier.highHeight.max) * 0.5f,
            HeightType.Mountain => (biomeClassifier.mountainHeight.min + biomeClassifier.mountainHeight.max) * 0.5f,
            _ => 0f
        };
    }

    private float GetTypeCenter(TemperatureType type)
    {
        return type switch
        {
            TemperatureType.Cold => (biomeClassifier.coldTemp.min + biomeClassifier.coldTemp.max) * 0.5f,
            TemperatureType.Temperate => (biomeClassifier.temperateTemp.min + biomeClassifier.temperateTemp.max) * 0.5f,
            TemperatureType.Hot => (biomeClassifier.hotTemp.min + biomeClassifier.hotTemp.max) * 0.5f,
            _ => 0f
        };
    }

    private float GetTypeRange(HeightType type)
    {
        return type switch
        {
            HeightType.Ocean => (biomeClassifier.ocean.max - biomeClassifier.ocean.min) * 0.5f,
            HeightType.Low => (biomeClassifier.lowHeight.max - biomeClassifier.lowHeight.min) * 0.5f,
            HeightType.Medium => (biomeClassifier.mediumHeight.max - biomeClassifier.mediumHeight.min) * 0.5f,
            HeightType.High => (biomeClassifier.highHeight.max - biomeClassifier.highHeight.min) * 0.5f,
            HeightType.Mountain => (biomeClassifier.mountainHeight.max - biomeClassifier.mountainHeight.min) * 0.5f,
            _ => 1f
        };
    }

    private float GetTypeRange(TemperatureType type)
    {
        return type switch
        {
            TemperatureType.Cold => (biomeClassifier.coldTemp.max - biomeClassifier.coldTemp.min) * 0.5f,
            TemperatureType.Temperate => (biomeClassifier.temperateTemp.max - biomeClassifier.temperateTemp.min) * 0.5f,
            TemperatureType.Hot => (biomeClassifier.hotTemp.max - biomeClassifier.hotTemp.min) * 0.5f,
            _ => 1f
        };
    }

}