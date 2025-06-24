using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runs 
/// </summary>
/// /// <remarks>
/// The generator allows to choose and run any sphereMesh with ISphereMesh
/// </remarks>
public class SphereMeshGenerator
{
    public enum SphereAlgorithm { Nothing = 0, SebastianLeague = 1, Optimal = 2 }

    public Vector3[] BaseVertices { get; private set; }
    public int[] Triangles { get; private set; }
    public int NumVertices { get; private set; }

    private ISphereMesh currentSphereMeshGenerator;

    /// <summary>
    /// Generates the sphere mesh
    /// </summary>
    /// <param name="algorithm">enum that determins what sphere mesh algoritm will be used</param>
    /// <param name="resolution">determins the LOD in the sphere</param>
    /// <returns>returns a bool representing the success of generation</returns>
    public bool Generate(SphereAlgorithm algorithm, int resolution) // Use float radius internally if needed by algos
    {
        Debug.Log($"Generating Sphere Data: Algorithm={algorithm}, Resolution={resolution}");
        if (algorithm == SphereAlgorithm.Nothing) return false;

        currentSphereMeshGenerator = null; // Reset previous generator

        if (algorithm == SphereAlgorithm.SebastianLeague) currentSphereMeshGenerator = new SphereMesh(resolution);
        else if (algorithm == SphereAlgorithm.Optimal) currentSphereMeshGenerator = new SphereMeshOptimal(resolution);

        if (currentSphereMeshGenerator == null)
        {
            Debug.LogError("Selected Sphere Algorithm not implemented or invalid.");
            BaseVertices = null;
            Triangles = null;
            NumVertices = 0;
            return false;
        }

        // store results
        BaseVertices = currentSphereMeshGenerator.Vertices;
        Triangles = currentSphereMeshGenerator.Triangles;
        NumVertices = BaseVertices?.Length ?? 0; // Handle potential null vertices

        if (NumVertices == 0)
        {
            Debug.LogError("Sphere mesh generation resulted in 0 vertices.");
            BaseVertices = null;
            Triangles = null;
            return false;
        }

        Debug.Log($"Sphere Data Generated: Vertices={NumVertices}, Triangles={Triangles.Length / 3}");
        return true;
    }
}