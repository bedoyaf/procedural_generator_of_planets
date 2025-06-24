using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Vector3;

/// <summary>
/// generates and represents a spherical mesh
/// </summary>
public class SphereMeshOptimal : ISphereMesh
{

    public Vector3[] Vertices { get; private set; } //All the finished Verticies
    public int[] Triangles { get; private set; } // All the finished triangles
    public int Resolution { get; } // Represents the LOD by determining the num of subdivisions

    private List<Vector3> vertices;
    private int numDivisions;

    private Dictionary<(int, int), Edge> edgeCache = new Dictionary<(int, int), Edge>(); //stored all the created edges from the base triangles, key corresponds to the two main verticies on the edge

    static readonly Vector3[] baseVertices = { up, left, back, right, forward, down };

    /// <summary>
    /// Constructor that sets up the whole sphere mainly calculates positions of verticies and determins triangles. 
    /// Starts by setting up a octohedron and then subdividing the faces 
    /// </summary>
    /// <param name="resolution">Determins the LOD, the number corresponds to the number each edge of the octahedron face will be subdivided</param>
    public SphereMeshOptimal(int resolution)
    {
        this.Resolution = resolution;
        numDivisions = Mathf.Max(0, resolution);

        vertices = new List<Vector3>();

        vertices.AddRange(baseVertices);

        List<Triangle> baseTriangles = new List<Triangle>();

        //Manual assigmen of the base octahedron, from which the triangles subdivide into a sphere

        // Up Faces (Counterclockwise)
        baseTriangles.Add(new Triangle(0, 2, 1));
        baseTriangles.Add(new Triangle(0, 3, 2));
        baseTriangles.Add(new Triangle(0, 4, 3));
        baseTriangles.Add(new Triangle(0, 1, 4));

        // Down Faces (Counterclockwise)
        baseTriangles.Add(new Triangle(5, 1, 2));
        baseTriangles.Add(new Triangle(5, 2, 3));
        baseTriangles.Add(new Triangle(5, 3, 4));
        baseTriangles.Add(new Triangle(5, 4, 1));

        List<Triangle> newTriangles = new List<Triangle>();

        for (int i = 0; i < baseTriangles.Count; i++)
        {
            bool up = false;

            //gets edges of the octahedrons face and making sure it doesnt create new ones if they already exist
            Edge edge1 = GetOrCreateEdge(baseTriangles[i].vertex1, baseTriangles[i].vertex2);
            Edge edge2 = GetOrCreateEdge(baseTriangles[i].vertex1, baseTriangles[i].vertex3);
            Edge edge3 = GetOrCreateEdge(baseTriangles[i].vertex2, baseTriangles[i].vertex3);

            if (i < 4) up = true;

            List<Triangle> createdTriangles = SubdivideTriangle(edge1, edge2, edge3, up);

            foreach (var newtriangle in createdTriangles)
            {
                newTriangles.Add(newtriangle);
            }
        }
        //Store all the data
        Vertices = vertices.ToArray();
        Triangles = StoreClassTrianglesIntoIndicies(newTriangles).ToArray();
        Vertices = vertices.ToArray();
     //  Debug.Log("trinagles:" + Triangles.Length + " a vertexy " + Vertices.Length + " edges: " + edgeCache.Count);
    }

    /// <summary>
    /// Fuction determins if there already exists an edge with the vertex indicies, and if not it creates it and returns it
    /// </summary>
    /// <param name="v1">First vertex index</param>
    /// <param name="anotherParam">Second vertex index</param>
    /// <returns>an new edge to be used</returns>

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

    /// <summary>
    /// Takes the edges of the triangle and adds new verticies along the edges corresponding to resolution
    /// Than calls functions to generate new verticies inside the triangle that allign with those on the edges, and are correctly stored as connected triangles
    /// </summary>
    /// <param name="e1">first edge</param>
    /// <param name="e2">second edge</param>
    /// <param name="e3">third edge</param>
    /// <param name="upper">determins if the triangle is in the lower part of the octahedron or higher part</param>
    /// <returns>returns a list of triangles corresponding to the newly created triangle in our base triangle</returns>
    private List<Triangle> SubdivideTriangle(Edge e1, Edge e2, Edge e3, bool upper)
    {
        //checks if the triangle is in the upper or lower part of the octahedron and switches edges acordingly
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

        return ConnectVerticiesToTriangles(CreateCacheOfFaceVerticies(e1,e2,e3), e1.VertexIndices.Length);
    }


    /// <summary>
    /// It creates new verticies by filling the inside of the triangle line by line and calculating distances between verticies
    /// It stores them in a dictionary where the key is the coordinates of their creation, 
    /// this plays a crucial role in connecting the vertices into triangles in another function
    /// </summary>
    /// <param name="e1">first edge</param>
    /// <param name="e2">second edge</param>
    /// <param name="e3">third edge</param>
    /// <returns> Dictionary with coordinates of the verticies within the grind as their key and their index in the verticies as value</returns>
    private Dictionary<(int, int), int> CreateCacheOfFaceVerticies(Edge e1, Edge e2, Edge e3)
    {
        Dictionary<(int, int), int> currentvertices = new Dictionary<(int, int), int> ();

        //corner
        currentvertices[(0, 0)] = e1.VertexIndices[0];

        //line by line creation of verticies
        for (int i = 1; i < e1.VertexIndices.Length - 1; i++)
        {
            //the first one on the edge
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

    /// <summary>
    /// Goes though a dictionary of verticies with the keys being coordinates within a grid and connects those indicies into triangles
    /// </summary>
    /// <param name="currentvertices">Dictionary containing coordinates in a grid of verticies</param>
    /// <param name="numOfVerticiesOnEdge">How many verticies are on one of the base triangles</param>
    /// <returns>[What the method returns]</returns>
    /// <remarks>
    /// the list of the newly connected triangles on the face
    /// </remarks>

    private List<Triangle> ConnectVerticiesToTriangles(Dictionary<(int, int), int> currentvertices, int numOfVerticiesOnEdge)
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

                //takes care of the backwards facing triangles
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

                    //takes care of the backwards facing triangles
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

    /// <summary>
    /// Fills out the edge with new verticies correspongding to the resolution
    /// </summary>
    /// <param name="numDivisions">resolution, determining the LOD and how many verticies will be now added</param>
    /// <param name="e">current edge</param>
    /// <returns>the edge filled with added verticies</returns>
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

    /// <summary>
    /// Converts the list of triangles into a List of indicies of triangles as used in a mesh
    /// </summary>
    /// <param name="triangles">List of triangles</param>
    /// <returns> List of indicies of triangles as used in a mesh
    private List<int> StoreClassTrianglesIntoIndicies(List<Triangle> triangles)
    => triangles.SelectMany(t => new[] { t.vertex1, t.vertex2, t.vertex3 }).ToList();

    /// <summary>
    /// Represents an edge on the base triangle, stored the first and last vertex and all the ones in between in an array
    /// </summary>
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

    /// <summary>
    /// Scores indicies of verticies in a triangle, that represents a triangle in the mesh
    /// </summary>
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