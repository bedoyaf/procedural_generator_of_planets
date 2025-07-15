using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for spherical meshes, useful if more implementations are at work 
/// </summary>
public interface ISphereMesh
{
    Vector3[] Vertices { get; }
    int[] Triangles { get; }
    int Resolution { get; }
}
