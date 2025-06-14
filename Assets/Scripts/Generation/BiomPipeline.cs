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
using ProceduralMeshes;
using Color = UnityEngine.Color;
using static SphereMeshOptimal;

public class BiomPipeline
{
    private ComputeBuffer biomeIndexBuffer;
    
    private float slopeThreshold = 30f;
    
    
    private float equatorTemperature = 1.0f;

    
    private float poleTemperature = 0.0f;

    
    private float temperatureNoiseScale = 1.0f;
    private float temperatureNoiseStrength = 0.2f;



    private float falloff = 1f;

    private float smoothnessExponent = 1;
    private float blenddistance = 1;

  //  private float delta = 0.005f;

    
    private Material material;

    private BiomeCollectionSO biomeCollection;
    private BiomeClassifierSO biomeClassifier;

    // [SerializeField] private float minHeight=0f;
    //  [SerializeField] private float maxHeight=1f;

    private Vector3[] baseVertices;

    private float[] heights;

    private float sphereRadius;

    private Mesh mesh;

    private MeshFilter meshFilter;

    private MeshRenderer meshRenderer;

    Transform currentPlanetPos;


    private float maxHeight;
    private float minHeight;

    public void Initialize(MeshRenderer meshRenderer, MeshFilter meshFilter, BiomeClassifierSO biomeClassifier, BiomeCollectionSO biomeCollection)
    {
        meshRenderer.material = material;
        mesh = meshFilter.mesh;
        this.meshRenderer = meshRenderer;
        this.meshFilter = meshFilter;
        this.biomeClassifier = biomeClassifier;
        this.biomeCollection = biomeCollection;
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

    public void UpdateBiomPipelineValues( 
        float slopeThreshold,
        float equatorTemperature, 
        float poleTemperature, 
        float temperatureNoiseScale, 
        float temperatureNoiseStrength,
        float falloff,
        float smoothnessExponent,
        float blenddistance,
        float heightBlendmin,
        float heightBlendmax,
        float heightBlendCurve,
        Transform planetPosition
        
        //,
    //    float delta
        )
    {
        this.slopeThreshold = slopeThreshold;
        this.equatorTemperature = equatorTemperature;
        this.poleTemperature =  poleTemperature;
        this.temperatureNoiseScale = temperatureNoiseScale;
        this.temperatureNoiseStrength = temperatureNoiseStrength;
        this.falloff = falloff;
        this.smoothnessExponent = smoothnessExponent;
        this.blenddistance = blenddistance;
        this.currentPlanetPos = planetPosition;
        //    this.delta = delta;

        this.heightBlendmin = heightBlendmin;
        this.heightBlendmax = heightBlendmax;
        this.heightBlendCurve = heightBlendCurve;
    }

    private ComputeBuffer heightBuffer;
    // private ComputeBuffer positionBuffer;

    public void ApplyTexturesToMesh(Material material,Vector3[] vertices, Vector3[] normals)
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

        /*
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

        maxHeight = maxHeight2;
        minHeight = minHeight2;*/

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

        //puvodni shit blend top 2 ------------------------------------------------------------
        /*
      var biomeIndices = new List<Vector4>(vertices.Length);
      var biomeWeights = new List<Vector4>(vertices.Length);

      for (int i = 0; i < vertices.Length; i++)
      {
          float height = heights[i];
        //  float temp = temperatures[i];

          HeightType heightType = biomeClassifier.GetHeightType(height);
          //   TemperatureType tempType = biomeClassifier.GetTemperatureType(temp);

          float slope = CalculateSlopeFromNormal(normals[i], vertices[i]);

          var scores = new List<(int biomeIndex, float score)>();

          for (int j = 0; j < biomeCollection.biomes.Count; j++)
          {
              var biome = biomeCollection.biomes[j];




              var supportedHeight = biome.supportedHeights[0];
              float heightCenter = GetTypeCenter(supportedHeight);
              float heightRange = GetTypeRange(supportedHeight);
              float heightScore = Mathf.Clamp01(1f - Mathf.Abs(height - heightCenter) / (heightRange * blenddistance + 1e-5f));

              // SLOPE SCORING
              (float slopeMin, float slopeMax) = biomeClassifier.GetSlopeValues(biome.supportedSlopes[0]);
              float slopeCenter = (slopeMin + slopeMax) / 2f;
              float slopeRange = (slopeMax - slopeMin) / 2f;
              float slopeScore = Mathf.Clamp01(1f - Mathf.Abs(slope - slopeCenter) / (slopeRange * blenddistance + 1e-5f));

            slopeScore = 0;

              float score = heightScore * biome.heightAffinity + slopeScore * biome.slopeAffinity;
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

    */

        //  ------------------------------------------------------

        /*
        
        int[] biomIndicies = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            float height = heights[i];
            float temp = CalculateTemperature(baseVertices[i]);
            Vector3 normal = normals[i];
            Vector3 vertex = vertices[i];

            biomIndicies[i] = FindBiomeIndex(height, temp, normal, vertex);
        }

        int[] triangles = mesh.triangles;
        Vector3[] originalVerts = mesh.vertices;
        Color[] originalColors = mesh.colors;

        int triCount = triangles.Length / 3;
        Vector3[] newVerts = new Vector3[triangles.Length];

        Vector4[] uv1 = new Vector4[triangles.Length]; // color0
        Vector4[] uv2 = new Vector4[triangles.Length]; // color1
        Vector4[] uv3 = new Vector4[triangles.Length]; // color2
        Vector3[] barycentrics = new Vector3[triangles.Length]; // TEXCOORD0

        int[] newTris = new int[triangles.Length];

        for (int tri = 0; tri < triCount; tri++)
        {
            int i0 = triangles[tri * 3 + 0];
            int i1 = triangles[tri * 3 + 1];
            int i2 = triangles[tri * 3 + 2];

            Color c0 = biomeCollection.biomes[biomIndicies[i0]].biomeColor;
            Color c1 = biomeCollection.biomes[biomIndicies[i1]].biomeColor;
            Color c2 = biomeCollection.biomes[biomIndicies[i2]].biomeColor;

            // For each vertex of the triangle, duplicate and assign
            int baseIndex = tri * 3;

            newVerts[baseIndex + 0] = originalVerts[i0];
            newVerts[baseIndex + 1] = originalVerts[i1];
            newVerts[baseIndex + 2] = originalVerts[i2];

            barycentrics[baseIndex + 0] = new Vector3(1, 0, 0); // A
            barycentrics[baseIndex + 1] = new Vector3(0, 1, 0); // B
            barycentrics[baseIndex + 2] = new Vector3(0, 0, 1); // C

            uv1[baseIndex + 0] = c0;
            uv2[baseIndex + 0] = c1;
            uv3[baseIndex + 0] = c2;

            uv1[baseIndex + 1] = c0;
            uv2[baseIndex + 1] = c1;
            uv3[baseIndex + 1] = c2;

            uv1[baseIndex + 2] = c0;
            uv2[baseIndex + 2] = c1;
            uv3[baseIndex + 2] = c2;

            newTris[baseIndex + 0] = baseIndex + 0;
            newTris[baseIndex + 1] = baseIndex + 1;
            newTris[baseIndex + 2] = baseIndex + 2;
        }

        Mesh triMesh = new Mesh();
        triMesh.indexFormat = numVertices > 65535 ?
               UnityEngine.Rendering.IndexFormat.UInt32 :
               UnityEngine.Rendering.IndexFormat.UInt16;

        triMesh.vertices = newVerts;
        triMesh.triangles = newTris;
        triMesh.SetUVs(0, barycentrics);
        triMesh.SetUVs(1, uv1);
        triMesh.SetUVs(2, uv2);
        triMesh.SetUVs(3, uv3);
        triMesh.RecalculateNormals();
        triMesh.RecalculateBounds();


        meshFilter.mesh = triMesh;
        */

        /*
        //4 blend zmrd

        var biomeIndices = new List<Vector4>(vertices.Length);
        var biomeWeights = new List<Vector4>(vertices.Length);

        for (int i = 0; i < vertices.Length; i++)
        {
            float height = heights[i];
            float slope = CalculateSlopeFromNormal(normals[i], vertices[i]);

            var scores = new List<(int biomeIndex, float score)>();

            for (int j = 0; j < biomeCollection.biomes.Count; j++)
            {
                var biome = biomeCollection.biomes[j];

                // Height scoring
                var supportedHeight = biome.supportedHeights[0];
                float heightCenter = GetTypeCenter(supportedHeight);
                float heightRange = GetTypeRange(supportedHeight);
                // float heightScore = Mathf.Clamp01(1f - Mathf.Abs(height - heightCenter) / (heightRange * blenddistance + 1e-5f));


                // Slope in range [0, 1]; steeper = closer to 1
                float slopeSharpness = Mathf.Clamp01(slope / 90); // already normalized if calculated as dot(normal, up)


                //   float slopeBlendFactor = Mathf.Lerp(heightBlendmin, heightBlendmax,Mathf.Pow(slopeSharpness, heightBlendCurve)); // more curve


                // Now apply that to the height blending range
                float effectiveRange = heightRange * biome.blenddistance + 1e-5f;

                float heightScore = Mathf.Clamp01(1f - Mathf.Abs(height - heightCenter) / effectiveRange);


                float temp = CalculateTemperature(baseVertices[i]);
                float tempCenter = GetTypeCenter(biome.supportedTemperatures[0]);
                float tempRange = GetTypeRange(biome.supportedTemperatures[0]);
                float tempScore = Mathf.Clamp01(1f - Mathf.Abs(temp - tempCenter) / (tempRange* biome.blenddistance + 1e-5f));

          //      if (temp < (tempCenter - tempRange) || temp > (tempCenter + tempRange)) continue; 
                    // Slope scoring (optional - currently disabled)
                    //       float slopeScore = 0f;

                    (float slopeMin, float slopeMax) = biomeClassifier.GetSlopeValues(biome.supportedSlopes[0]);
                float slopeCenter = (slopeMin + slopeMax) / 2f;
                float slopeRange = (slopeMax - slopeMin) / 2f;
                float slopeScore = Mathf.Clamp01(1f - Mathf.Abs(slope - slopeCenter) / (slopeRange * biome.blenddistance + 1e-5f));


                // float totalAffinity = biome.heightAffinity + biome.slopeAffinity +  biome.temperatureAffinity + 1e-5f;
                float score = (heightScore * biome.heightAffinity +
                               slopeScore * biome.slopeAffinity/)*tempScore;// / totalAffinity;

                scores.Add((j, score));
            }

            // 1. Najdi top 4 biomy podle score
            var top4 = scores.OrderByDescending(s => s.score).Take(4).ToList();

            // 2. Lexikograficky setøiï tìchto top 4 podle biomeIndex
        //    top4 = top4.OrderBy(s => s.biomeIndex).ToList();

            // 3. Rozbal indexy a váhy
            int indexA = top4[0].biomeIndex;
            int indexB = top4[1].biomeIndex;
            int indexC = top4[2].biomeIndex;
            int indexD = top4[3].biomeIndex;

            float weightA = top4[0].score;
            float weightB = top4[1].score;
            float weightC = top4[2].score;
            float weightD = top4[3].score;

            // 4. Normalizace váh (aby jejich souèet byl 1)
            float total = weightA + weightB + weightC + weightD + 1e-6f;

            biomeIndices.Add(new Vector4(indexA, indexB, indexC, indexD));
            biomeWeights.Add(new Vector4(weightA / total, weightB / total, weightC / total, weightD / total));
        }

        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomTexArray);
        mesh.SetUVs(2, biomeIndices);
        mesh.SetUVs(3, biomeWeights);
        */
        var biomeIndices = new Vector4[vertices.Length];
        var biomeWeights = new Vector4[vertices.Length];
        var vertexBiomeScores = new Dictionary<int, float>[vertices.Length];

        // 1. Spoèítej skóre pro každý vrchol
        for (int i = 0; i < vertices.Length; i++)
        {
            float height = heights[i];
            float slope = CalculateSlopeFromNormal(normals[i], vertices[i]);
            var scores = new Dictionary<int, float>();

            for (int j = 0; j < biomeCollection.biomes.Count; j++)
            {
                var biome = biomeCollection.biomes[j];
                var supportedHeight = biome.supportedHeights[0];

                float heightCenter = GetTypeCenter(supportedHeight);
                float heightRange = GetTypeRange(supportedHeight);
                float effectiveRange = heightRange * biome.blenddistance + 1e-5f;

                float heightScore = Mathf.Clamp01(1f - Mathf.Abs(height - heightCenter) / effectiveRange);

                (float slopeMin, float slopeMax) = biomeClassifier.GetSlopeValues(biome.supportedSlopes[0]);
                float slopeCenter = (slopeMin + slopeMax) / 2f;
                float slopeRange = (slopeMax - slopeMin) / 2f;

                float slopeScore = Mathf.Clamp01(1f - Mathf.Abs(slope - slopeCenter) / (slopeRange * biome.blenddistance + 1e-5f));

                float temp = CalculateTemperature(baseVertices[i]);
                float tempCenter = GetTypeCenter(biome.supportedTemperatures[0]);
                float tempRange = GetTypeRange(biome.supportedTemperatures[0]);
                float tempScore = Mathf.Clamp01(1f - Mathf.Abs(temp - tempCenter) / (tempRange * biome.blenddistance + 1e-5f));



                //     float score = heightScore * biome.heightAffinity + slopeScore * biome.slopeAffinity;
                float score = (heightScore * biome.heightAffinity +
                    slopeScore * biome.slopeAffinity ) * tempScore;

                if (score > 0.001f)
                    scores[j] = score;
            }

            vertexBiomeScores[i] = scores;
        }

        // 2. Získání indexù trojúhelníkù
        int[] indices = mesh.triangles;

        // 3. Pro každý trojúhelník vyber top 4 biomy a pøidìl je vrcholùm
        for (int tri = 0; tri < indices.Length; tri += 3)
        {
            int a = indices[tri];
            int b = indices[tri + 1];
            int c = indices[tri + 2];

            var combined = new Dictionary<int, float>();

            // Agreguj skóre ze všech 3 vrcholù
            foreach (var dict in new[] { vertexBiomeScores[a], vertexBiomeScores[b], vertexBiomeScores[c] })
            {
                foreach (var kvp in dict)
                {
                    if (!combined.ContainsKey(kvp.Key))
                        combined[kvp.Key] = 0f;
                    combined[kvp.Key] += kvp.Value;
                }
            }

            // Top 4 biomy pro celý trojúhelník
            var top4 = combined.OrderByDescending(p => p.Value).Take(4).Select(p => p.Key).OrderBy(i => i).ToList();
            while (top4.Count < 4)
                top4.Add(0); // fallback pokud je ménì než 4 biomy (napø. u vody)

            // Pøidìl váhy každému vrcholu podle tìchto top4 biomù
            foreach (int vert in new[] { a, b, c })
            {
                var vertexScores = vertexBiomeScores[vert];
                Vector4 weights = Vector4.zero;

                for (int i = 0; i < 4; i++)
                {
                    int biomeIdx = top4[i];
                    weights[i] = vertexScores.TryGetValue(biomeIdx, out float score) ? score : 0f;
                }

                float total = weights.x + weights.y + weights.z + weights.w + 1e-6f;
                weights /= total;

                biomeIndices[vert] = new Vector4(top4[0], top4[1], top4[2], top4[3]);
                biomeWeights[vert] = weights;
            }
        }

        // 4. Nastav data do mesh + shader
        mesh.SetUVs(2, biomeIndices.ToList());
        mesh.SetUVs(3, biomeWeights.ToList());

        Texture2DArray biomeTextureArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomeTextureArray);






