using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static ProceduralMesh;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class PlanetGenerator : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] int seed = 0;
    private int currentlyUsingSeed = 0;
    [SerializeField]
    SphereAlgorithm sphereAlgorithm;
    [SerializeField]
    TerrainGeneration terrainGeneration;

    [Header("Sphere Mesh")]
    [Range(0, 300)] // Controls the level of subdivision
    [SerializeField] int resolution = 2;
    [Range(0, 100)] // Controls the level of subdivision
    public int radius = 1;

    [SerializeField] bool renderTriangles = true;

    private MeshFilter meshFilter;

    private Vector3[] originalVertices;

    private Vector3[] originalVerticesTerrain;

    //  private Vector3[] deformedVertices;
    private Vector3[] vertices, normals;

    private Vector4[] tangents;

    public enum SphereAlgorithm { Nothing = 0, SebastianLeague = 1, Recursive = 2, Optimal }
    public enum TerrainGeneration { Non = 0, Sin, Crater, Noise, Noise2, Noise3 }


    Mesh currentmesh;

    private bool meshGenerated = false;

    private SphereMeshOptimal specialmesh;

    private int numVertices;


    [Header("Shaders")]
    public ShaderControllerAbstract currentShader;
    [SerializeField] public CraterGenerator craterGenerator;
    [SerializeField] public NoiseShaderController noiseShaderController;
    [SerializeField] public NoiseShaderControllerMoreShaders noiseShaderController2;
    [SerializeField] public NoiseShaderController2 noiseShaderController3;


    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentlyUsingSeed != seed) { 
            UnityEngine.Random.InitState(seed);
            currentlyUsingSeed = seed;
        }
    }

    public void GeneratePlanet()
    {
        if (sphereAlgorithm == SphereAlgorithm.Nothing) return;
        else if (sphereAlgorithm == SphereAlgorithm.SebastianLeague) GenerateSphereMesh(new SphereMesh(resolution));
        /*  else if (sphereAlgorithm == SphereAlgorithm.Recursive) GenerateSphereMesh(new SphereMeshRecursive(resolution, radius));*/
        else if (sphereAlgorithm == SphereAlgorithm.Optimal) GenerateSphereMesh(new SphereMeshOptimal(resolution, radius));
        //    GenerateSphereMesh();
    }

    private void GenerateSphereMesh(ISphereMesh sphereMesh)
    {
        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = sphereMesh.Vertices,
            triangles = sphereMesh.Triangles
        };

        mesh.RecalculateNormals();

        if (!renderTriangles)
        {
            List<int> edgeIndices = sphereMesh.CreateEdgeIndices(sphereMesh.Triangles);
            mesh.SetIndices(edgeIndices.ToArray(), MeshTopology.Lines, 0);

        }

        if(sphereAlgorithm == SphereAlgorithm.Optimal)
        {
            specialmesh = (SphereMeshOptimal)sphereMesh;
        }

        Debug.Log($"Vertex Count: {sphereMesh.Vertices.Length}, Triangle Count: {sphereMesh.Triangles.Length / 3}");

        currentmesh = mesh;
        meshFilter.mesh = mesh;
        originalVertices = mesh.vertices.Clone() as Vector3[];
        meshGenerated = true;

        numVertices = originalVertices.Length;
    }

    private bool isOneOfSpecialVerticies(Vector3 vertex)
    {
        foreach (var ver in specialmesh.specialVerticies)
        {
            if (vertex == ver) return true;
        }
        return false;
    }


    [SerializeField] ComputeShader computeShaderMoonHeight;
  //  [SerializeField] ComputeShader computeShaderCrater;
   // ComputeBuffer vertexBuffer, heightBuffer, craterBuffer;




  //  private ComputeBuffer verticesBuffer;
  //  private ComputeBuffer heightsBuffer;

    void OnValidate()
    {
        if (Application.isPlaying && meshGenerated)
        {
          //  if (craterGenerator.settedUp && !craterGenerator.running) craterGenerator.RunComputeShader();
        }
    }

    private int kernel;

    //  craterList.Add(new Crater { center = new Vector3(0, 0, 0), radius = 0.5f, depth = -0.2f, rimWidth = 0.1f, rimSteepness = 5 });


    public void RunComputeShader()
    {
        if(!meshGenerated) GeneratePlanet();
        Debug.Log("about to generate teerain");
        if (terrainGeneration == TerrainGeneration.Crater)
        {
            currentShader = craterGenerator;
        }
        else if (terrainGeneration == TerrainGeneration.Noise)
        {
            currentShader = noiseShaderController;
        }
        else if (terrainGeneration == TerrainGeneration.Noise2)
        {
            currentShader = noiseShaderController2;
        }
        else if (terrainGeneration == TerrainGeneration.Noise3)
        {
            currentShader = noiseShaderController3;
        }
        currentShader.SetupTerrainGenerator(meshFilter, originalVertices, radius);
        currentShader.RunComputeShader();

      //  originalVerticesTerrain = 
    }


    /*
  void SetupComputeShader()
  {
      // Example craters
      craterList.Add(new Crater { center = new Vector3(0, 0, 0), radius = 1, depth = -0.2f, rimWidth = 0.1f, rimSteepness = 5 });
      craterList.Add(new Crater { center = new Vector3(2, 0, 1), radius = 1.5f, depth = -0.3f, rimWidth = 0.15f, rimSteepness = 4 });

      ComputeBuffer craterBuffer = new ComputeBuffer(craterList.Count, sizeof(float) * 6);
      craterBuffer.SetData(craterList.ToArray());

      computeShader.SetBuffer(kernel, "craters", craterBuffer);
      computeShader.SetInt("numCraters", craterList.Count);
  }

  */




    /*
    void OnDrawGizmos()
    {
        if (currentmesh == null)
        {
            return;
        }

        Vector3[] verticiesGizmo= new Vector3[1];


        if (vertices == null)
        {
            // vertices = currentmesh.vertices;
            verticiesGizmo = specialmesh.specialVerticies.ToArray();
        }

        Transform t = transform;
        for (int i = 0; i < verticiesGizmo.Length; i++)
        {
            Vector3 position = t.TransformPoint(verticiesGizmo[i]);

            if (sphereAlgorithm == SphereAlgorithm.Optimal && isOneOfSpecialVerticies(vertices[i]))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(position, 0.002f);
        //    }
        }
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }


    [ContextMenu("Store triangle data")]
    public void WriteTrianglesToFile()
    {
        if (specialmesh == null || specialmesh.Triangles == null)
        {
            Debug.LogError("SphereMesh or Triangles array is null!");
            return;
        }

        string path = Application.dataPath + "/triangle_debug.txt";
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("Triangle Indices:");
            for (int i = 0; i < specialmesh.Triangles.Length; i += 3)
            {
                writer.WriteLine($"{i / 3}: {specialmesh.Triangles[i]}, {specialmesh.Triangles[i + 1]}, {specialmesh.Triangles[i + 2]}");
            }
        }

        Debug.Log($"Triangle data written to: {path}");
    }*/

}
