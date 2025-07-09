using UnityEngine;


/// <summary>
/// Class important for storing all the important data relevant in sphere and planet generation
/// </summary>
public class PlanetData
{
    public GameObject gameobject;
    public MeshFilter meshFilter;
    public Mesh generatedMesh;
    public MeshRenderer meshRenderer;
    public Vector3[] baseVertices;      // base vertices
    public float[] processedHeights;    // height calculated from terrain
    public Vector3[] processedVertices; // The current vertice, changed by the terrain generation
    public int numVertices;
    public bool meshDataGenerated = false; // tracks if the sphere itself is generated
    public bool pipelineInitialized = false; // Tracks if terrain can be generated
}