        /*
        
     //   Debug.Log("skibidi: " + currentPlanetPos[0] +" "+ currentPlanetPos[1]+" " + currentPlanetPos[2]);

        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomTexArray);

        // Set blending and scale parameters
     //     material.SetFloat("_Scale", textureScale);
          material.SetFloat("_BlendDistance", blenddistance);
        material.SetVector("_PlanetCenter", currentPlanetPos.position);

        //     material.SetFloat("_HeightBlendMin", heightBlendMin);
        //    material.SetFloat("_HeightBlendMax", heightBlendMax);
        //     material.SetFloat("_HeightBlendCurve", heightBlendCurve);

        // Prepare biome data
        int biomeCount = Mathf.Min(biomeCollection.biomes.Count, 8); // Max 8 for now
        Vector4[] biomeDataArray = new Vector4[8];
        for (int i = 0; i < biomeCount; i++)
        {

            var supportedHeight = biomeCollection.biomes[i].supportedHeights[0];
            float heightCenter = GetTypeCenter(supportedHeight);
            float heightRange = GetTypeRange(supportedHeight);

            biomeDataArray[i] = new Vector4(
                heightCenter,
                heightRange,
                biomeCollection.biomes[i].heightAffinity,
                biomeCollection.biomes[i].slopeAffinity
            );
        }


        Vector4[] slopeData = new Vector4[biomeCount];
        for (int i = 0; i < biomeCount; i++)
        {

            var biome = biomeCollection.biomes[i];
            (float slopeMin, float slopeMax) = biomeClassifier.GetSlopeValues(biome.supportedSlopes[0]);
         //   Debug.Log("skibidi: " + slopeMin);
            slopeData[i] = new Vector4(slopeMin, slopeMax, 0, 0);
        }
        material.SetVectorArray("_BiomeSlopeData", slopeData);



        // Fill remaining entries with zeroes
        for (int i = biomeCount; i < 8; i++)
            biomeDataArray[i] = Vector4.zero;




        material.SetInt("_BiomeCount", biomeCount);
        material.SetVectorArray("_BiomeData", biomeDataArray);
        
        */
        /*
        List<Vector2> biomeIndices = new List<Vector2>(numVertices);
        for (int i = 0; i < numVertices; i++)
        {


            float temp = CalculateTemperature(baseVertices[i]);
            
            int newIndex = FindBiomeIndex(heights[i], temp, normals[i], vertices[i]);

            biomeIndices.Add(new Vector2((float)newIndex, 0f));
        }
        //   mesh.colors = vertexColors;

        mesh.SetUVs(2, biomeIndices);
        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_BiomeTexArray", biomTexArray);
        */
        /*
        
        int[] triangles = mesh.triangles;
        Vector3[] biomeData = new Vector3[numVertices];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int triA = triangles[i];
            int triB = triangles[i + 1];
            int triC = triangles[i + 2];

            biomeData[triA] = new Vector3(FindBiomeIndex(heights[triA], 0, normals[triA], vertices[triA]), 0, 0); // only first channel
            biomeData[triB] = new Vector3(0, FindBiomeIndex(heights[triB], 0, normals[triB], vertices[triB]), 0); // only second channel
            biomeData[triC] = new Vector3(0, 0, FindBiomeIndex(heights[triC], 0, normals[triC], vertices[triC])); // only third channel
        }
        mesh.SetUVs(2, biomeData);
        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_BiomeTexArray", biomTexArray);
        
        */







