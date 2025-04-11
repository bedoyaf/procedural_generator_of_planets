using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Vector3;

public class SphereMeshOptimal : ISphereMesh
{

    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }
    public int Resolution { get; }

    private List<Vector3> vertices;
    private int numDivisions;

    private Dictionary<(int, int), Edge> edgeCache = new Dictionary<(int, int), Edge>();

    static readonly Vector3[] baseVertices = { up, left, back, right, forward, down };

    public List<Vector3> specialVerticies = new List<Vector3>();




    public SphereMeshOptimal(int resolution, int radius)
    {
        this.Resolution = resolution;
        numDivisions = Mathf.Max(0, resolution);

        vertices = new List<Vector3>();

        vertices.AddRange(baseVertices);

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

        List<Triangle> newTriangles = new List<Triangle>();

        for (int i = 0; i < maintriangles.Count; i++)
        {
            bool up = false;

            Edge edge1 = GetOrCreateEdge(maintriangles[i].vertex1, maintriangles[i].vertex2);
            Edge edge2 = GetOrCreateEdge(maintriangles[i].vertex1, maintriangles[i].vertex3);
            Edge edge3 = GetOrCreateEdge(maintriangles[i].vertex2, maintriangles[i].vertex3);

            if (i < 4) up = true;

            List<Triangle> createdTriangles = SubdivideTriangle(edge1, edge2, edge3, up,i);

            foreach (var newtriangle in createdTriangles)
            {
                newTriangles.Add(newtriangle);
            }
        }

        Vertices = vertices.ToArray();
        Triangles = StoreClassTrianglesIntoIndicies(newTriangles).ToArray();

        Vertices = vertices.ToArray();


     //  Debug.Log("trinagles:" + Triangles.Length + " a vertexy " + Vertices.Length + " edges: " + edgeCache.Count);
    }

    Edge GetOrCreateEdge(int v1, int v2)
    {
        var key = v1 < v2 ? (v1, v2) : (v2, v1);
        if (edgeCache.TryGetValue(key, out Edge existingEdge))
        {
            return existingEdge;
        }
        Edge newEdge = new Edge(v1, v2);
        edgeCache[key] = newEdge; 

        return newEdge;
    }

    private List<Triangle> SubdivideTriangle(Edge e1, Edge e2, Edge e3, bool upper, int triangleIndex)
    {
        if(!upper)
        {
            (e1, e2) = (e2, e1);
        }

        (e2, e3) = (e3, e2);

        (e2.a,e2.b) = (e2.b, e2.a);

        List<Triangle> newTriangles = new List<Triangle>();

        Dictionary<(int, int), int> currentvertices = new Dictionary<(int, int), int>();

        // Add points along the edges
        if (e1.VertexIndices == null) e1 = AddVertexesBetweenPoints(numDivisions, e1);
        if (e2.VertexIndices == null) e2 = AddVertexesBetweenPoints(numDivisions, e2);
        if (e3.VertexIndices == null) e3 = AddVertexesBetweenPoints(numDivisions, e3);

        return ConnectVerticiesToTriangles(CreateCacheOfFaceVerticies(e1,e2,e3, triangleIndex), e1.VertexIndices.Length, triangleIndex);
    }

    private Dictionary<(int, int), int> CreateCacheOfFaceVerticies(Edge e1, Edge e2, Edge e3, int triangleIndex)
    {
        Dictionary<(int, int), int> currentvertices = new Dictionary<(int, int), int> ();

        //corner
        currentvertices[(0, 0)] = e1.VertexIndices[0];

        for (int i = 1; i < e1.VertexIndices.Length - 1; i++)
        {
            // Side A vertex
            currentvertices[(i, 0)] = e1.VertexIndices[i];

            Vector3 sideAVertex = vertices[e1.VertexIndices[i]];
            Vector3 sideBVertex = vertices[e2.VertexIndices[i]];
            int numInnerPoints = e1.VertexIndices.Length - i - 2;

            for (int j = 1; j <= numInnerPoints; j++)
            {
                float t = (float)j / (numInnerPoints + 1);
                currentvertices[(i, j)] = vertices.Count;

                vertices.Add(Slerp(sideAVertex.normalized, sideBVertex.normalized, t));
            }

            // Side B vertex
            currentvertices[(i, e1.VertexIndices.Length - i - 1)] = e2.VertexIndices[i];

        }
        //top
        currentvertices[(e2.VertexIndices.Length - 1, 0)] = e2.VertexIndices[e2.VertexIndices.Length - 1];

        for (int i = 0; i < e3.VertexIndices.Length; i++)
        {
            currentvertices[(0, i)] = e3.VertexIndices[i];
        }

        return currentvertices;
    }

    private List<Triangle> ConnectVerticiesToTriangles(Dictionary<(int, int), int> currentvertices, int numOfVerticiesOnEdge, int triangleIndex)
    {
        List<Triangle> newTriangles = new List<Triangle>(); 
        for (int i = 0; i < numOfVerticiesOnEdge - 1; i++)
        {
            for (int j = 0; j < numOfVerticiesOnEdge - 1 - i; j++)
            {
                int v0 = currentvertices[(i, j)];
                int v1 = currentvertices[(i + 1, j)];
                int v2 = currentvertices[(i, j + 1)];

                Vector3 normal = Vector3.Cross(vertices[v1] - vertices[v0], vertices[v2] - vertices[v0]);
                if (Vector3.Dot(normal, vertices[v0].normalized) < 0)
                {
                    newTriangles.Add(new Triangle(v1, v0, v2));
                }
                else
                {
                    newTriangles.Add(new Triangle(v0, v1, v2));
                }

                if (i > 0)
                {
                    int v0u = currentvertices[(i, j)];
                    int v1u = currentvertices[(i, j + 1)];
                    int v2u = currentvertices[(i - 1, j + 1)];

                    Vector3 normalu = Vector3.Cross(vertices[v1u] - vertices[v0u], vertices[v2u] - vertices[v0u]);
                    if (Vector3.Dot(normalu, vertices[v0u].normalized) < 0)
                    {
                        newTriangles.Add(new Triangle(v1u, v0u, v2u));
                    }
                    else
                    {
                        newTriangles.Add(new Triangle(v0u, v1u, v2u));
                    }
                }
            }
        }



        return newTriangles;
    }
 
    private Edge AddVertexesBetweenPoints(int numDivisions, Edge e)
    {
        List<int> createdVertices = new List<int>();

        //first corner vertex
        createdVertices.Add(e.a);

        // Add vertices along edge
        for (int divisionIndex = 0; divisionIndex < numDivisions; divisionIndex++)
        {
            float t = (divisionIndex + 1f) / (numDivisions + 1f);
            createdVertices.Add(vertices.Count);
            vertices.Add(Slerp(vertices[e.a], vertices[e.b], t).normalized);
        }
        //last corner vertex
        createdVertices.Add(e.b);

        //Debug.Log("created vertices: " + createdVertices.Count);
        e.VertexIndices = createdVertices.ToArray();
        return e;
    }



    public List<int> CreateEdgeIndices(int[] triangleIndices)
    {
        List<int> edgeIndices = new List<int>();
        HashSet<(int, int)> uniqueEdges = new HashSet<(int, int)>();

        for (int i = 0; i < triangleIndices.Length; i += 3)
        {
            // Each set of 3 indices forms a triangle
            int v1 = triangleIndices[i];
            int v2 = triangleIndices[i + 1];
            int v3 = triangleIndices[i + 2];

            // Add edges for the triangle
            AddEdgeIfUnique(v1, v2, uniqueEdges, edgeIndices);
            AddEdgeIfUnique(v2, v3, uniqueEdges, edgeIndices);
            AddEdgeIfUnique(v3, v1, uniqueEdges, edgeIndices);
        }

        return edgeIndices;
    }

    private void AddEdgeIfUnique(int v1, int v2, HashSet<(int, int)> uniqueEdges, List<int> edgeIndices)
    {
        var edge = v1 < v2 ? (v1, v2) : (v2, v1);
        if (!uniqueEdges.Contains(edge))
        {
            uniqueEdges.Add(edge);
            edgeIndices.Add(v1);
            edgeIndices.Add(v2);
        }
    }


    private List<int> StoreClassTrianglesIntoIndicies(List<Triangle> triangles)
    => triangles.SelectMany(t => new[] { t.vertex1, t.vertex2, t.vertex3 }).ToList();

    public class Edge
    {
        public int a;
        public int b;
        public int[] VertexIndices { get; set; }

        public Edge(int a, int b)
        {
            this.a = a;
            this.b = b;
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
}