using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Vector3;
/*
public class SphereMeshRecursive : ISphereMesh
{
    public abstract List<int> CreateEdgeIndices(int[] triangleIndices);
    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }
    public int Resolution { get; }

    // Internal:
    List<Vector3> vertices;
    List<int> triangles;
    int numDivisions;
    int numVertsPerFace;

    // Indices of the vertex pairs that make up each of the initial 12 edges
    //static readonly int[] vertexPairs = { 0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 2, 3, 3, 4, 4, 1, 5, 1, 5, 2, 5, 3, 5, 4 };
    // Indices of the edge triplets that make up the initial 8 faces
    // static readonly int[] edgeTriplets = { 0, 1, 4, 1, 2, 5, 2, 3, 6, 3, 0, 7, 8, 9, 4, 9, 10, 5, 10, 11, 6, 11, 8, 7 };
    // The six initial vertices
    private Dictionary<(int, int), int> edgeMidpointCache = new Dictionary<(int, int), int>();


    static readonly Vector3[] baseVertices = { up, left, back, right, forward, down };

    public SphereMeshRecursive(int resolution, int radius)
    {
        this.Resolution = resolution;
        numDivisions = Mathf.Max(0, resolution);
        numVertsPerFace = ((numDivisions + 3) * (numDivisions + 3) - (numDivisions + 3)) / 2;
        int numVerts = numVertsPerFace * 8 - (numDivisions + 2) * 12 + 6;
        int numTrisPerFace = (numDivisions + 1) * (numDivisions + 1);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        vertices.AddRange(baseVertices);


        List<Edge> edges = new List<Edge>();

        for (int i = 0; i < 12 - 1; i++)
        {
            int[] edgeVertexIndices = new int[2];
            edgeVertexIndices[0] = i;
            edgeVertexIndices[1] = i;
            Edge edge = new Edge(edgeVertexIndices);
        }

        List<Triangle> maintriangles = new List<Triangle>();
        // Up Faces (Counterclockwise)
        maintriangles.Add(new Triangle(0, 2, 1));
        maintriangles.Add(new Triangle(0, 3, 2));
        maintriangles.Add(new Triangle(0, 4, 3));
        maintriangles.Add(new Triangle(0, 1, 4));

        // Down Faces (Counterclockwise)
        maintriangles.Add(new Triangle(5, 1, 2));
        maintriangles.Add(new Triangle(5, 2, 3));
        maintriangles.Add(new Triangle(5, 3, 4));
        maintriangles.Add(new Triangle(5, 4, 1));


        for (int i = 0; i < numDivisions; i++)
        {
            Debug.Log("dividing");
            List<Triangle> newTriangles = new List<Triangle>();
            List<Triangle> trinaglesToRemove = new List<Triangle>();
            foreach (var triangle in maintriangles)
            {
                trinaglesToRemove.Add(triangle);
                List<Triangle> createdTriangles = CutUpTriangle(triangle);

                foreach (var newtriangle in createdTriangles)
                {
                    newTriangles.Add(newtriangle);
                }
            }
            foreach (var triangle in trinaglesToRemove)
            {
                maintriangles.Remove(triangle);
            }
            foreach (var newtriangle in newTriangles)
            {
                maintriangles.Add(newtriangle);
            }
        }

        Vertices = vertices.ToArray();
        Triangles = StoretrianglesIntoTriangles(maintriangles).ToArray();


        Vertices = vertices.ToArray();
        //  Triangles = vertexPairs;
        Debug.Log("Hotovo" + Triangles.Length + " a vertexy " + Vertices.Length);
    }

    private List<Triangle> CutUpTriangle(Triangle triangle)
    {
        List<Triangle> triangles = new List<Triangle>();

        // Get shared midpoints (ensuring they are only created once)
        int mid1Index = GetOrCreateMidpoint(triangle.vertex1, triangle.vertex2);
        int mid2Index = GetOrCreateMidpoint(triangle.vertex2, triangle.vertex3);
        int mid3Index = GetOrCreateMidpoint(triangle.vertex3, triangle.vertex1);



        // Create four new triangles
        triangles.Add(new Triangle(triangle.vertex1, mid1Index, mid3Index));
        triangles.Add(new Triangle(mid1Index, triangle.vertex2, mid2Index));
        triangles.Add(new Triangle(mid3Index, mid2Index, triangle.vertex3));
        triangles.Add(new Triangle(mid1Index, mid2Index, mid3Index));

        return triangles;
    }

    private int GetOrCreateMidpoint(int v1, int v2)
    {
        // Ensure consistent order (smallest index first)
        var edgeKey = v1 < v2 ? (v1, v2) : (v2, v1);

        if (edgeMidpointCache.TryGetValue(edgeKey, out int existingMidpoint))
        {
            return existingMidpoint; // Return existing midpoint index if already created
        }

        // Compute the new midpoint using Slerp and normalize to maintain sphere shape
        Vector3 midpoint = Vector3.Slerp(vertices[v1], vertices[v2], 0.5f).normalized;

        int newIndex = vertices.Count;
        vertices.Add(midpoint);
        edgeMidpointCache[edgeKey] = newIndex; // Store midpoint in cache

        return newIndex;
    }

    private List<int> StoretrianglesIntoTriangles(List<Triangle> triangles)
    {
        List<int> triangleIndexes = new List<int>();
        foreach (Triangle triangle in triangles)
        {
            triangleIndexes.Add(triangle.vertex1);
            triangleIndexes.Add(triangle.vertex2);
            triangleIndexes.Add(triangle.vertex3);
        }
        return triangleIndexes;
    }
    // Convenience classes:

    public class Edge
    {
        public int[] vertexIndices;

        public Edge(int[] vertexIndices)
        {
            this.vertexIndices = vertexIndices;
        }
    }

    public struct Triangle
    {
        public int vertex1;
        public int vertex2;
        public int vertex3;

        public Triangle(int v1, int v2, int v3)
        {
            vertex1 = v1;
            vertex2 = v2;
            vertex3 = v3;
        }
    }
}*/