        /*
        float[] biomeStrengths = new float[biomeCollection.biomes.Count];
        List<Vector3> biomeWeights = new List<Vector3>(numVertices);
        List<Vector3> biomeIndices = new List<Vector3>(numVertices);
        for(int i = 0;i<numVertices;i++)
        {
            float totalStrength = 0;
            for (int j = 0; j < biomeCollection.biomes.Count; j++)
            {
                // Calculate strength based on distance from the biome's ideal height
                float dist = Mathf.Abs(heights[i] - GetTypeCenter(biomeCollection.biomes[j].supportedHeights[0]));
                float strength = Mathf.Max(0, 1 - (dist / blenddistance*GetTypeRange(biomeCollection.biomes[j].supportedHeights[0])));

                // Optional: Power makes the core of the biome stronger
                strength = Mathf.Pow(strength, 1.5f);

                biomeStrengths[j] = strength;
                totalStrength += strength;
            }

            // Normalize the weights so they sum to 1
            Vector3 finalWeights = Vector3.zero;
            if (totalStrength > 0)
            {
                // This safely handles cases with 1, 2, or 3 biomes.
                // It assumes biome 0 -> R, 1 -> G, 2 -> B channel.
                if (biomeCollection.biomes.Count > 0) finalWeights.x = biomeStrengths[0] / totalStrength;
                if (biomeCollection.biomes.Count > 1) finalWeights.y = biomeStrengths[1] / totalStrength;
                if (biomeCollection.biomes.Count > 2) finalWeights.z = biomeStrengths[2] / totalStrength;
            }
            else
            {
                // Fallback: If vertex is outside all blend zones, assign it to the first biome.
                if (biomeCollection.biomes.Count > 0) finalWeights.x = 1;
            }

            biomeWeights.Add(finalWeights);

        }
        mesh.SetUVs(2, biomeIndices); // List<Vector3> of biome index triplets (int-castable floats)
        mesh.SetUVs(3, biomeWeights); // List<Vector3> of blend weights (must sum to ~1.0)
        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_BiomeTexArray", biomTexArray);
        
        */
        /*
        List<Vector3> biomeIndices = new List<Vector3>(numVertices);

        for (int i = 0; i < numVertices; i++)
        {



            float temp = CalculateTemperature(baseVertices[i]);
            int biomeIndex = FindBiomeIndex(heights[i], temp, normals[i], vertices[i]);


            biomeIndices.Add(new Vector3((float)biomeIndex, 0, 0)); // only .x will be used
        }

        mesh.SetUVs(2, biomeIndices); // TEXCOORD2.x = biome index

        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_BiomeTexArray", biomTexArray);
        */



        meshRenderer.sharedMaterial = material;
    }


    private float heightBlendmin = 1.5f;
    private float heightBlendmax = 0.5f;
    private float heightBlendCurve = 2.5f;

    // public float delta = 0.005f;


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



 //   [SerializeField] private Gradient biomeGradient;
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



    /*
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

        BiomeSO[] neighbors = { heightUp, heightDown };

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
*/
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
    /*
    BiomeData FindBiomeData(float height, float temperature, Vector3 vertex, Vector3 normal)
    {
        int primary = FindBiomeIndex(height, temperature, vertex, normal); // your logic
        int secondary = -1;
        float blend = 0f;


        int low = FindBiomeIndex(height - delta, temperature, vertex, normal);
        int high = FindBiomeIndex(height + delta, temperature, vertex, normal);

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
    }*/


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