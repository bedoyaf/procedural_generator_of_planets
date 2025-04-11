using System.Collections.Generic;
using UnityEngine;

public interface ISphereMesh
{
    Vector3[] Vertices { get; }
    int[] Triangles { get; }
    int Resolution { get; }

    public List<int> CreateEdgeIndices(int[] triangleIndices);
}
