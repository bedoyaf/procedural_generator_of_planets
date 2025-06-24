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
        Transform planetPosition
        )
    {
        this.slopeThreshold = slopeThreshold;
        this.equatorTemperature = equatorTemperature;
        this.poleTemperature =  poleTemperature;
        this.temperatureNoiseScale = temperatureNoiseScale;
        this.temperatureNoiseStrength = temperatureNoiseStrength;
        this.currentPlanetPos = planetPosition;
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


        //4 blend zmrd
        /*
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
                               slopeScore * biome.slopeAffinity)*tempScore;// / totalAffinity;

                scores.Add((j, score));
            }

            // 1. Najdi top 4 biomy podle score
            var top4 = scores.OrderByDescending(s => s.score).Take(4).ToList();

            // 2. Lexikograficky set�i� t�chto top 4 podle biomeIndex
            top4 = top4.OrderBy(s => s.biomeIndex).ToList();

            // 3. Rozbal indexy a v�hy
            int indexA = top4[0].biomeIndex;
            int indexB = top4[1].biomeIndex;
            int indexC = top4[2].biomeIndex;
            int indexD = top4[3].biomeIndex;

            float weightA = top4[0].score;
            float weightB = top4[1].score;
            float weightC = top4[2].score;
            float weightD = top4[3].score;

            // 4. Normalizace v�h (aby jejich sou�et byl 1)
            float total = weightA + weightB + weightC + weightD + 1e-6f;

            biomeIndices.Add(new Vector4(indexA, indexB, indexC, indexD));
            biomeWeights.Add(new Vector4(weightA / total, weightB / total, weightC / total, weightD / total));
        }

        Texture2DArray biomTexArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomTexArray);
        mesh.SetUVs(2, biomeIndices);
        mesh.SetUVs(3, biomeWeights);
        
        


        */

        //4 blend trippy
        /*
        var biomeIndices = new Vector4[vertices.Length];
        var biomeWeights = new Vector4[vertices.Length];
        var vertexBiomeScores = new Dictionary<int, float>[vertices.Length];

        // 1. Spo��tej sk�re pro ka�d� vrchol
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


                float score = (heightScore * biome.heightAffinity + slopeScore * biome.slopeAffinity)*tempScore;

                if (score > 0.001f)
                    scores[j] = score;
            }

            vertexBiomeScores[i] = scores;
        }

        // 2. Z�sk�n� index� troj�heln�k�
        int[] indices = mesh.triangles;

        // 3. Pro ka�d� troj�heln�k vyber top 4 biomy a p�id�l je vrchol�m
        for (int tri = 0; tri < indices.Length; tri += 3)
        {
            int a = indices[tri];
            int b = indices[tri + 1];
            int c = indices[tri + 2];

            var combined = new Dictionary<int, float>();

            // Agreguj sk�re ze v�ech 3 vrchol�
            foreach (var dict in new[] { vertexBiomeScores[a], vertexBiomeScores[b], vertexBiomeScores[c] })
            {
                foreach (var kvp in dict)
                {
                    if (!combined.ContainsKey(kvp.Key))
                        combined[kvp.Key] = 0f;
                    combined[kvp.Key] += kvp.Value;
                }
            }

            // Top 4 biomy pro cel� troj�heln�k
            var top4 = combined.OrderByDescending(p => p.Value).Take(4).Select(p => p.Key).OrderBy(i => i).ToList();
            while (top4.Count < 4)
                top4.Add(0); // fallback pokud je m�n� ne� 4 biomy (nap�. u vody)

            // P�id�l v�hy ka�d�mu vrcholu podle t�chto top4 biom�

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
             //   if (rizz) Debug.Log("Top verticies "+tri+": "+top4[0]+" "+ top4[1]+" "+ top4[2]+" "+ top4[3]);

            }
        }

        // 4. Nastav data do mesh + shader
        mesh.SetUVs(2, biomeIndices.ToList());
        mesh.SetUVs(3, biomeWeights.ToList());

        Texture2DArray biomeTextureArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomeTextureArray);
        
        */

        // tady se to sere u regeneraci asi tvorenim meshe, bacha na to rizzlere

        /*
        var deformedVerticies =meshFilter.mesh.vertices;    

        var biomeIndices = new Vector4[vertices.Length];
        var biomeWeights = new Vector4[vertices.Length];
        var vertexBiomeScores = new Dictionary<int, float>[vertices.Length];

        // 1. Spo��tej sk�re pro ka�d� vrchol
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


                float score = (heightScore * biome.heightAffinity + slopeScore * biome.slopeAffinity) * tempScore;

                if (score > 0.001f)
                    scores[j] = score;
            }

            vertexBiomeScores[i] = scores;
        }


        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector4> newBiomeIndices = new List<Vector4>();
        List<Vector4> newBiomeWeights = new List<Vector4>();
        List<int> newTriangles = new List<int>();

        int[] triangles = mesh.triangles;

        for (int tri = 0; tri < triangles.Length; tri += 3)
        {
            int a = triangles[tri];
            int b = triangles[tri + 1];
            int c = triangles[tri + 2];

            // Kombinuj biome score ze v�ech 3 vrchol�
            var combined = new Dictionary<int, float>();

            foreach (int vert in new[] { a, b, c })
            {
                foreach (var kvp in vertexBiomeScores[vert])
                {
                    if (!combined.ContainsKey(kvp.Key))
                        combined[kvp.Key] = 0f;
                    combined[kvp.Key] += kvp.Value;
                }
            }

            var top4 = combined.OrderByDescending(kvp => kvp.Value)
                               .Take(4)
                               .Select(kvp => kvp.Key)
                               .OrderBy(i => i)
                               .ToList();
            while (top4.Count < 4) top4.Add(0); // fallback

            // Duplikuj v�echny 3 vrcholy troj�heln�ku
            foreach (int vert in new[] { a, b, c })
            {
                Vector3 position = deformedVerticies[vert];
                Vector3 normal = normals[vert];

                // Vypo��tej v�hy pro top4
                Vector4 weights = Vector4.zero;
                var scores = vertexBiomeScores[vert];
                for (int i = 0; i < 4; i++)
                {
                    int biomeIdx = top4[i];
                    weights[i] = scores.TryGetValue(biomeIdx, out float score) ? score : 0f;
                }
                float total = weights.x + weights.y + weights.z + weights.w + 1e-6f;
                weights /= total;

                newVertices.Add(position);
                newNormals.Add(normal);
                newBiomeIndices.Add(new Vector4(top4[0], top4[1], top4[2], top4[3]));
                newBiomeWeights.Add(weights);
                newTriangles.Add(newVertices.Count - 1);
                if (tri%10000==0) Debug.Log("Top verticies " + tri + ": " + top4[0] + " " + top4[1] + " " + top4[2] + " " + top4[3]);
                if (tri % 10000 == 0) Debug.Log("Top verticies " + tri + ": " + weights[0] + " " + weights[1] + " " + weights[2] + " " + weights[3]);
            }

        }







        Texture2DArray biomeTextureArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomeTextureArray);

        Mesh newMesh = new Mesh();
        newMesh.indexFormat = newVertices.Count > 65535 ?
               UnityEngine.Rendering.IndexFormat.UInt32 :
               UnityEngine.Rendering.IndexFormat.UInt16;

        newMesh.SetVertices(newVertices);
        newMesh.SetNormals(newNormals);
        newMesh.SetTriangles(newTriangles, 0);
        newMesh.SetUVs(2, newBiomeIndices.ToList());
        newMesh.SetUVs(3, newBiomeWeights.ToList());
        newMesh.RecalculateBounds();

        meshFilter.mesh = newMesh;
        
        */




        //splatting
        
        Vector2[] uvs1 = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normalized = vertices[i].normalized;
            float u = 0.5f + Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI);
            float v = 0.5f - Mathf.Asin(normalized.y) / Mathf.PI;
            uvs1[i] = new Vector2(u, v);
        }
        mesh.uv = uvs1;



        Vector3[] vertices1 = mesh.vertices;
            Vector3[] normals1 = mesh.normals;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;
      //      int numVertices = vertices.Length;

            if (numVertices == 0 || uvs.Length != numVertices || triangles.Length == 0)
            {
                Debug.LogError("Biome texturing failed: Mesh data is invalid or incomplete (missing UVs?).");
                return;
            }

            // KROK 1: Vypo��tat data biom� pro ka�d� vrchol
            var perVertexIndices = new List<Vector4>(numVertices);
            var perVertexWeights = new List<Vector4>(numVertices);
            var scoresList = new List<(int, float)>();

            for (int i = 0; i < numVertices; i++)
            {
                Vector3 worldPos = vertices1[i];
                float height = heights[i];
                float slope = CalculateSlopeFromNormal(normals1[i], worldPos.normalized);
                float temperature = CalculateTemperature(worldPos);

                scoresList.Clear();
                for (int j = 0; j < biomeCollection.biomes.Count; j++)
                {
                    var biome = biomeCollection.biomes[j];

                    var supportedHeight = biome.supportedHeights[0];
                    float heightCenter = biomeClassifier.GetTypeCenter(supportedHeight);
                    float heightRange = biomeClassifier.GetTypeRange(supportedHeight);
                    float heightScore = Mathf.Clamp01(1f - Mathf.Abs(height - heightCenter) / (heightRange * biome.blenddistance + 1e-5f));

                    (float slopeMin, float slopeMax) = biomeClassifier.GetSlopeValues(biome.supportedSlopes[0]);
                    float slopeCenter = (slopeMin + slopeMax) / 2f;
                    float slopeRange = (slopeMax - slopeMin) / 2f;
                    float slopeScore = Mathf.Clamp01(1f - Mathf.Abs(slope - slopeCenter) / (slopeRange * biome.blenddistance + 1e-5f));

                    var supportedTemp = biome.supportedTemperatures[0];
                    float tempCenter = biomeClassifier.GetTypeCenter(supportedTemp);
                    float tempRange = biomeClassifier.GetTypeRange(supportedTemp);
                    float tempScore = Mathf.Clamp01(1f - Mathf.Abs(temperature - tempCenter) / (tempRange * biome.blenddistance + 1e-5f));

                    float finalScore = (heightScore * biome.heightAffinity + slopeScore * biome.slopeAffinity) * tempScore;
                    if (finalScore > 0)
                    {
                        scoresList.Add((j, finalScore));
                    }
                }

                var top4 = scoresList.OrderByDescending(s => s.Item2).Take(4).ToList();
                while (top4.Count < 4) { top4.Add((0, 0.0f)); }

                float totalWeight = top4.Sum(s => s.Item2) + 1e-6f;
                perVertexIndices.Add(new Vector4(top4[0].Item1, top4[1].Item1, top4[2].Item1, top4[3].Item1));
                perVertexWeights.Add(new Vector4(top4[0].Item2 / totalWeight, top4[1].Item2 / totalWeight, top4[2].Item2 / totalWeight, top4[3].Item2 / totalWeight));

              //  if (i % 100 == 0 && top4[0].Item1==0) Debug.Log($"v�hy {top4[0].Item2/totalWeight} {top4[1].Item2/totalWeight} {top4[2].Item2/totalWeight} {top4[3].Item2/totalWeight}");
            }

            // KROK 2: Rasterizovat data do textur
            const int mapResolution = 2048;
            // Vytvo��me indexMap BEZ mipmap a nastav�me spr�vn� filtrov�n�
            var indexMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, false, true); // false = no mipmaps
            indexMap.filterMode = FilterMode.Point;
            indexMap.wrapMode = TextureWrapMode.Repeat;
            var weightMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, true, true); // true = use mipmaps
            weightMap.filterMode = FilterMode.Bilinear;
            weightMap.wrapMode = TextureWrapMode.Repeat;
            var indexMapColors = new Color32[mapResolution * mapResolution];
            var weightMapColors = new Color[mapResolution * mapResolution];

            Action<Vector2, Vector2, Vector2, Vector4, Vector4, Vector4, Vector4, Vector4, Vector4> rasterizeTriangle =
                (uv0, uv1, uv2, i0, i1, i2, w0, w1, w2) =>
                {
                    var allBiomes = new Dictionary<int, (float w_v0, float w_v1, float w_v2)>();
                    Action<Vector4, Vector4> addBiomesForVertex = (indices, weights) => {
                        for (int i = 0; i < 4; i++)
                        {
                            int index = (int)indices[i];
                            if (weights[i] > 0 && !allBiomes.ContainsKey(index))
                            {
                                allBiomes.Add(index, (0, 0, 0));
                            }
                        }
                    };

                    addBiomesForVertex(i0, w0); 
                    addBiomesForVertex(i1, w1); 
                    addBiomesForVertex(i2, w2);

                    var biomeList = allBiomes.Keys.ToList();
                    for (int i = 0; i < biomeList.Count; i++)
                    {
                        int biomeIndex = biomeList[i];
                        var currentWeights = allBiomes[biomeIndex];
                        for (int j = 0; j < 4; j++)
                        {
                            if ((int)i0[j] == biomeIndex) currentWeights.w_v0 = w0[j];
                            if ((int)i1[j] == biomeIndex) currentWeights.w_v1 = w1[j];
                            if ((int)i2[j] == biomeIndex) currentWeights.w_v2 = w2[j];
                        }
                        allBiomes[biomeIndex] = currentWeights;
                    }

                    Vector2 p0 = new Vector2(uv0.x * (mapResolution - 1), uv0.y * (mapResolution - 1));
                    Vector2 p1 = new Vector2(uv1.x * (mapResolution - 1), uv1.y * (mapResolution - 1));
                    Vector2 p2 = new Vector2(uv2.x * (mapResolution - 1), uv2.y * (mapResolution - 1));

                    int xMin = (int)Mathf.Floor(Mathf.Min(p0.x, p1.x, p2.x));
                    int xMax = (int)Mathf.Ceil(Mathf.Max(p0.x, p1.x, p2.x));
                    int yMin = (int)Mathf.Floor(Mathf.Min(p0.y, p1.y, p2.y));
                    int yMax = (int)Mathf.Ceil(Mathf.Max(p0.y, p1.y, p2.y));

                    var perPixelScores = new List<(int index, float score)>();

                    for (int y = yMin; y <= yMax; y++)
                    {
                        for (int x = xMin; x <= xMax; x++)
                        {
                            if (x < 0 || x >= mapResolution || y < 0 || y >= mapResolution) continue;

                            Vector2 p = new Vector2(x, y);
                            Vector3 bary = Barycentric(p, p0, p1, p2);

                            if (bary.x >= -0.001f && bary.y >= -0.001f && bary.z >= -0.001f)
                            {
                                perPixelScores.Clear();
                                foreach (var biomeIndex in biomeList)
                                {
                                    var vWeights = allBiomes[biomeIndex];
                                    float interpolatedWeight = vWeights.w_v0 * bary.x + vWeights.w_v1 * bary.y + vWeights.w_v2 * bary.z;
                                    if (interpolatedWeight > 0.0001f)
                                    {
                                        perPixelScores.Add((biomeIndex, interpolatedWeight));
                                    }
                                }

                                var top4 = perPixelScores.OrderByDescending(s => s.score).Take(4).ToList();
                                while (top4.Count < 4) { top4.Add((0, 0.0f)); }

                                var finalIndices = new Vector4(top4[0].index, top4[1].index, top4[2].index, top4[3].index);
                                var finalWeights = new Vector4(top4[0].score, top4[1].score, top4[2].score, top4[3].score);

                                int pixelIndex = y * mapResolution + x;
                                indexMapColors[pixelIndex] = new Color32((byte)finalIndices.x, (byte)finalIndices.y, (byte)finalIndices.z, (byte)finalIndices.w);
                                weightMapColors[pixelIndex] = new Color(finalWeights.x, finalWeights.y, finalWeights.z, finalWeights.w);
                            }
                        }
                    }
                };

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                Vector2 uv0 = uvs[v0];
                Vector2 uv1 = uvs[v1];
                Vector2 uv2 = uvs[v2];

            // Jednoduch� oprava �vu na UV map�
              if (Mathf.Max(uv0.x, uv1.x, uv2.x) - Mathf.Min(uv0.x, uv1.x, uv2.x) > 0.8f)
              {
                  continue;
              }


            rasterizeTriangle(uv0, uv1, uv2,
                    perVertexIndices[v0], perVertexIndices[v1], perVertexIndices[v2],
                    perVertexWeights[v0], perVertexWeights[v1], perVertexWeights[v2]);
            }




        // KROK 3: Aplikovat textury na materi�l
        indexMap.SetPixels32(indexMapColors);
            indexMap.Apply(false); // false = neaktualizovat mipmapy (proto�e ��dn� nejsou) a ponechat �iteln�

            weightMap.SetPixels(weightMapColors);
            weightMap.Apply(true); // true = aktualizovat mipmapy

            material.SetTexture("_Biomes", GenerateBiomeTextureArray(biomeCollection));
            material.SetTexture("_IndexMap", indexMap);
            material.SetTexture("_WeightMap", weightMap);
            meshRenderer.sharedMaterial = material;

            // Voliteln�: Ulo�en� debug textur

            try
            {
                System.IO.File.WriteAllBytes(Application.dataPath + "/_DEBUG_indexMap.png", indexMap.EncodeToPNG());
                System.IO.File.WriteAllBytes(Application.dataPath + "/_DEBUG_weightMap.png", weightMap.EncodeToPNG());
                Debug.Log("Biome debug textures saved to Assets folder.");
            }
            catch (Exception e) { Debug.LogError($"Failed to save debug textures: {e.Message}"); }

        Texture2DArray biomeTextureArray = GenerateBiomeTextureArray(biomeCollection);
        material.SetTexture("_Biomes", biomeTextureArray);
        

        meshRenderer.sharedMaterial = material;
    }



    private static Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = p - a;

        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;
        if (Mathf.Abs(denom) < 1e-6f)
            return new Vector3(-1, -1, -1); // Degenerate triangle

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        return new Vector3(u, v, w);
    }


  //  private float heightBlendmin = 1.5f;
  //  private float heightBlendmax = 0.5f;
  //  private float heightBlendCurve = 2.5f;

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





}